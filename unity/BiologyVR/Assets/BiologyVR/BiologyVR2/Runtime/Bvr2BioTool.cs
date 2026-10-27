using System;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Right-hand BioTool, ability "Притяжение" (attraction): hold the trigger on a cell, the beam
    /// pulls it out of the stream into your hand; keep holding and it is stored as a sample.
    /// Let go early and the cell drifts back into the flow.
    /// </summary>
    public sealed class Bvr2BioTool : MonoBehaviour
    {
        public Bvr2Rig rig; public Bvr2Input input; public Bvr2Blood blood; public Bvr2Audio audioDirector; public Bvr2Fx fx;
        public Transform tip; public Renderer emitterGlow;
        public Mesh redMesh, whiteMesh, plateletMesh;
        public Material redMaterial, whiteMaterial, plateletMaterial;
        public float range = 20f, cone = 4.5f, pullSpeed = 9f;
        public bool locked;
        /// <summary>Set by <see cref="Bvr2Abilities"/> while another ability is selected.</summary>
        public bool suspended;
        public int onlyKind = -1;

        public enum State { Idle, Pulling, Holding }
        public State Current { get; private set; }
        public CellKind HeldKind { get; private set; }
        /// <summary>Cell stored as a sample.</summary>
        public event Action<CellKind, Vector3> OnSample;
        public event Action<CellKind> OnGrab;

        int cellIndex = -1; Transform proxy; Vector3 proxyVel; float holdTime; Bvr2Fx.Beam beam; MaterialPropertyBlock mpb;

        void Awake() { mpb = new MaterialPropertyBlock(); }

        /// <summary>Test / autopilot hook.</summary>
        public void ForceSample(CellKind kind, Vector3 at) => OnSample?.Invoke(kind, at);

        void Update()
        {
            var hand = rig ? rig.rightHand : null; if (!hand) return;
            Vector3 origin = tip ? tip.position : hand.position; Vector3 dir = hand.forward;
            bool held = input && input.Tool && !locked && !suspended;
            float glow = 0.3f;
            switch (Current)
            {
                case State.Idle:
                    if (held && input.ToolDown || held && cellIndex < 0 && Time.frameCount % 3 == 0)
                    {
                        if (blood && blood.Pick(new Ray(origin, dir), range, cone, out int idx, out var kind, out _, onlyKind))
                            StartPull(idx, kind);
                        else if (input.ToolDown) audioDirector?.Play("UI_Hover", 0.5f, 0.8f);
                    }
                    if (held) glow = 0.6f;
                    break;
                case State.Pulling:
                {
                    if (!held) { Drop(); break; }
                    Vector3 hold = origin + dir * 0.32f;
                    Vector3 to = hold - proxy.position;
                    proxyVel = Bvr2Math.Damp(proxyVel, to.normalized * Mathf.Min(pullSpeed, to.magnitude * 6f + 2f), 6f, Time.deltaTime);
                    proxy.position += proxyVel * Time.deltaTime;
                    proxy.Rotate(Vector3.up, 120 * Time.deltaTime, Space.Self);
                    ShapeBeam(origin, dir, proxy.position, 1f);
                    glow = 1f;
                    if (to.magnitude < 0.12f) { Current = State.Holding; holdTime = 0; audioDirector?.Play("SFX_Capture_Lock", 0.9f); Bvr2Haptics.Pulse(false, 0.5f, 0.1f); OnGrab?.Invoke(HeldKind); }
                    break;
                }
                case State.Holding:
                {
                    if (!held && holdTime < 0.45f) { Drop(); break; }
                    holdTime += Time.deltaTime;
                    proxy.position = Vector3.Lerp(proxy.position, origin + dir * 0.32f, Time.deltaTime * 14f);
                    proxy.Rotate(Vector3.up, 90 * Time.deltaTime, Space.Self);
                    ShapeBeam(origin, dir, proxy.position, 0.5f);
                    glow = 1f;
                    if (holdTime >= 0.45f) Store();
                    break;
                }
            }
            if (emitterGlow) { mpb.SetColor("_Color", Bvr2Style.Ally * glow); emitterGlow.SetPropertyBlock(mpb); }
            if (audioDirector) audioDirector.FieldLevel = Current == State.Idle ? 0 : 0.6f;
        }

        void StartPull(int idx, CellKind kind)
        {
            cellIndex = idx; HeldKind = kind; Current = State.Pulling;
            proxy = new GameObject("Held " + kind).transform;
            var mf = proxy.gameObject.AddComponent<MeshFilter>(); var mr = proxy.gameObject.AddComponent<MeshRenderer>();
            mf.sharedMesh = kind == CellKind.RedCell ? redMesh : kind == CellKind.WhiteCell ? whiteMesh : plateletMesh;
            mr.sharedMaterial = kind == CellKind.RedCell ? redMaterial : kind == CellKind.WhiteCell ? whiteMaterial : plateletMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var b = new MaterialPropertyBlock(); b.SetVector("_Inst", new Vector4(1, 0.8f, 0, 0)); mr.SetPropertyBlock(b);
            proxy.position = blood.PositionOf(idx); proxy.rotation = blood.RotationOf(idx);
            float size = kind == CellKind.RedCell ? blood.redSize * 0.5f : kind == CellKind.WhiteCell ? blood.whiteSize * 0.5f : blood.plateletSize;
            // shrink while it approaches so it fits in front of the face
            proxy.localScale = Vector3.one * size * (kind == CellKind.WhiteCell ? 0.35f : kind == CellKind.RedCell ? 0.38f : 0.7f);
            blood.Take(idx, -1);
            proxyVel = Vector3.zero;
            beam = fx ? fx.GetBeam() : null;
            audioDirector?.Play("SFX_Launch", 0.6f, 1.3f);
            Bvr2Haptics.Pulse(false, 0.3f, 0.08f);
        }

        void ShapeBeam(Vector3 origin, Vector3 dir, Vector3 to, float strength)
        {
            if (beam != null) Bvr2Fx.ShapeBeam(beam, origin, dir, to, new Color(0.45f, 0.93f, 1f, 0.75f * strength), 0.035f, 0.03f);
        }

        void Drop()
        {
            if (proxy) { fx?.Burst(proxy.position, Bvr2Style.AllySoft, 10, 1.2f, 0.04f, 0.5f); Destroy(proxy.gameObject); }
            if (cellIndex >= 0) blood.Take(cellIndex, 0.05f); // reappears in the stream
            Reset();
            audioDirector?.Play("SFX_Release", 0.6f);
        }

        void Store()
        {
            Vector3 p = proxy.position;
            fx?.Burst(p, Bvr2Style.Ally, 26, 1.8f, 0.05f, 0.6f);
            fx?.Shock(p, rig.head.transform.forward, Bvr2Style.Ally, 0.05f, 0.5f, 0.4f);
            audioDirector?.Play("SFX_Success_Step", 0.8f);
            Bvr2Haptics.Pulse(false, 0.7f, 0.12f);
            Destroy(proxy.gameObject);
            if (cellIndex >= 0) blood.Take(cellIndex, 4f);
            var kind = HeldKind; Reset();
            OnSample?.Invoke(kind, p);
        }

        void Reset()
        {
            Current = State.Idle; cellIndex = -1; proxy = null;
            if (beam != null) { fx?.ReleaseBeam(beam); beam = null; }
        }
    }
}
