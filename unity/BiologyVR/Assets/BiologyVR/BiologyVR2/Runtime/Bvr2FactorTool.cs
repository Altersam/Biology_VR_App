using System;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// BioTool ability "Фактор" (chapter 4). Paint mode: hold the trigger on a collagen patch in the tear —
    /// the beam lays the "glue" factor that platelets stick to. Held on healthy wall, it grows a clot where
    /// none is needed. Stitch mode: hold the trigger and sweep the beam across the tear, rim to rim —
    /// every crossing lays one fibrin thread over the plug.
    /// </summary>
    public sealed class Bvr2FactorTool : MonoBehaviour
    {
        public Bvr2Rig rig; public Bvr2Input input; public Bvr2Vessel vessel; public Bvr2Wound wound; public Bvr2Fx fx; public Bvr2Audio audioDirector;
        public Transform tip;
        public bool active, stitchMode;
        public float range = 18f, cone = 7f, paintSeconds = 0.9f, strayAfter = 1.0f, stitchStep = 0.14f;

        public event Action<Vector3> OnStray;
        public event Action<float, float> OnStitch;
        public int Strays { get; private set; }
        public int Stitches { get; private set; }
        public bool Painting { get; private set; }

        Bvr2Fx.Beam beam; float strayTime, strayCooldown, nextSound; Vector3 strayPoint;
        int lastSide; float lastDs; bool armed;

        public static readonly Color PaintColor = new Color(0.45f, 0.78f, 1f);
        public static readonly Color ThreadColor = new Color(1f, 0.92f, 0.6f);

        void OnDisable() { Release(); }

        void Update()
        {
            Painting = false;
            if (!active || !rig || !rig.rightHand || !input || !wound || !wound.Active) { Release(); return; }
            Vector3 origin = tip ? tip.position : rig.rightHand.position; Vector3 dir = rig.rightHand.forward;
            bool held = input.Tool;
            if (!held) { Release(); strayTime = Mathf.Max(0, strayTime - Time.deltaTime); lastSide = 0; armed = false; return; }
            if (stitchMode) Stitch(origin, dir); else Paint(origin, dir);
        }

        void Paint(Vector3 origin, Vector3 dir)
        {
            float dt = Time.deltaTime;
            var ray = new Ray(origin, dir);
            var p = wound.NearestPatch(ray, range, cone);
            if (p != null)
            {
                Painting = true; strayTime = Mathf.Max(0, strayTime - dt * 2f);
                wound.Paint(p, dt / paintSeconds);
                Shape(origin, dir, p.t.position + wound.Inward * 0.15f, PaintColor, 1f);
                if (Time.time > nextSound) { nextSound = Time.time + 0.32f; audioDirector?.PlayAt("BVR2_Paint", p.t.position, 0.45f); }
                if (Time.frameCount % 2 == 0) fx?.Burst(p.t.position + wound.Inward * 0.2f, PaintColor, 2, 0.6f, 0.03f, 0.4f);
                if (Time.frameCount % 6 == 0) Bvr2Haptics.Pulse(false, 0.12f, 0.03f);
                return;
            }
            // nothing to glue: the beam lands on the wall — if it lingers on healthy wall, a clot starts there
            if (vessel.RaycastWall(ray, range, out var hit, out _, out _))
            {
                Shape(origin, dir, hit, PaintColor, 0.55f);
                wound.ToTear(hit, out float ds, out float side);
                bool inTear = Mathf.Abs(ds) < 1.15f && Mathf.Abs(side) < 1.6f;
                if (!inTear && Time.time > strayCooldown)
                {
                    strayPoint = strayTime <= 0 ? hit : Vector3.Lerp(strayPoint, hit, dt * 4f);
                    strayTime += dt;
                    if (strayTime >= strayAfter)
                    {
                        strayTime = 0; strayCooldown = Time.time + 2.5f; Strays++;
                        wound.SpawnStray(strayPoint, 0.9f);
                        OnStray?.Invoke(strayPoint);
                    }
                }
            }
            else Shape(origin, dir, origin + dir * 6f, PaintColor, 0.35f);
        }

        void Stitch(Vector3 origin, Vector3 dir)
        {
            var ray = new Ray(origin, dir);
            if (!vessel.RaycastWall(ray, range, out var hit, out _, out _)) { Shape(origin, dir, origin + dir * 6f, ThreadColor, 0.35f); return; }
            wound.ToTear(hit, out float ds, out float side);
            bool near = Mathf.Abs(ds) < 1.3f && Mathf.Abs(side) < 3f;
            Shape(origin, dir, hit, ThreadColor, near ? 1f : 0.4f);
            if (!near) { lastSide = 0; armed = false; return; }
            int s = side > 0.55f ? 1 : side < -0.55f ? -1 : 0;
            if (s != 0)
            {
                if (armed && lastSide != 0 && s != lastSide) Lay(lastDs, ds);
                lastSide = s; lastDs = ds; armed = true;
            }
            if (Time.frameCount % 3 == 0) fx?.Burst(hit, ThreadColor, 1, 0.4f, 0.025f, 0.35f);
        }

        /// <summary>One fibrin thread across the plug (also used by tests).</summary>
        public void Lay(float ds0, float ds1)
        {
            Stitches++;
            wound.AddFibrin(ds0, ds1);
            wound.FibrinProgress += stitchStep;
            audioDirector?.PlayAt("BVR2_Stitch", wound.Centre, 0.8f);
            Bvr2Haptics.Pulse(false, 0.35f, 0.06f);
            fx?.Burst(wound.PointOn((ds0 + ds1) * 0.5f, 0, 0.4f), ThreadColor, 12, 1.2f, 0.03f, 0.5f);
            OnStitch?.Invoke(ds0, ds1);
        }

        /// <summary>Test hook: behave as if the beam lingered on healthy wall at a point.</summary>
        public void ForceStray(Vector3 wallPoint) { Strays++; wound.SpawnStray(wallPoint, 0.9f); OnStray?.Invoke(wallPoint); }

        public void ResetCounters() { Strays = 0; Stitches = 0; strayTime = 0; lastSide = 0; armed = false; }

        void Shape(Vector3 from, Vector3 dir, Vector3 to, Color c, float strength)
        {
            if (beam == null && fx) beam = fx.GetBeam();
            if (beam != null) Bvr2Fx.ShapeBeam(beam, from, dir, to, new Color(c.r, c.g, c.b, 0.8f * strength), 0.03f, 0.04f);
        }

        void Release() { if (beam != null) { fx?.ReleaseBeam(beam); beam = null; } }
    }
}
