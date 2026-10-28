using System;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Free swimming in the bloodstream: thrust relative to the head, the current carries you,
    /// the heartbeat surges, the wall pushes back softly. Boost for dynamics, magnetic anchor
    /// for calm episodes. Comfort: smooth acceleration, vignette at high optic flow, snap turn.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public sealed class Bvr2Swimmer : MonoBehaviour
    {
        public Bvr2Input input;
        public Bvr2Rig rig;
        public Bvr2Vessel vessel;
        public Bvr2Organism organism;
        [Header("Feel")]
        public float thrust = 7.5f;           // m/s² while the stick is held
        public float maxSwim = 4.2f;          // speed relative to the plasma
        public float boostSpeed = 11f;
        public float boostTime = 0.55f, boostCooldown = 1.6f;
        public float plasmaDrag = 1.7f;       // how quickly you match the current when idle
        public float currentShare = 0.3f;     // fraction of the local blood speed that carries the player
        public float wallMargin = 1.1f;
        [Tooltip("0..1 comfort mode slows everything a bit.")]
        public bool comfortMode;
        [Tooltip("Soft limits along the artery for an arena (s, metres); min >= max = off.")]
        public float regionMin, regionMax;
        [Tooltip("Extra force from the scenario (the pull of a leak), m/s².")]
        public Vector3 externalPull;
        [Header("Room mode (the lab prelude): walk on a floor, no blood current, no vessel walls")]
        public bool roomMode; public Vector3 roomCentre; public float roomRadius = 5.5f, walkSpeed = 2.2f;
        /// <summary>Room mode with several rooms: walkable rectangles (x, z) in roomSpace local coordinates; replaces the circle when set.</summary>
        public Rect[] roomRects; public Transform roomSpace;
        public bool frozen;

        public Vector3 Velocity { get; private set; }
        public float S { get; private set; }
        public float RadialDistance { get; private set; }
        public float SpeedOverWall => Velocity.magnitude;
        public bool Boosting => Time.time < boostUntil;
        public float BoostReady => Mathf.Clamp01((Time.time - boostUntil) / boostCooldown);
        public bool Anchored => anchor != null;
        public Transform AnchorPoint => anchor;
        public event Action OnBoost, OnWallBump, OnAnchored, OnReleased;

        Transform anchor; Vector3 anchorOffset; int hint; float boostUntil = -10, lastBump;
        Vector3 boostDir;

        public Vector3 HeadPosition => rig && rig.head ? rig.head.transform.position : transform.position;

        /// <summary>Keeps a point inside the union of the walkable rectangles (nearest point when outside).</summary>
        public Vector3 ClampToRooms(Vector3 world)
        {
            Vector3 l = roomSpace.InverseTransformPoint(world);
            Vector2 q = new Vector2(l.x, l.z), best = q; float bestD = float.MaxValue;
            foreach (var r in roomRects)
            {
                var c = new Vector2(Mathf.Clamp(q.x, r.xMin, r.xMax), Mathf.Clamp(q.y, r.yMin, r.yMax));
                float d = (c - q).sqrMagnitude; if (d < bestD) { bestD = d; best = c; if (d == 0) break; }
            }
            l.x = best.x; l.z = best.y;
            Vector3 w = roomSpace.TransformPoint(l); w.y = world.y; return w;
        }

        public void Teleport(Vector3 world, Vector3 lookDir)
        {
            var delta = world - HeadPosition; transform.position += delta; Velocity = Vector3.zero;
            if (rig) rig.LookAt(HeadPosition + lookDir * 10f);
            hint = -1; S = vessel ? vessel.Locate(HeadPosition, ref hint, out _) : 0;
        }

        public void AttachTo(Transform point)
        {
            anchor = point; Velocity = Vector3.zero;
            anchorOffset = HeadPosition - point.position;
            OnAnchored?.Invoke();
        }

        public void Detach() { if (!anchor) return; anchor = null; OnReleased?.Invoke(); }
        bool boostRequest;
        /// <summary>Boost as if the button was pressed (tests, tutorials).</summary>
        public void RequestBoost() { boostRequest = true; }

        void Update()
        {
            if (!vessel || !input || !rig) return;
            float dt = Time.deltaTime; if (dt <= 0) return;
            Vector3 headPos = HeadPosition;
            S = vessel.Locate(headPos, ref hint, out var radial);
            RadialDistance = radial.magnitude;
            if (frozen) { Velocity = Vector3.zero; rig.SetVignette(0); return; }
            if (roomMode)
            {
                var h = rig.head.transform;
                Vector3 fl = Vector3.ProjectOnPlane(h.forward, Vector3.up).normalized, rt = Vector3.ProjectOnPlane(h.right, Vector3.up).normalized;
                Vector3 w = fl * input.Move.y + rt * input.Move.x; if (w.sqrMagnitude > 1f) w.Normalize();
                Velocity = Bvr2Math.Damp(Velocity, w * walkSpeed, 8f, dt);
                Vector3 p = transform.position + Velocity * dt;
                if (roomRects != null && roomRects.Length > 0 && roomSpace) p = ClampToRooms(p);
                else
                {
                    Vector3 flat = p - roomCentre; flat.y = 0;
                    if (flat.magnitude > roomRadius) p -= flat.normalized * (flat.magnitude - roomRadius);
                }
                transform.position = p; rig.SetVignette(0);
                return;
            }

            if (anchor)
            {
                // Magnet: glide to the pad and stay, the current no longer drags.
                Vector3 goal = anchor.position + anchorOffset.normalized * Mathf.Min(anchorOffset.magnitude, 1.6f);
                Vector3 to = goal - headPos;
                Velocity = Bvr2Math.Damp(Velocity, to * 3f, 6f, dt);
                transform.position += Velocity * dt;
                rig.SetVignette(Mathf.InverseLerp(2.5f, 7f, Velocity.magnitude) * 0.6f);
                return;
            }

            var head = rig.head.transform;
            Vector3 fwd = head.forward, right = head.right, up = head.up;
            Vector3 wish = fwd * input.Move.y + right * input.Move.x + up * input.Vertical;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            float comfort = comfortMode ? 0.7f : 1f;

            // Blood current at the player's position (heartbeat makes it surge).
            float mean = organism ? organism.MeanSpeed : 2.6f;
            Vector3 current = vessel.FlowAt(S, RadialDistance, mean) * currentShare * Bvr2Time.Scale;

            if ((input.BoostDown || boostRequest) && BoostReady >= 1f)
            {
                boostDir = wish.sqrMagnitude > 0.04f ? wish.normalized : fwd;
                boostUntil = Time.time + boostTime; OnBoost?.Invoke(); boostRequest = false;
                Bvr2Haptics.Both(0.35f, 0.12f);
            }

            Vector3 rel = Velocity - current;
            if (Boosting)
            {
                float k = Mathf.Clamp01((boostUntil - Time.time) / boostTime);
                rel = Vector3.Lerp(rel, boostDir * boostSpeed * comfort, 1f - Mathf.Exp(-10f * dt * (0.3f + k)));
            }
            else
            {
                rel += wish * thrust * comfort * dt;
                if (wish.sqrMagnitude < 0.01f) rel *= Mathf.Exp(-plasmaDrag * dt);
                else rel *= Mathf.Exp(-0.35f * dt);
                rel = Vector3.ClampMagnitude(rel, maxSwim * comfort);
            }
            Velocity = current + rel;
            if (externalPull.sqrMagnitude > 0) Velocity += externalPull * dt;

            // Soft wall: push back toward the centre, keep a little bounce, notify for feedback.
            Vector3 next = headPos + Velocity * dt;
            float sNext = vessel.Locate(next, ref hint, out var radialNext);
            float rn = radialNext.magnitude;
            float limit = (rn > 0.01f ? vessel.FreeRadius(sNext, vessel.AngleOf(sNext, radialNext)) : vessel.Radius(sNext)) - wallMargin;
            if (rn > limit && rn > 0.01f)
            {
                Vector3 outward = radialNext / rn;
                float vOut = Vector3.Dot(Velocity, outward);
                if (vOut > 0) Velocity -= outward * vOut * 1.35f;
                next -= outward * (rn - limit);
                Velocity -= outward * (rn - limit) * 4f * dt;
                if (vOut > 1.2f && Time.time - lastBump > 0.6f) { lastBump = Time.time; OnWallBump?.Invoke(); Bvr2Haptics.Both(Mathf.Clamp01(vOut / 6f), 0.08f); }
            }
            // Arena limits (a battle stays in one stretch of the artery).
            if (regionMax > regionMin)
            {
                if (sNext < regionMin) Velocity += vessel.Tangent(sNext) * (regionMin - sNext) * 4f * dt;
                if (sNext > regionMax) Velocity -= vessel.Tangent(sNext) * (sNext - regionMax) * 4f * dt;
            }
            // Ends of the vessel are closed for the chapter.
            if (sNext < 2f) { Velocity += vessel.Tangent(2f) * (2f - sNext) * 4f * dt; }
            if (sNext > vessel.Length - 3f) { Velocity -= vessel.Tangent(sNext) * (sNext - (vessel.Length - 3f)) * 5f * dt; }
            transform.position += next - headPos;

            // Comfort: vignette rises with optic flow over the wall and during boosts.
            float flow = Velocity.magnitude;
            rig.SetVignette(Mathf.Clamp01(Mathf.InverseLerp(5.5f, 11f, flow) + (Boosting ? 0.35f : 0f)));
        }
    }
}
