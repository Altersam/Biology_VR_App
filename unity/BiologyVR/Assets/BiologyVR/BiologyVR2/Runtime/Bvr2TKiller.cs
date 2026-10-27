using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Ally T-killer lymphocyte (chapter 5). Keeps near the player; sent to a wall cell (by the player's
    /// Signal) it swims there, docks, and after a moment of "checking" hits it: an infected cell dies by
    /// apoptosis, a healthy one is damaged. Then it returns.
    /// </summary>
    public sealed class Bvr2TKiller : MonoBehaviour
    {
        public enum State { Follow, Go, Attack, Back }
        public Renderer body;
        public float speed = 6.5f, attackSeconds = 1.3f;
        public State Current { get; private set; } = State.Follow;
        public Bvr2WallCell Target { get; private set; }
        public bool Free => Current == State.Follow || Current == State.Back;
        public int Kills { get; private set; }

        Bvr2Vessel vessel; Transform player; Vector3 formation, vel; float attackT; int hint = -1; MaterialPropertyBlock mpb; Bvr2Fx fx;
        Vector3? point; System.Action onPoint; // a target without a wall cell (an infected spot)

        void Awake() { mpb = new MaterialPropertyBlock(); }

        public void Init(Bvr2Vessel v, Transform playerHead, Vector3 formationOffset, Bvr2Fx effects)
        {
            vessel = v; player = playerHead; formation = formationOffset; fx = effects;
            Current = State.Follow; Target = null; Kills = 0; vel = Vector3.zero; gameObject.SetActive(true);
        }

        public void Send(Bvr2WallCell cell)
        {
            if (cell == null) return;
            if (Target) Target.Targeted = false;
            Target = cell; cell.Targeted = true; point = null; onPoint = null; Current = State.Go;
        }

        /// <summary>Go to a point (an infected spot on the wall) and call back when the strike lands.</summary>
        public void SendTo(Vector3 p, System.Action onArrive)
        {
            if (Target) Target.Targeted = false;
            Target = null; point = p; onPoint = onArrive; Current = State.Go;
        }

        void Update()
        {
            float dt = Bvr2Time.Delta; if (dt <= 0 || !vessel) return;
            Vector3 goal = transform.position; float sp = speed;
            switch (Current)
            {
                case State.Follow:
                case State.Back:
                {
                    var f = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized; if (f.sqrMagnitude < 0.01f) f = player.forward;
                    var r = Vector3.Cross(Vector3.up, f);
                    goal = player.position + f * formation.z + r * formation.x + Vector3.up * formation.y;
                    if (Current == State.Back && (transform.position - goal).sqrMagnitude < 4f) Current = State.Follow;
                    break;
                }
                case State.Go:
                    if (point.HasValue) { goal = point.Value; sp *= 1.3f; if ((transform.position - goal).sqrMagnitude < 1.2f) { Current = State.Attack; attackT = 0; } break; }
                    if (!Target || Target.Current != Bvr2WallCell.State.Alive) { Release(); break; }
                    goal = Target.transform.position + Target.transform.up * 1.1f; sp *= 1.3f;
                    if ((transform.position - goal).sqrMagnitude < 0.6f) { Current = State.Attack; attackT = 0; }
                    break;
                case State.Attack:
                {
                    if (!Target && !point.HasValue) { Release(); break; }
                    Vector3 at = point ?? Target.transform.position; Vector3 up = Target ? Target.transform.up : (vessel.Centre(vessel.Locate(at, ref hint, out _)) - at).normalized;
                    goal = at + up * 0.9f;
                    attackT += dt;
                    if (Mathf.Repeat(attackT, 0.4f) < dt) fx?.Shock(at + up * 0.5f, up, new Color(0.7f, 0.8f, 1f), 0.1f, 1.2f, 0.35f);
                    if (attackT >= attackSeconds)
                    {
                        if (point.HasValue) { onPoint?.Invoke(); Kills++; }
                        else { bool wasInfected = Target.infected; Target.Hit(); if (wasInfected) Kills++; }
                        Release();
                    }
                    break;
                }
            }
            Vector3 to = goal - transform.position;
            Vector3 want = to.sqrMagnitude > 0.04f ? to.normalized * Mathf.Min(sp, to.magnitude * 2.2f) : Vector3.zero;
            vel = Bvr2Math.Damp(vel, want, 3.5f, dt);
            Vector3 p = transform.position + vel * dt;
            float s = vessel.Locate(p, ref hint, out var radial); float rr = radial.magnitude, lim = vessel.FreeRadius(s, vessel.AngleOf(s, radial)) - 0.7f;
            if (rr > lim && rr > 0.01f) p -= radial / rr * (rr - lim);
            transform.position = p;
            if (vel.sqrMagnitude > 0.1f) transform.rotation = Bvr2Math.Damp(transform.rotation, Quaternion.LookRotation(vel), 4f, dt);
            if (body)
            {
                float busy = Current == State.Attack ? 1f : Current == State.Go ? 0.6f : 0.15f;
                mpb.Clear(); mpb.SetVector("_Inst", new Vector4(0.3f, Current == State.Attack ? 0.5f + 0.3f * Mathf.Sin(Time.time * 12f) : 0.1f, busy, 0.7f));
                body.SetPropertyBlock(mpb);
            }
        }

        void Release()
        {
            if (Target) Target.Targeted = false;
            Target = null; point = null; onPoint = null; Current = State.Back;
        }
    }
}
