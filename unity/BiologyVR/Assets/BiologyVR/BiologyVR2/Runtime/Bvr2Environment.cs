using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>Pushes the shared shader globals (headlamp, fog, heartbeat, constriction zones,
    /// alarm, wall window) once per frame. Lighting in the lumen is entirely shader-side.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class Bvr2Environment : MonoBehaviour
    {
        public Bvr2Organism organism;
        public Bvr2Vessel vessel;
        public Camera viewCamera;
        [Header("Look")]
        // a warm, softly lit vessel (not a horror tunnel): rosy depth instead of black, generous fill light
        public Color fogColor = new Color(0.28f, 0.07f, 0.09f);
        public float fogNear = 7f, fogFar = 72f, fogStrength = 0.9f;
        public float headlampRange = 46f;
        public Color ambient = new Color(1f, 0.62f, 0.6f);
        public float ambientStrength = 0.27f;
        public Color alarmColor = new Color(1f, 0.25f, 0.12f);
        [Range(0, 1)] public float alarm;
        [Header("Wall window (layer study)")]
        public bool windowOn; public float windowS, windowAngle01, windowHalfLength = 3.5f, windowHalfAngle01 = 0.07f;
        [Tooltip("Tear look: ragged rim + lens shape (chapter 4).")] public float windowRagged; public bool windowSlit;
        public Color windowEdge = new Color(0, 0, 0, 0);

        [Header("Focus (knowledge moments)")]
        [Range(0, 1)] public float focusDimTarget;
        float focusDim;
        public static Bvr2Environment Current { get; private set; }
        void OnEnable() { Current = this; }
        public void SetFocus(bool on) { focusDimTarget = on ? 1f : 0f; }

        public float FlowScroll { get; private set; }
        static readonly int Head = Shader.PropertyToID("_B2_Head"), HeadDir = Shader.PropertyToID("_B2_HeadDir"), FogC = Shader.PropertyToID("_B2_FogColor"),
            Fog = Shader.PropertyToID("_B2_Fog"), PulseId = Shader.PropertyToID("_B2_Pulse"), AlarmC = Shader.PropertyToID("_B2_AlarmColor"),
            Amb = Shader.PropertyToID("_B2_Ambient"), Zones = Shader.PropertyToID("_B2_Zones"), Window = Shader.PropertyToID("_B2_Window");
        readonly Vector4[] zoneData = new Vector4[4], plaqueA = new Vector4[2], plaqueB = new Vector4[2];
        static readonly int PlaqueAId = Shader.PropertyToID("_B2_Plaque"), PlaqueBId = Shader.PropertyToID("_B2_PlaqueB");

        void LateUpdate()
        {
            Bvr2Time.Tick();
            var cam = viewCamera ? viewCamera : Camera.main;
            if (cam)
            {
                Shader.SetGlobalVector(Head, new Vector4(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z, headlampRange));
                var f = cam.transform.forward; Shader.SetGlobalVector(HeadDir, new Vector4(f.x, f.y, f.z, 0));
                cam.backgroundColor = fogColor;
            }
            Shader.SetGlobalColor(FogC, fogColor);
            focusDim = Mathf.MoveTowards(focusDim, focusDimTarget, Time.unscaledDeltaTime * 2.5f);
            Shader.SetGlobalVector(Fog, new Vector4(fogNear, fogFar, fogStrength, focusDim));
            Shader.SetGlobalColor(Amb, new Color(ambient.r, ambient.g, ambient.b, ambientStrength));
            Shader.SetGlobalColor(AlarmC, alarmColor);
            float pulse = organism ? organism.Pulse : 0, phase = organism ? organism.HeartPhase : Time.time * 1.2f;
            if (organism) FlowScroll += organism.MeanSpeed * Bvr2Time.Delta;
            Shader.SetGlobalVector(PulseId, new Vector4(pulse, phase, FlowScroll, alarm));
            if (vessel)
                for (int i = 0; i < 4; i++)
                {
                    var z = vessel.zones[i];
                    zoneData[i] = z.active ? new Vector4(z.centre, z.halfWidth, z.factor, 0) : Vector4.zero;
                }
            Shader.SetGlobalVectorArray(Zones, zoneData);
            if (vessel)
                for (int i = 0; i < 2; i++)
                {
                    var p = i < vessel.plaques.Length ? vessel.plaques[i] : default;
                    bool on = p.active && p.height > 0.001f;
                    plaqueA[i] = on ? new Vector4(p.s, p.halfLength, Mathf.Repeat(p.angleDeg, 360f) / 360f, p.height) : Vector4.zero;
                    plaqueB[i] = on ? new Vector4(p.halfAngleDeg / 360f, p.rupture, p.reveal, 0) : Vector4.zero;
                }
            Shader.SetGlobalVectorArray(PlaqueAId, plaqueA); Shader.SetGlobalVectorArray(PlaqueBId, plaqueB);
            Shader.SetGlobalVector(Window, windowOn ? new Vector4(windowS, windowAngle01, windowHalfLength, windowHalfAngle01) : Vector4.zero);
            Shader.SetGlobalVector("_B2_WindowStyle", new Vector4(windowRagged, windowSlit ? 1 : 0, 0, 0));
            Shader.SetGlobalColor("_B2_WindowEdge", windowEdge);
        }

        void OnDisable() { Shader.SetGlobalVector(Window, Vector4.zero); Shader.SetGlobalVector(PulseId, Vector4.zero); Shader.SetGlobalVector("_B2_Field", Vector4.zero); if (Current == this) Current = null; }
    }
}
