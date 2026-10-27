using System;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// A piece of thrombus torn off and carried by the blood (embolus). It drifts with the flow toward
    /// the narrow branch. The player holds it with the BioTool beam (Притяжение): the beam slows it,
    /// and once caught the professor's thrombolytic dissolves it — as long as the beam stays on it.
    /// Every heartbeat yanks it, so the aim has to follow.
    /// </summary>
    public sealed class Bvr2Embolus : MonoBehaviour
    {
        public Bvr2Rig rig; public Bvr2Input input; public Bvr2Vessel vessel; public Bvr2Organism organism; public Bvr2Fx fx; public Bvr2Audio audioDirector;
        public Bvr2Clot clot; public Transform tip;
        public float range = 13f, aimCone = 9f, catchSeconds = 2.2f, lysisSeconds = 4f, speedFactor = 0.95f;
        public float r01 = 0.35f;

        public enum State { Idle, Drifting, Caught, Dissolved, Lodged }
        public State Current { get; private set; } = State.Idle;
        public float S { get; private set; }
        public float CatchProgress { get; private set; }   // 0..1
        public float Lysis { get; private set; }           // 0..1
        public bool Beamed { get; private set; }
        public float stopAtS = 1e9f;                       // where it gets stuck (narrow branch)
        public event Action OnCaught, OnEscaped, OnDissolved, OnLodged;

        float angle, offTime, yank; Vector3 yankOffset; Bvr2Fx.Beam beam; int lastBeat;

        public void Launch(float s, float angleDeg)
        {
            S = s; angle = angleDeg; Current = State.Drifting; transform.localScale = Vector3.one; CatchProgress = 0; Lysis = 0; offTime = 0; yankOffset = Vector3.zero;
            gameObject.SetActive(true);
            if (clot) { clot.growth = 1f; clot.dissolve = 0f; }
            transform.position = vessel.PointAt(S, vessel.Radius(S) * r01, angle);
            lastBeat = organism ? organism.Beats : 0;
        }

        public void Hide() { Current = State.Idle; gameObject.SetActive(false); ReleaseBeam(); }

        /// <summary>Tests / autopilot: count as aimed this frame.</summary>
        public bool forceAim;

        void Update()
        {
            if (Current == State.Idle || Current == State.Dissolved) return;
            float dt = Time.deltaTime;
            if (Current == State.Lodged) { transform.Rotate(Vector3.up, 10f * dt, Space.Self); return; }

            // aim test from the BioTool
            var hand = rig ? rig.rightHand : null;
            Vector3 origin = tip ? tip.position : (hand ? hand.position : transform.position);
            Vector3 to = transform.position - origin; float dist = to.magnitude;
            bool aimed = forceAim || (hand && input && input.Tool && dist < range && Vector3.Angle(hand.forward, to) < aimCone + Mathf.Atan2(1f, dist) * Mathf.Rad2Deg);
            Beamed = aimed;
            offTime = aimed ? 0 : offTime + dt;

            // heartbeat yanks
            if (organism && organism.Beats != lastBeat)
            {
                lastBeat = organism.Beats;
                yankOffset += UnityEngine.Random.onUnitSphere * (Current == State.Caught ? 0.9f : 0.4f);
                if (Current == State.Caught) Bvr2Haptics.Pulse(false, 0.45f, 0.08f);
            }
            yankOffset = Vector3.Lerp(yankOffset, Vector3.zero, dt * 1.8f);

            float mean = organism ? organism.MeanSpeed : 2.6f;
            float flow = vessel.FlowAt(S, vessel.Radius(S) * r01, mean).magnitude * speedFactor;
            float hold = 0f;
            if (Current == State.Drifting)
            {
                if (aimed) CatchProgress = Mathf.Min(1f, CatchProgress + dt / catchSeconds);
                else CatchProgress = Mathf.Max(0f, CatchProgress - dt / (catchSeconds * 0.6f));
                hold = aimed ? Mathf.Lerp(0.35f, 0.85f, CatchProgress) : 0f;
                if (CatchProgress >= 1f) { Current = State.Caught; audioDirector?.Play("SFX_Capture_Lock", 1f); Bvr2Haptics.Pulse(false, 0.7f, 0.15f); OnCaught?.Invoke(); }
            }
            else if (Current == State.Caught)
            {
                hold = aimed ? 0.97f : 0.5f;
                if (aimed) Lysis = Mathf.Min(1f, Lysis + dt / lysisSeconds);
                if (offTime > 0.8f)
                {
                    Current = State.Drifting; CatchProgress = 0.3f; audioDirector?.Play("SFX_Release", 0.9f);
                    fx?.Burst(transform.position, Bvr2Style.Danger, 18, 2f, 0.05f, 0.5f); OnEscaped?.Invoke();
                }
                if (clot) clot.dissolve = Lysis;
                if (Lysis >= 1f) { Dissolve(); return; }
            }
            S += flow * (1f - hold) * dt;
            angle += Mathf.Sin(Time.time * 0.7f) * 12f * dt;
            Vector3 home = vessel.PointAt(S, vessel.Radius(S) * r01, angle);
            transform.position = Vector3.Lerp(transform.position, home + yankOffset, 1f - Mathf.Exp(-dt * 6f));
            transform.Rotate(new Vector3(0.3f, 1f, 0.2f), (40f + flow * 20f) * dt, Space.Self);

            // the beam
            if (aimed && hand)
            {
                if (beam == null && fx) beam = fx.GetBeam();
                if (beam != null)
                {
                    var c = Current == State.Caught ? Color.Lerp(new Color(0.45f, 0.93f, 1f, 0.9f), new Color(0.5f, 1f, 0.6f, 0.9f), Lysis) : new Color(0.45f, 0.93f, 1f, 0.5f + 0.4f * CatchProgress);
                    Bvr2Fx.ShapeBeam(beam, origin, hand.forward, transform.position, c, 0.04f + 0.03f * CatchProgress, 0.08f);
                }
                if (audioDirector) audioDirector.FieldLevel = 0.5f + 0.4f * CatchProgress;
            }
            else ReleaseBeam();

            if (S >= stopAtS) Lodge();
        }

        void ReleaseBeam() { if (beam != null) { fx?.ReleaseBeam(beam); beam = null; } }

        void Dissolve()
        {
            Current = State.Dissolved; ReleaseBeam();
            fx?.Burst(transform.position, new Color(1f, 0.6f, 0.55f), 50, 2.6f, 0.06f, 0.9f);
            fx?.Burst(transform.position, Bvr2Style.Good, 30, 1.5f, 0.04f, 0.7f);
            fx?.Shock(transform.position, (rig ? rig.head.transform.position : transform.position) - transform.position, Bvr2Style.Good, 0.2f, 2.4f, 0.6f);
            audioDirector?.Play("SFX_Success_Step", 1f, 1.1f);
            Bvr2Haptics.Both(0.6f, 0.2f);
            gameObject.SetActive(false);
            OnDissolved?.Invoke();
        }

        void Lodge()
        {
            Current = State.Lodged; ReleaseBeam(); S = stopAtS;
            transform.position = vessel.Centre(S);
            if (clot) transform.localScale = Vector3.one * Mathf.Max(1f, vessel.Radius(S) * 2.2f / Mathf.Max(clot.size, 0.1f)); // plugs the branch
            fx?.Shock(transform.position, vessel.Tangent(S), Bvr2Style.Danger, 0.3f, 3.5f, 0.8f);
            audioDirector?.Play("SFX_Error", 1f, 0.8f);
            OnLodged?.Invoke();
        }
    }
}
