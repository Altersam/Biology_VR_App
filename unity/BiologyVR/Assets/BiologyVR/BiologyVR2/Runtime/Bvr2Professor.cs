using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    public enum Mood { Neutral, Smile, Surprised, Serious, Sad, Angry }

    /// <summary>
    /// The professor as a hologram on a pop-up comm screen: when he speaks, a waist-up hologram
    /// materialises in a small panel at the left of the view (docked to the visor), looks at you,
    /// blinks, changes expression, "talks" (mouth shapes + subtitles), glances at what he explains,
    /// and fades out a moment after the line. (docked = false: the old full-body companion.)
    /// Lines are short (3–8 s) and queued; the voice will be added later.
    /// </summary>
    public sealed class Bvr2Professor : MonoBehaviour
    {
        public Bvr2Rig rig;
        public Bvr2Hud hud;
        public Transform model, headPivot, handL, handR, medallion;
        public SkinnedMeshRenderer face;
        public Vector3 followOffset = new Vector3(0.8f, -0.12f, 1.5f);
        public float scale = 0.34f;
        public string speakerName = "Профессор";
        [Header("Comm screen (docked)")]
        public bool docked = true;
        public float dockScale = 0.4f, waistHeight = 1.0f, topHeight = 1.93f, linger = 2.5f;

        struct Line { public string text; public Mood mood; public float seconds; }
        readonly Queue<Line> queue = new Queue<Line>();
        Renderer[] renderers; MaterialPropertyBlock mpb;
        int bsSmile = -1, bsSurprised = -1, bsSerious = -1, bsSad = -1, bsAngry = -1, bsBlink = -1, bsSquint = -1;
        float speakUntil, nextBlink, blinkT = -1, reveal, revealTarget = 1, talkPhase; Mood mood = Mood.Smile;
        readonly float[] weights = new float[7];
        Vector3 vel; Vector3? presentAt; float presentUntil; Vector3? pointAt; float pointUntil;
        Quaternion handLRest, handRRest, headRest; Vector3 modelRest; float lingerUntil;

        public bool Speaking => Time.unscaledTime < speakUntil;
        public bool Busy => Speaking || queue.Count > 0;
        public bool Visible => revealTarget > 0.5f;
        /// <summary>0..1 how materialised the comm hologram is (docked mode).</summary>
        public float CommReveal => docked ? reveal : 0f;
        public string CurrentLine { get; private set; } = "";

        void Awake()
        {
            renderers = model ? model.GetComponentsInChildren<Renderer>(true) : GetComponentsInChildren<Renderer>(true);
            mpb = new MaterialPropertyBlock();
            if (face && face.sharedMesh)
            {
                var m = face.sharedMesh;
                bsSmile = m.GetBlendShapeIndex("Smile"); bsSurprised = m.GetBlendShapeIndex("Surprised"); bsSerious = m.GetBlendShapeIndex("Serious");
                bsSad = m.GetBlendShapeIndex("Sad"); bsAngry = m.GetBlendShapeIndex("Angry"); bsBlink = m.GetBlendShapeIndex("Blink"); bsSquint = m.GetBlendShapeIndex("Squint");
            }
            if (model) modelRest = model.localPosition;
            if (handL) handLRest = handL.localRotation; if (handR) handRRest = handR.localRotation; if (headPivot) headRest = headPivot.localRotation;
            transform.localScale = Vector3.one * (docked ? dockScale : scale);
            if (docked) reveal = 0;
        }

        public void Appear(bool on) { revealTarget = on ? 1 : 0; }

        public void Say(string text, Mood m = Mood.Neutral, float seconds = 0)
        {
            if (seconds <= 0) seconds = Mathf.Clamp(1.6f + text.Length * 0.052f, 2.5f, 9f);
            queue.Enqueue(new Line { text = text, mood = m, seconds = seconds });
        }

        public void Interrupt() { queue.Clear(); speakUntil = 0; hud?.ClearSubtitle(); }
        public void SetMood(Mood m) { mood = m; }
        public void Present(Vector3 point, float seconds, float side = -1, float sideOffset = 0f) { presentAt = point; presentUntil = Time.unscaledTime + seconds; presentSide = Mathf.Sign(side); presentOffset = sideOffset; }
        float presentSide = -1, presentOffset;
        public void PointAt(Vector3 point, float seconds) { pointAt = point; pointUntil = Time.unscaledTime + seconds; }

        void Update()
        {
            float dt = Time.unscaledDeltaTime, now = Time.unscaledTime;
            if (!Speaking && queue.Count > 0)
            {
                var l = queue.Dequeue(); CurrentLine = l.text; mood = l.mood; speakUntil = now + l.seconds;
                hud?.Say(speakerName, l.text, l.seconds);
            }
            if (!rig || !rig.head) return;
            var head = rig.head.transform;
            if (docked && hud && hud.CommDock) Dock(head, dt, now);
            else Follow(head, dt, now);
            Face(dt, now);
            // hologram reveal + medallion glow
            Vector3 axis, cutPoint; float baseY, topY;
            if (docked && hud && hud.CommDock)
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
                reveal = Mathf.MoveTowards(reveal, revealTarget, dt * 0.9f);
                axis = Vector3.up; cutPoint = Vector3.zero;
                baseY = transform.position.y; topY = baseY + 1.85f * transform.lossyScale.y;
            }
            var clip = docked ? new Vector4(axis.x, axis.y, axis.z, -Vector3.Dot(axis, cutPoint)) : Vector4.zero;
            foreach (var r in renderers)
            {
                if (!r) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_Reveal", reveal);
                mpb.SetVector("_RevealRange", new Vector4(baseY, topY, 0, 0));
                mpb.SetVector("_RevealAxis", axis);
                mpb.SetVector("_ClipPlane", clip);
                if (r.transform == medallion) mpb.SetColor("_Emission", Bvr2Style.Ally * (0.25f + (Speaking ? 0.5f + 0.4f * Mathf.Sin(now * 9f) : 0.15f)));
                r.SetPropertyBlock(mpb);
                r.enabled = reveal > 0.001f;
            }
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
                // eye contact; a glance toward what is being explained
                Vector3 lookTarget = pointing ? Vector3.Lerp(head.position, pointAt.Value, 0.6f)
                    : presenting && docked && Mathf.Repeat(now, 5f) < 1.6f ? Vector3.Lerp(head.position, presentAt.Value, 0.5f) : head.position;
                Quaternion want = Quaternion.LookRotation(lookTarget - headPivot.position, body * Vector3.up);
                Quaternion local = Quaternion.Inverse(body) * want;
                var e = local.eulerAngles; float yaw = Mathf.Clamp(Mathf.DeltaAngle(0, e.y), -55, 55), pitch = Mathf.Clamp(Mathf.DeltaAngle(0, e.x), -25, 25);
                float nod = Speaking ? Mathf.Sin(now * 5.3f) * 2.5f + Mathf.Sin(now * 2.1f) * 2f : Mathf.Sin(now * 0.7f) * 1.5f;
                headPivot.localRotation = Bvr2Math.Damp(headPivot.localRotation, headRest * Quaternion.Euler(pitch + nod, yaw, Mathf.Sin(now * 0.9f) * 2f), 6f, dt);
            }
            // gestures
            if (handR) handR.localRotation = Bvr2Math.Damp(handR.localRotation, handRRest * Quaternion.Euler(pointing ? -55 : (Speaking ? Mathf.Sin(now * 3.1f) * 9 - 8 : Mathf.Sin(now) * 3), pointing ? 20 : 0, pointing ? 25 : 0), 5f, dt);
            if (handL) handL.localRotation = Bvr2Math.Damp(handL.localRotation, handLRest * Quaternion.Euler(Speaking ? Mathf.Sin(now * 2.6f + 1) * 8 - 6 : Mathf.Sin(now * 0.8f + 2) * 3, 0, 0), 5f, dt);
        }
        void Face(float dt, float now)
        {
            if (!face) return;
            // target expression weights (0..100)
            for (int i = 0; i < weights.Length; i++) weights[i] = 0;
            switch (mood)
            {
                case Mood.Smile: weights[0] = 70; break;
                case Mood.Surprised: weights[1] = 80; break;
                case Mood.Serious: weights[2] = 90; break;
                case Mood.Sad: weights[3] = 70; break;
                case Mood.Angry: weights[4] = 60; break;
                default: weights[0] = 25; break;
            }
            if (Speaking)
            {
                talkPhase += dt * (7.5f + Mathf.PerlinNoise(now * 2f, 0.3f) * 5f);
                float open = Mathf.Abs(Mathf.Sin(talkPhase)) * (0.55f + 0.45f * Mathf.PerlinNoise(now * 3f, 1.7f));
                weights[1] = Mathf.Max(weights[1], 18 + open * 42); // mouth movement from the "surprised" shape
                weights[0] = Mathf.Max(weights[0], 15 + open * 15);
            }
            // blinking
            if (now > nextBlink && blinkT < 0) { blinkT = 0; nextBlink = now + Random.Range(2.2f, 5f); }
            float blink = 0;
            if (blinkT >= 0) { blinkT += dt; blink = Mathf.Sin(Mathf.Clamp01(blinkT / 0.16f) * Mathf.PI) * 100; if (blinkT > 0.16f) blinkT = -1; }
            Set(bsSmile, weights[0], dt); Set(bsSurprised, weights[1], dt); Set(bsSerious, weights[2], dt); Set(bsSad, weights[3], dt); Set(bsAngry, weights[4], dt);
            if (bsBlink >= 0) face.SetBlendShapeWeight(bsBlink, blink);
        }

        void Set(int index, float target, float dt)
        {
            if (index < 0) return;
            face.SetBlendShapeWeight(index, Mathf.Lerp(face.GetBlendShapeWeight(index), target, 1f - Mathf.Exp(-dt * 14f)));
        }
    }
}
