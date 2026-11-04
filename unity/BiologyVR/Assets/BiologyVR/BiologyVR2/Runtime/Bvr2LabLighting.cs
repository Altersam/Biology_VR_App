using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Lighting of the prologue lab (SecVarLab room + dressing, BVR2/LabToon materials): writes the sun
    /// direction, ambient fill and up to eight coloured lamp pools as shader globals. Runs in edit mode too,
    /// so the builder previews look like the game. Lamps flagged in <see cref="flicker"/> breathe a little
    /// (screens, neon), which keeps the room alive without any realtime light.
    /// </summary>
    [ExecuteAlways]
    public sealed class Bvr2LabLighting : MonoBehaviour
    {
        public const int MaxLamps = 8;
        [Tooltip("Direction the sunlight travels (world), as in the source scene's directional light.")]
        public Vector3 sunTravel = new Vector3(-0.3f, -0.5f, 0.8f);
        public Color sunColor = new Color(1f, 0.86f, 0.7f);
        [Range(0, 2)] public float sunStrength = 1f;
        public Color ambient = new Color(0.55f, 0.66f, 0.75f);
        [Range(0, 2)] public float ambientStrength = 0.45f;
        public Transform[] lamps = new Transform[0];
        public float lampRange = 6f;
        public Color lampColor = new Color(0.36f, 0.4f, 0.46f);
        [Tooltip("Per-lamp colour (optional; falls back to lampColor).")]
        public Color[] lampColors = new Color[0];
        [Tooltip("Per-lamp range (optional; 0 = lampRange).")]
        public float[] lampRanges = new float[0];
        [Tooltip("Per-lamp gentle flicker amount (optional).")]
        public float[] flicker = new float[0];
        [Tooltip("Optional point of interest; surfaces farther than radius get slightly darker.")]
        public Transform focus; public float focusRadius = 0f;

        static readonly int Sun = Shader.PropertyToID("_B2_LabSun"), SunColor = Shader.PropertyToID("_B2_LabSunColor"),
            Ambient = Shader.PropertyToID("_B2_LabAmbient"), Lamps = Shader.PropertyToID("_B2_LabLamps8"),
            LampColors = Shader.PropertyToID("_B2_LabLampColor8"), Focus = Shader.PropertyToID("_B2_LabFocus");
        readonly Vector4[] lampData = new Vector4[MaxLamps], lampColorData = new Vector4[MaxLamps];

        void OnEnable() => Push();
        void LateUpdate() => Push();

        public void Push()
        {
            Vector3 toSun = -sunTravel.normalized;
            Shader.SetGlobalVector(Sun, new Vector4(toSun.x, toSun.y, toSun.z, sunStrength));
            Shader.SetGlobalColor(SunColor, sunColor);
            Shader.SetGlobalColor(Ambient, new Color(ambient.r, ambient.g, ambient.b, ambientStrength));
            float t = Application.isPlaying ? Time.time : 0f;
            for (int i = 0; i < MaxLamps; i++)
            {
                var l = i < lamps.Length ? lamps[i] : null;
                float range = i < lampRanges.Length && lampRanges[i] > 0 ? lampRanges[i] : lampRange;
                lampData[i] = l && l.gameObject.activeInHierarchy ? new Vector4(l.position.x, l.position.y, l.position.z, range * Mathf.Max(0.01f, l.lossyScale.y)) : Vector4.zero;
                Color c = i < lampColors.Length && lampColors[i].maxColorComponent > 0 ? lampColors[i] : lampColor;
                float f = i < flicker.Length ? flicker[i] : 0f;
                if (f > 0) c *= 1f - f * (0.5f + 0.5f * Mathf.Sin(t * (2.1f + i * 0.73f)) * Mathf.Sin(t * (0.37f + i * 0.21f)));
                lampColorData[i] = c;
            }
            Shader.SetGlobalVectorArray(Lamps, lampData);
            Shader.SetGlobalVectorArray(LampColors, lampColorData);
            Shader.SetGlobalVector(Focus, focus && focusRadius > 0 ? new Vector4(focus.position.x, focus.position.y, focus.position.z, focusRadius) : Vector4.zero);
        }
    }
}
