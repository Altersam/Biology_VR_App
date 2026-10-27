using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Ally phagocyte (macrophage). Keeps near the player; on a Signal it swims to the point and hunts
    /// viruses there, preferring antibody-tagged ones (opsonization). It wraps a virus, digests it,
    /// and an untagged virus may slip away. Pseudopods (cell shader "morph") show how busy it is.
    /// </summary>
    public sealed class Bvr2Phagocyte : MonoBehaviour
    {
        public Renderer body;
        public float speed = 5.5f, huntRadius = 6f, autoTagged = 7f, autoUntagged = 2.6f;
        [Tooltip("Body colour override (a = 0: material colour). Chapter 5 tints two of them as neutrophils.")] public Color tint = new Color(0, 0, 0, 0);
        public enum State { Follow, Seek, Hunt, Engulf }
        public State Current { get; private set; } = State.Follow;
        public int Eaten { get; private set; }

        Bvr2Immune immune; Bvr2Vessel vessel; Transform player;
        Vector3 seekPoint, vel, formation; float seekUntil, boostUntil, morph, hint = -1; Bvr2Virus prey; MaterialPropertyBlock mpb; int locateHint = -1;

        public void Init(Bvr2Immune owner, Transform playerHead, Vector3 formationOffset)
        {
            immune = owner; vessel = owner.vessel; player = playerHead; formation = formationOffset;
            Current = State.Follow; prey = null; Eaten = 0; gameObject.SetActive(true);
        }

        void Awake() { mpb = new MaterialPropertyBlock(); }

        /// <summary>Chemotaxis: go to the signal (a strong, on-beat signal makes it hurry).</summary>
        public void Signal(Vector3 point, bool strong)
        {
            if (Current == State.Engulf) return;
            seekPoint = point; seekUntil = Time.time + 9f; Current = State.Seek; prey = null;
            if (strong) boostUntil = Time.time + 3f;
        }

        public void Released(Bvr2Virus v) { if (prey == v) prey = null; Current = State.Hunt; seekUntil = Time.time + 3f; }
        public void Digested(Bvr2Virus v) { Eaten++; prey = null; Current = seekUntil > Time.time ? State.Hunt : State.Follow; immune.fx?.Burst(transform.position, Bvr2Style.Good, 16, 1.4f, 0.05f, 0.5f); }

        void Update()
        {
            float dt = Bvr2Time.Delta; if (dt <= 0 || !immune) return;
            float sp = speed * (Time.time < boostUntil ? 1.7f : 1f);
            Vector3 goal = transform.position;
            switch (Current)
            {
                case State.Follow:
                {
                    var f = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized; if (f.sqrMagnitude < 0.01f) f = player.forward;
                    var r = Vector3.Cross(Vector3.up, f);
                    goal = player.position + f * formation.z + r * formation.x + Vector3.up * formation.y;
                    // opportunistic: snap up viruses that come close (tagged ones from farther away)
                    var v = immune.NearestVirus(transform.position, autoTagged, true, immune.autoEatDebris, true) ?? immune.NearestVirus(transform.position, autoUntagged, false, immune.autoEatDebris, true);
                    if (v) { prey = v; Current = State.Hunt; seekUntil = Time.time + 4f; }
                    break;
                }
                case State.Seek:
                {
                    goal = seekPoint;
                    if ((transform.position - seekPoint).sqrMagnitude < 9f || Time.time > seekUntil) { Current = State.Hunt; seekUntil = Time.time + 6f; }
                    var v = immune.NearestVirus(transform.position, 3.5f, true, true, true); if (v) { prey = v; Current = State.Hunt; }
                    break;
                }
                case State.Hunt:
                {
                    if (!prey || !prey.Alive || prey.Engulfer) prey = immune.NearestVirus(transform.position, huntRadius, true, true, true) ?? immune.NearestVirus(transform.position, huntRadius * 0.6f, false, true, true);
                    if (!prey) { if (Time.time > seekUntil) Current = State.Follow; goal = seekPoint; break; }
                    goal = prey.transform.position;
                    if ((transform.position - goal).sqrMagnitude < 1.6f * 1.6f) { prey.BeginEngulf(this); Current = State.Engulf; immune.audioDirector?.PlayAt("BVR2_Gulp", transform.position, 0.8f); }
                    break;
                }
                case State.Engulf:
                    if (!prey || prey.Current != Bvr2Virus.State.Engulfed) { prey = null; Current = State.Hunt; }
                    goal = transform.position;
                    break;
            }
            Vector3 to = goal - transform.position;
            Vector3 want = to.sqrMagnitude > 0.04f ? to.normalized * Mathf.Min(sp, to.magnitude * 2f) : Vector3.zero;
            vel = Bvr2Math.Damp(vel, want, 3f, dt);
            Vector3 p = transform.position + vel * dt;
            // stay inside the lumen
            float s = vessel.Locate(p, ref locateHint, out var radial); float rr = radial.magnitude, lim = vessel.FreeRadius(s, vessel.AngleOf(s, radial)) - 1.2f;
            if (rr > lim && rr > 0.01f) p -= radial / rr * (rr - lim);
            transform.position = p;
            if (vel.sqrMagnitude > 0.1f) transform.rotation = Bvr2Math.Damp(transform.rotation, Quaternion.LookRotation(vel), 3f, dt);
            morph = Mathf.MoveTowards(morph, Current == State.Follow ? 0.15f : Current == State.Engulf ? 1f : 0.6f, dt * 1.5f);
            if (body)
            {
                body.GetPropertyBlock(mpb); mpb.SetVector("_Inst", new Vector4(0.6f, Current == State.Seek ? 0.25f : 0f, morph, hint < 0 ? (hint = Random.value) : hint));
                if (tint.a > 0) mpb.SetColor("_BaseColor", tint);
                body.SetPropertyBlock(mpb);
            }
        }
    }
}
