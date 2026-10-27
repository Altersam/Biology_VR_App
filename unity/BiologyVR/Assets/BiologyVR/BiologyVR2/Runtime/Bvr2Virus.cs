using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// One virus particle. It drifts with the blood toward the wall; once it touches it, it starts to
    /// infect an endothelial cell (a few seconds in which it can still be eaten). Antibodies cover it
    /// progressively (<see cref="tag"/>); a tagged virus is eaten fast and cannot escape a phagocyte.
    /// </summary>
    public sealed class Bvr2Virus : MonoBehaviour
    {
        public enum State { Free, Infecting, Engulfed, Dead }
        public Renderer body;
        public Transform antibodies;          // children appear as the tag grows
        [Range(0, 1)] public float tag;
        public float radius = 0.45f;
        [Tooltip("Cell debris (chapter 4): sits in place, never infects; phagocytes clear it.")] public bool debris;
        [Tooltip("Armoured \"hedgehog\" (chapter 5): ordinary antibodies slide off; only specific ones work.")] public bool armored;
        /// <summary>Covered by specific antibodies: harmless, about to be cleared.</summary>
        public bool Neutralized { get; private set; }
        public State Current { get; private set; } = State.Free;
        public bool Tagged => tag >= 0.999f;
        public bool Alive => Current == State.Free || Current == State.Infecting;
        public float InfectProgress { get; private set; }
        public Bvr2Phagocyte Engulfer { get; private set; }
        public float TaggedAt { get; private set; } = -1;

        Bvr2Immune immune; Bvr2Vessel vessel; Bvr2Organism organism;
        float s, angle, r01, targetS, targetAngle, seek, phase, engulfT, engulfTime, infectTime;
        bool escapeRoll; Vector3 wobble; MaterialPropertyBlock mpb; float spin; Transform absorbInto; Vector3 anchorPos; bool anchored; float neutralT, anchoredT;
        /// <summary>Debris: seconds before it is cleared on its own (0 = stays until eaten).</summary>
        public float fadeAfter;

        public void Init(Bvr2Immune owner, float s0, float angle0, float r0, float seekSpeed, float infectSeconds)
        {
            immune = owner; vessel = owner.vessel; organism = owner.organism;
            s = s0; angle = angle0; r01 = r0; seek = seekSpeed; infectTime = infectSeconds;
            targetS = s0 + Random.Range(-4f, 8f); targetAngle = angle0 + Random.Range(-50f, 50f);
            tag = 0; TaggedAt = -1; InfectProgress = 0; Engulfer = null; absorbInto = null; Current = State.Free; phase = Random.value * 10f; anchored = false; Neutralized = false; neutralT = 0;
            transform.localScale = Vector3.one * radius; spin = Random.Range(20f, 60f);
            transform.position = vessel.PointAt(s, vessel.Radius(s) * r01, angle);
            if (antibodies) for (int i = 0; i < antibodies.childCount; i++) antibodies.GetChild(i).gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        void Awake() { mpb = new MaterialPropertyBlock(); }

        /// <summary>Debris: placed at a point, drifts only a little; phagocytes take it only after a Signal.</summary>
        public void InitAnchored(Bvr2Immune owner, Vector3 position)
        {
            Init(owner, 0, 0, 0, 0, 999f);
            anchored = true; anchorPos = position; transform.position = position; anchoredT = 0;
            transform.localScale = Vector3.one * radius * Random.Range(0.8f, 1.25f); spin = Random.Range(8f, 25f);
        }

        /// <summary>Antibodies land on the virus.</summary>
        public void AddTag(float amount)
        {
            if (!Alive || Tagged || armored) return;
            tag = Mathf.Clamp01(tag + amount);
            if (antibodies)
            {
                int show = Mathf.CeilToInt(tag * antibodies.childCount);
                for (int i = 0; i < antibodies.childCount; i++) antibodies.GetChild(i).gameObject.SetActive(i < show);
            }
            if (Tagged) { TaggedAt = Time.time; immune.NotifyTagged(this); }
        }

        /// <summary>Specific antibodies cover it completely: it can no longer enter a cell and is cleared.</summary>
        public void Neutralize()
        {
            if (!Alive || Neutralized || Engulfer) return;
            // antibodies also stop a virion that is only attaching to a cell: it cannot get in any more
            if (Current == State.Infecting) { Current = State.Free; r01 = Mathf.Clamp(r01 * 0.8f, 0.2f, 0.85f); }
            Neutralized = true; tag = 1f; TaggedAt = Time.time; neutralT = 0;
            if (antibodies) for (int i = 0; i < antibodies.childCount; i++) antibodies.GetChild(i).gameObject.SetActive(true);
            immune.NotifyTagged(this);
        }

        /// <summary>A phagocyte wraps around it. Tagged: quick and certain. Untagged: slow and it may escape.</summary>
        public void BeginEngulf(Bvr2Phagocyte p)
        {
            if (!Alive || Engulfer) return;
            Engulfer = p; Current = State.Engulfed; engulfT = 0;
            engulfTime = Tagged ? 0.7f : debris ? 1.1f : 2.4f; escapeRoll = !Tagged && !debris && Random.value < 0.4f;
        }

        /// <summary>The player (synchronised as a macrophage) swallows it: pulled into the hand, gone.</summary>
        public void Absorb(Transform into)
        {
            if (!Alive || Engulfer) return;
            absorbInto = into; Current = State.Engulfed; engulfT = 0; engulfTime = 0.35f; escapeRoll = false;
        }

        public void Kill(bool byPlayer)
        {
            if (Current == State.Dead) return;
            Current = State.Dead; immune.NotifyKilled(this, byPlayer); gameObject.SetActive(false);
        }

        void Update()
        {
            float dt = Bvr2Time.Delta; if (dt <= 0) return;
            transform.Rotate(new Vector3(0.3f, 1f, 0.15f), spin * dt, Space.Self);
            switch (Current)
            {
                case State.Free when Neutralized:
                {
                    // covered by antibodies: it drifts, clumps and is gone
                    neutralT += dt; phase += dt;
                    s += vessel.FlowAt(s, vessel.Radius(s) * r01, organism ? organism.MeanSpeed : 2.6f).magnitude * 0.1f * dt;
                    transform.position = vessel.PointAt(s, Mathf.Min(vessel.Radius(s) * r01, vessel.FreeRadius(s, angle) - radius), angle) + wobble;
                    transform.localScale = Vector3.one * radius * Mathf.Lerp(1f, 0.2f, Mathf.Clamp01((neutralT - 0.6f) / 0.6f));
                    if (neutralT > 1.2f) Kill(true);
                    break;
                }
                case State.Free when anchored:
                    phase += dt; anchoredT += dt;
                    transform.position = anchorPos + new Vector3(Mathf.Sin(phase * 0.9f), Mathf.Sin(phase * 0.7f + 1f), Mathf.Sin(phase * 1.1f + 2f)) * 0.12f;
                    // remains nobody came for are cleared quietly after a while (other phagocytes, off screen)
                    if (fadeAfter > 0 && anchoredT > fadeAfter) { transform.localScale *= 1f - Mathf.Clamp01(dt * 1.5f); if (anchoredT > fadeAfter + 2f) { Current = State.Dead; gameObject.SetActive(false); } }
                    break;
                case State.Free:
                {
                    // drift with the stream (like the player) and steer toward a spot on the wall
                    float mean = organism ? organism.MeanSpeed : 2.6f;
                    float R = vessel.Radius(s);
                    s += vessel.FlowAt(s, R * r01, mean).magnitude * 0.22f * dt;
                    // steer in vessel coordinates toward the chosen spot on the wall
                    r01 = Mathf.MoveTowards(r01, 1f, seek * dt / Mathf.Max(R, 1f));
                    angle = Mathf.MoveTowardsAngle(angle, targetAngle, seek * dt * 12f);
                    targetS += vessel.FlowAt(s, R * r01, mean).magnitude * 0.22f * dt; // the spot it aims at moves on with it
                    s = Mathf.MoveTowards(s, targetS, seek * 0.5f * dt);
                    phase += dt;
                    wobble = new Vector3(Mathf.Sin(phase * 1.7f), Mathf.Sin(phase * 1.3f + 1f), Mathf.Sin(phase * 2.1f + 2f)) * 0.15f;
                    float rr = Mathf.Min(R * r01, vessel.FreeRadius(s, angle) - radius * 0.6f);
                    transform.position = vessel.PointAt(s, rr, angle) + wobble;
                    if (rr >= vessel.FreeRadius(s, angle) - radius * 0.65f) { Current = State.Infecting; InfectProgress = 0; immune.NotifyLanded(this); }
                    break;
                }
                case State.Infecting:
                {
                    InfectProgress += dt / infectTime;
                    float sink = Mathf.SmoothStep(0, 1, InfectProgress) * radius * 0.9f;
                    transform.position = vessel.PointAt(s, vessel.FreeRadius(s, angle) - radius * 0.6f + sink, angle);
                    transform.localScale = Vector3.one * radius * (1f + 0.08f * Mathf.Sin(Time.time * 9f));
                    if (InfectProgress >= 1f) { Current = State.Dead; immune.NotifyInfected(this, s, angle); gameObject.SetActive(false); }
                    break;
                }
                case State.Engulfed:
                {
                    if (absorbInto)
                    {
                        engulfT += Time.deltaTime; float ka = Mathf.Clamp01(engulfT / engulfTime);
                        transform.position = Vector3.Lerp(transform.position, absorbInto.position, ka);
                        transform.localScale = Vector3.one * radius * (1f - ka * 0.9f);
                        if (ka >= 1f) { absorbInto = null; Kill(true); }
                        break;
                    }
                    if (!Engulfer) { Current = State.Free; break; }
                    engulfT += dt;
                    float k = Mathf.Clamp01(engulfT / engulfTime);
                    Vector3 c = Engulfer.transform.position;
                    transform.position = Vector3.Lerp(transform.position, c + Random.insideUnitSphere * 0.08f * (1f - k), dt * 6f);
                    transform.localScale = Vector3.one * radius * Mathf.Lerp(1f, 0.25f, k);
                    if (escapeRoll && k > 0.75f)
                    {
                        // an unmarked virus slips out of the phagocyte
                        Current = State.Free; Engulfer.Released(this); Engulfer = null; transform.localScale = Vector3.one * radius;
                        r01 = Mathf.Clamp(r01 * 0.7f, 0.2f, 0.8f); targetS = s + Random.Range(2f, 8f); targetAngle = angle + Random.Range(-90f, 90f);
                        immune.NotifyEscaped(this);
                        break;
                    }
                    if (k >= 1f) { var e = Engulfer; Engulfer = null; Kill(false); e.Digested(this); }
                    break;
                }
            }
            if (body)
            {
                body.GetPropertyBlock(mpb);
                float hl = Tagged ? 0.55f + 0.25f * Mathf.Sin(Time.time * 6f) : tag * 0.4f;
                mpb.SetVector("_Inst", new Vector4(0.4f, hl, Current == State.Infecting ? InfectProgress : 0f, phase));
                body.SetPropertyBlock(mpb);
            }
        }

        /// <summary>Vessel coordinates (for the wall spot when it infects).</summary>
        public float S => s;
        public float Angle => angle;
    }
}
