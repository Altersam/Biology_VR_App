using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    public enum Mood { Neutral, Smile, Surprised, Serious, Sad, Angry }

    /// <summary>
    /// The professor. Three ways to appear:
    /// • stage (prologue lab): full height, solid, stands at a spot, turns to you, talks and gestures;
    /// • docked (the artery): a waist-up hologram on a pop-up comm screen at the left of the view;
    /// • follow (legacy, docked = false): a small companion floating beside the player.
    /// He speaks with a recorded voice (<see cref="Bvr2VoiceBank"/>): the clip plays, the subtitle stays as long
    /// as the clip, and the mouth follows lip-sync curves (open / rounded / wide) on the head, beard and
    /// moustache. Lines without a recording keep subtitles and a procedural mouth.
    /// </summary>
    public sealed class Bvr2Professor : MonoBehaviour
    {
        public Bvr2Rig rig;
        public Bvr2Hud hud;
        public Transform model, headPivot, handL, handR, medallion;
        public SkinnedMeshRenderer face;
        [Tooltip("Other meshes with a MouthOpen blend shape (beard, moustache).")]
        public SkinnedMeshRenderer[] speechParts = new SkinnedMeshRenderer[0];
        public Vector3 followOffset = new Vector3(0.8f, -0.12f, 1.5f);
        public float scale = 0.34f;
        public string speakerName = "Профессор";
        [Header("Comm screen (docked)")]
        public bool docked = true;
        public float dockScale = 0.4f, waistHeight = 1.0f, topHeight = 1.93f, linger = 2.5f;
        [Header("Stage (full height, solid)")]
        public float stageScale = 1f;
        [Header("Voice")]
        public Bvr2VoiceBank voice;
        public AudioSource voiceSource;

        struct Line { public string text; public Mood mood; public float seconds; public Bvr2VoiceBank.Line rec; }
        readonly Queue<Line> queue = new Queue<Line>();
        Renderer[] renderers; MaterialPropertyBlock mpb;
        int bsSmile = -1, bsSurprised = -1, bsSerious = -1, bsSad = -1, bsAngry = -1, bsBlink = -1, bsSquint = -1, bsOpen = -1, bsRound = -1, bsWide = -1;
        int[] partOpen = new int[0];
        float speakUntil, nextBlink, blinkT = -1, reveal, revealTarget = 1, talkPhase, mouthOpen, mouthRound, mouthWide, emphasis; Mood mood = Mood.Smile;
        readonly float[] weights = new float[5];
        Vector3 vel; Vector3? presentAt; float presentUntil; Vector3? pointAt; float pointUntil;
        Quaternion handLRest, handRRest, headRest; Vector3 modelRest; float lingerUntil;
        Bvr2VoiceBank.Line current; Transform stageSpot, pendingSpot; bool commFilters = true;
        AudioHighPassFilter commHigh; AudioLowPassFilter commLow;

        public bool Speaking => Time.unscaledTime < speakUntil;
        public bool Busy => Speaking || queue.Count > 0;
        public bool Visible => revealTarget > 0.5f;
        public bool OnStage => stageSpot || pendingSpot;
        /// <summary>0..1 how materialised the comm hologram is (docked mode).</summary>
        public float CommReveal => docked && !OnStage ? reveal : 0f;
        public string CurrentLine { get; private set; } = "";
        /// <summary>The line being spoken has a recording that is playing.</summary>
        public bool Voiced => current != null && voiceSource && voiceSource.isPlaying;
        /// <summary>Current mouth opening 0..1 (tests).</summary>
        public float MouthOpen => mouthOpen;

        void Awake()
        {
            renderers = model ? model.GetComponentsInChildren<Renderer>(true) : GetComponentsInChildren<Renderer>(true);
            mpb = new MaterialPropertyBlock();
            if (face && face.sharedMesh)
            {
                var m = face.sharedMesh;
                bsSmile = m.GetBlendShapeIndex("Smile"); bsSurprised = m.GetBlendShapeIndex("Surprised"); bsSerious = m.GetBlendShapeIndex("Serious");
                bsSad = m.GetBlendShapeIndex("Sad"); bsAngry = m.GetBlendShapeIndex("Angry"); bsBlink = m.GetBlendShapeIndex("Blink"); bsSquint = m.GetBlendShapeIndex("Squint");
                bsOpen = m.GetBlendShapeIndex("MouthOpen"); bsRound = m.GetBlendShapeIndex("MouthO"); bsWide = m.GetBlendShapeIndex("MouthWide");
            }
            partOpen = new int[speechParts.Length];
            for (int i = 0; i < speechParts.Length; i++) partOpen[i] = speechParts[i] && speechParts[i].sharedMesh ? speechParts[i].sharedMesh.GetBlendShapeIndex("MouthOpen") : -1;
            if (model) modelRest = model.localPosition;
            if (handL) handLRest = handL.localRotation; if (handR) handRRest = handR.localRotation; if (headPivot) headRest = headPivot.localRotation;
            transform.localScale = Vector3.one * (docked ? dockScale : scale);
            if (docked) reveal = 0;
            if (voiceSource)
            {
                commHigh = voiceSource.GetComponent<AudioHighPassFilter>(); if (!commHigh) commHigh = voiceSource.gameObject.AddComponent<AudioHighPassFilter>();
                commLow = voiceSource.GetComponent<AudioLowPassFilter>(); if (!commLow) commLow = voiceSource.gameObject.AddComponent<AudioLowPassFilter>();
                commHigh.cutoffFrequency = 240f; commLow.cutoffFrequency = 5600f;
            }
        }

        public void Appear(bool on) { revealTarget = on ? 1 : 0; }

        public void Say(string text, Mood m = Mood.Neutral, float seconds = 0)
        {
            var rec = voice ? voice.Find(text) : null;
            if (seconds <= 0) seconds = rec != null ? rec.clip.length + 0.35f : Mathf.Clamp(1.6f + text.Length * 0.052f, 2.5f, 9f);
            queue.Enqueue(new Line { text = text, mood = m, seconds = seconds, rec = rec });
        }

        public void Interrupt() { queue.Clear(); speakUntil = 0; current = null; if (voiceSource) voiceSource.Stop(); hud?.ClearSubtitle(); }
        public void SetMood(Mood m) { mood = m; }
        public void Present(Vector3 point, float seconds, float side = -1, float sideOffset = 0f) { presentAt = point; presentUntil = Time.unscaledTime + seconds; presentSide = Mathf.Sign(side); presentOffset = sideOffset; }
        float presentSide = -1, presentOffset;
        public void PointAt(Vector3 point, float seconds) { pointAt = point; pointUntil = Time.unscaledTime + seconds; }

        /// <summary>Stand at a spot in full height (prologue). Moving to another spot: fade out, reappear there.</summary>
        public void StandAt(Transform spot)
        {
            if (!spot) { LeaveStage(); return; }
            if (stageSpot == spot && !pendingSpot) { revealTarget = 1; return; }
            if (OnStage && reveal > 0.05f) { pendingSpot = spot; revealTarget = 0; }
            else { stageSpot = spot; pendingSpot = null; reveal = 0; revealTarget = 1; PlaceOnStage(true); }
        }

        /// <summary>Back to the comm screen (the artery).</summary>
        public void LeaveStage() { stageSpot = null; pendingSpot = null; reveal = 0; revealTarget = 1; }

        void Update()
        {
            float dt = Time.unscaledDeltaTime, now = Time.unscaledTime;
            if (!Speaking && queue.Count > 0)
            {
                var l = queue.Dequeue(); CurrentLine = l.text; mood = l.mood; speakUntil = now + l.seconds; current = l.rec;
                hud?.Say(speakerName, l.text, l.seconds);
                if (voiceSource) { voiceSource.Stop(); if (current != null) { voiceSource.clip = current.clip; voiceSource.Play(); } }
            }
            if (!Speaking && current != null && (!voiceSource || !voiceSource.isPlaying)) current = null;
            if (!rig || !rig.head) return;
            var head = rig.head.transform;
            bool stage = OnStage;
            if (stage) Stage(head, dt, now);
            else if (docked && hud && hud.CommDock) Dock(head, dt, now);
            else Follow(head, dt, now);
            Voice(stage);
            Face(dt, now);
            // reveal (materialise from the bottom up) + medallion glow
            Vector3 axis, cutPoint; float baseY, topY;
            if (!stage && docked && hud && hud.CommDock)
            {
                bool want = revealTarget > 0.5f && (Busy || now < lingerUntil);
                if (Busy) lingerUntil = now + linger;
                reveal = Mathf.MoveTowards(reveal, want ? 1f : 0f, dt * (want ? 2.8f : 1.8f));
                hud.CommAlpha = reveal;
                axis = transform.up; float sc = transform.lossyScale.y;
                cutPoint = transform.position + axis * waistHeight * sc;
                baseY = Vector3.Dot(cutPoint, axis); topY = Vector3.Dot(transform.position + axis * topHeight * sc, axis);
            }
            else
            {
                reveal = Mathf.MoveTowards(reveal, revealTarget, dt * (stage ? 1.5f : 0.9f));
                if (hud) hud.CommAlpha = 0;
                axis = Vector3.up; cutPoint = Vector3.zero;
                baseY = transform.position.y; topY = baseY + 1.9f * transform.lossyScale.y;
            }
            var clip = !stage && docked ? new Vector4(axis.x, axis.y, axis.z, -Vector3.Dot(axis, cutPoint)) : Vector4.zero;
            float solid = stage ? 1f : 0f;
            foreach (var r in renderers)
            {
                if (!r) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_Reveal", reveal);
                mpb.SetVector("_RevealRange", new Vector4(baseY, topY, 0, 0));
                mpb.SetVector("_RevealAxis", axis);
                mpb.SetVector("_ClipPlane", clip);
                mpb.SetFloat("_Solid", solid);
                if (r.transform == medallion) mpb.SetColor("_Emission", Bvr2Style.Ally * (0.25f + (Speaking ? 0.5f + 0.4f * Mathf.Sin(now * 9f) : 0.15f)));
                r.SetPropertyBlock(mpb);
                r.enabled = reveal > 0.001f;
            }
        }

        /// <summary>Voice: 3D and natural in the lab; a slightly band-limited "comm" sound on the screen.</summary>
        void Voice(bool stage)
        {
            if (!voiceSource) return;
            voiceSource.spatialBlend = stage ? 0.85f : 0f;
            if (headPivot) voiceSource.transform.position = headPivot.position + Vector3.up * 0.1f;
            bool comm = !stage;
            if (comm != commFilters) { commFilters = comm; if (commHigh) commHigh.enabled = comm; if (commLow) commLow.enabled = comm; }
        }

        // ---------------------------------------------------------------- placement
        void PlaceOnStage(bool snap)
        {
            if (!stageSpot) return;
            transform.localScale = Vector3.one * stageScale * stageSpot.lossyScale.y;
            transform.position = stageSpot.position;
            if (snap) transform.rotation = Quaternion.Euler(0, stageSpot.eulerAngles.y, 0);
            if (model) model.localPosition = modelRest;
        }

        /// <summary>Full height at the spot: turns to the player, looks at them, gestures while talking.</summary>
        void Stage(Transform head, float dt, float now)
        {
            if (pendingSpot && reveal <= 0.01f) { stageSpot = pendingSpot; pendingSpot = null; PlaceOnStage(true); revealTarget = 1; }
            if (!stageSpot) return;
            transform.localScale = Vector3.one * stageScale * stageSpot.lossyScale.y; // grows with the lab when you shrink
            transform.position = stageSpot.position;
            Vector3 flat = Vector3.ProjectOnPlane(head.position - transform.position, Vector3.up);
            if (flat.sqrMagnitude > 0.04f) transform.rotation = Bvr2Math.Damp(transform.rotation, Quaternion.LookRotation(flat), 2.5f, dt);
            if (model) model.localPosition = modelRest;
            LookAndGesture(head, dt, now, transform.rotation);
        }

        /// <summary>Docked on the comm screen: waist at the bottom of the panel, facing the player.</summary>
        void Dock(Transform head, float dt, float now)
        {
            var dock = hud.CommDock;
            float sc = dockScale; transform.localScale = Vector3.one * sc;
            Quaternion rot = Quaternion.LookRotation(-dock.forward, dock.up);
            Vector3 waist = dock.position + dock.up * (-hud.CommPanelHeight * 0.5f + 0.035f) - dock.forward * 0.03f;
            transform.SetPositionAndRotation(waist - dock.up * waistHeight * sc, rot);
            if (model) model.localPosition = modelRest + Vector3.up * (Mathf.Sin(now * 1.3f) * 0.02f);
            LookAndGesture(head, dt, now, transform.rotation);
        }

        /// <summary>World companion (legacy): floats beside the player or next to what we explain.</summary>
        void Follow(Transform head, float dt, float now)
        {
            transform.localScale = Vector3.one * scale;
            Vector3 flatF = Vector3.ProjectOnPlane(head.forward, Vector3.up); if (flatF.sqrMagnitude < 0.01f) flatF = head.up; flatF.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, flatF);
            Vector3 goal = head.position + flatF * followOffset.z + right * followOffset.x + Vector3.up * followOffset.y;
            if (presentAt.HasValue && now < presentUntil)
            {
                Vector3 p = presentAt.Value; Vector3 toP = p - head.position;
                float dd = Mathf.Min(toP.magnitude, 2.2f) + 0.2f;
                float off = presentOffset > 0 ? presentOffset : (presentSide < 0 ? 0.82f : 0.78f);
                goal = head.position + toP.normalized * dd + right * (off * presentSide) + Vector3.up * -0.05f;
            }
            goal -= Vector3.up * 1.68f * scale; // model origin is at the feet; keep the face near the eye line
            float dist = Vector3.Distance(transform.position, goal);
            if (dist > 6f) transform.position = goal;
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref vel, 0.55f, 6f, dt);
            if (model) model.localPosition = modelRest + Vector3.up * (Mathf.Sin(now * 1.3f) * 0.05f);
            Vector3 toHead = head.position - transform.position; Vector3 flat = Vector3.ProjectOnPlane(toHead, Vector3.up);
            if (flat.sqrMagnitude > 0.01f) transform.rotation = Bvr2Math.Damp(transform.rotation, Quaternion.LookRotation(flat), 4f, dt);
            LookAndGesture(head, dt, now, transform.rotation);
        }

        void LookAndGesture(Transform head, float dt, float now, Quaternion body)
        {
            bool pointing = pointAt.HasValue && now < pointUntil;
            bool presenting = presentAt.HasValue && now < presentUntil;
            if (headPivot)
            {
                // eye contact; a glance toward what is being explained; small nods that follow the voice
                Vector3 lookTarget = pointing ? Vector3.Lerp(head.position, pointAt.Value, 0.6f)
                    : presenting && Mathf.Repeat(now, 5f) < 1.6f ? Vector3.Lerp(head.position, presentAt.Value, 0.5f) : head.position;
                Quaternion want = Quaternion.LookRotation(lookTarget - headPivot.position, body * Vector3.up);
                Quaternion local = Quaternion.Inverse(body) * want;
                var e = local.eulerAngles; float yaw = Mathf.Clamp(Mathf.DeltaAngle(0, e.y), -55, 55), pitch = Mathf.Clamp(Mathf.DeltaAngle(0, e.x), -25, 25);
                float nod = Speaking ? Mathf.Sin(now * 2.1f) * 1.6f + emphasis * 4f : Mathf.Sin(now * 0.7f) * 1.5f;
                headPivot.localRotation = Bvr2Math.Damp(headPivot.localRotation, headRest * Quaternion.Euler(pitch + nod, yaw, Mathf.Sin(now * 0.9f) * 2f), 6f, dt);
            }
            // gestures: the hands move with the phrase, a little more on stressed words
            float g = Speaking ? 1f + emphasis : 0f;
            if (handR) handR.localRotation = Bvr2Math.Damp(handR.localRotation, handRRest * Quaternion.Euler(pointing ? -55 : (Speaking ? Mathf.Sin(now * 3.1f) * 9 * g - 8 : Mathf.Sin(now) * 3), pointing ? 20 : 0, pointing ? 25 : 0), 5f, dt);
            if (handL) handL.localRotation = Bvr2Math.Damp(handL.localRotation, handLRest * Quaternion.Euler(Speaking ? Mathf.Sin(now * 2.6f + 1) * 8 * g - 6 : Mathf.Sin(now * 0.8f + 2) * 3, 0, 0), 5f, dt);
        }

        void Face(float dt, float now)
        {
            if (!face) return;
            // expression (0..100)
            for (int i = 0; i < weights.Length; i++) weights[i] = 0;
            switch (mood)
            {
                case Mood.Smile: weights[0] = 65; break;
                case Mood.Surprised: weights[1] = 75; break;
                case Mood.Serious: weights[2] = 85; break;
                case Mood.Sad: weights[3] = 70; break;
                case Mood.Angry: weights[4] = 60; break;
                default: weights[0] = 22; break;
            }
            // mouth: recorded lip sync, or a syllable rhythm for lines without a recording
            float o = 0, r = 0, w = 0;
            if (Voiced) current.Sample(voiceSource.time, out o, out r, out w);
            else if (Speaking && current == null)
            {
                talkPhase += dt * (8.5f + Mathf.PerlinNoise(now * 2f, 0.3f) * 4f);
                o = Mathf.Max(0, Mathf.Sin(talkPhase)) * (0.35f + 0.4f * Mathf.PerlinNoise(now * 3f, 1.7f));
                r = Mathf.PerlinNoise(now * 1.7f, 4.2f) > 0.62f ? 0.6f : 0f; w = Mathf.PerlinNoise(now * 1.3f, 8.1f) > 0.6f ? 0.5f : 0f;
            }
            float k = 1f - Mathf.Exp(-dt * 28f);
            mouthOpen = Mathf.Lerp(mouthOpen, o, k); mouthRound = Mathf.Lerp(mouthRound, r * Mathf.Clamp01(o * 2.5f), k); mouthWide = Mathf.Lerp(mouthWide, w * Mathf.Clamp01(o * 2.5f), k);
            emphasis = Mathf.Lerp(emphasis, Mathf.Clamp01((o - 0.62f) * 3f), 1f - Mathf.Exp(-dt * 6f));
            weights[1] = Mathf.Max(weights[1], emphasis * 14f);           // brows lift on stressed words
            if (mouthWide > 0.2f) weights[0] = Mathf.Max(weights[0], 20f); // "и/э" pulls the corners
            // blinking
            if (now > nextBlink && blinkT < 0) { blinkT = 0; nextBlink = now + Random.Range(2.2f, 5f); }
            float blink = 0;
            if (blinkT >= 0) { blinkT += dt; blink = Mathf.Sin(Mathf.Clamp01(blinkT / 0.16f) * Mathf.PI) * 100; if (blinkT > 0.16f) blinkT = -1; }
            Set(bsSmile, weights[0], dt); Set(bsSurprised, weights[1], dt); Set(bsSerious, weights[2], dt); Set(bsSad, weights[3], dt); Set(bsAngry, weights[4], dt);
            if (bsBlink >= 0) face.SetBlendShapeWeight(bsBlink, blink);
            float open = mouthOpen * 78f;
            if (bsOpen >= 0) face.SetBlendShapeWeight(bsOpen, open);
            if (bsRound >= 0) face.SetBlendShapeWeight(bsRound, mouthRound * 85f);
            if (bsWide >= 0) face.SetBlendShapeWeight(bsWide, mouthWide * 70f);
            for (int i = 0; i < speechParts.Length; i++) if (speechParts[i] && partOpen[i] >= 0) speechParts[i].SetBlendShapeWeight(partOpen[i], open);
        }

        void Set(int index, float target, float dt)
        {
            if (index < 0) return;
            face.SetBlendShapeWeight(index, Mathf.Lerp(face.GetBlendShapeWeight(index), target, 1f - Mathf.Exp(-dt * 14f)));
        }
    }
}
