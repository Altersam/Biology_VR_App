using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Lighting of the prologue lab (SecVarLab room, BVR2/LabToon materials): writes the sun direction,
    /// ambient fill and up to four lamp pools as shader globals. Runs in edit mode too, so the builder
    /// previews look like the game.
    /// </summary>
    [ExecuteAlways]
    public sealed class Bvr2LabLighting : MonoBehaviour
    {
        [Tooltip("Direction the sunlight travels (world), as in the source scene's directional light.")]
        public Vector3 sunTravel = new Vector3(-0.3f, -0.5f, 0.8f);
        public Color sunColor = new Color(1f, 0.86f, 0.7f);
        [Range(0, 2)] public float sunStrength = 1f;
        public Color ambient = new Color(0.55f, 0.66f, 0.75f);
        [Range(0, 2)] public float ambientStrength = 0.45f;
        public Transform[] lamps = new Transform[0];
        public float lampRange = 6f;
        public Color lampColor = new Color(0.36f, 0.4f, 0.46f);
        [Tooltip("Optional point of interest; surfaces farther than radius get slightly darker.")]
        public Transform focus; public float focusRadius = 0f;

        static readonly int Sun = Shader.PropertyToID("_B2_LabSun"), SunColor = Shader.PropertyToID("_B2_LabSunColor"),
            Ambient = Shader.PropertyToID("_B2_LabAmbient"), Lamps = Shader.PropertyToID("_B2_LabLamps"),
            LampColors = Shader.PropertyToID("_B2_LabLampColor"), Focus = Shader.PropertyToID("_B2_LabFocus");
        readonly Vector4[] lampData = new Vector4[4], lampColors = new Vector4[4];

        void OnEnable() => Push();
        void LateUpdate() => Push();

        public void Push()
        {
            Vector3 toSun = -sunTravel.normalized;
            Shader.SetGlobalVector(Sun, new Vector4(toSun.x, toSun.y, toSun.z, sunStrength));
            Shader.SetGlobalColor(SunColor, sunColor);
            Shader.SetGlobalColor(Ambient, new Color(ambient.r, ambient.g, ambient.b, ambientStrength));
            for (int i = 0; i < 4; i++)
            {
                var t = i < lamps.Length ? lamps[i] : null;
                lampData[i] = t && t.gameObject.activeInHierarchy ? new Vector4(t.position.x, t.position.y, t.position.z, lampRange) : Vector4.zero;
                lampColors[i] = lampColor;
            }
            Shader.SetGlobalVectorArray(Lamps, lampData);
            Shader.SetGlobalVectorArray(LampColors, lampColors);
            Shader.SetGlobalVector(Focus, focus && focusRadius > 0 ? new Vector4(focus.position.x, focus.position.y, focus.position.z, focusRadius) : Vector4.zero);
        }
    }
}
