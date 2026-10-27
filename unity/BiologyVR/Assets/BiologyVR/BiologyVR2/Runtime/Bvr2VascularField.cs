using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Ability "Сосудистое поле": squeeze or widen a stretch of the artery like the smooth muscle does.
    /// VR: hold both grips and move the hands together / apart. Desktop: mouse wheel or Z / X.
    /// The segment's conductance follows r⁴, so the organism model reacts at once.
    /// </summary>
    public sealed class Bvr2VascularField : MonoBehaviour
    {
        public Bvr2Rig rig; public Bvr2Input input; public Bvr2Vessel vessel; public Bvr2Organism organism; public Bvr2Audio audioDirector;
        public Transform ringA, ringB; public Renderer[] ringRenderers;
        public int zoneIndex = 0;
        public float minFactor = 0.55f, maxFactor = 1.3f;
        public bool unlocked, active;
        public float centre, halfWidth = 14f;
        [Tooltip("Extra constriction applied by the scenario (spasm), multiplied with the player's control.")]
        public float scenarioFactor = 1f;
        [Tooltip("Debug: behave as if the player is holding the field.")] public bool debugGrab;

        public float Factor { get; private set; } = 1f;      // player's control (1 = relaxed)
        public float Effective => Factor * scenarioFactor;   // what the vessel actually does
        public bool Grabbing { get; private set; }
        float startDistance, startFactor, glow; MaterialPropertyBlock mpb;
        static readonly int FieldId = Shader.PropertyToID("_B2_Field"), FieldColorId = Shader.PropertyToID("_B2_FieldColor");

        void Awake() { mpb = new MaterialPropertyBlock(); }

        public void Activate(float sCentre, float half) { centre = sCentre; halfWidth = half; active = true; }
        public void Deactivate() { active = false; Grabbing = false; }
        public void SetFactor(float f) { Factor = Mathf.Clamp(f, minFactor, maxFactor); }

        void Update()
        {
            bool on = unlocked && active;
            if (on)
            {
                if (rig.IsXR)
                {
                    bool both = input.GripL && input.GripR;
                    float d = Vector3.Distance(rig.leftHand.position, rig.rightHand.position);
                    if (both && !Grabbing) { Grabbing = true; startDistance = Mathf.Max(d, 0.1f); startFactor = Factor; Bvr2Haptics.Both(0.3f, 0.06f); }
                    if (!both) Grabbing = false;
                    if (Grabbing) SetFactor(startFactor * Mathf.Pow(d / startDistance, 0.9f));
                }
                else
                {
                    if (Mathf.Abs(input.FieldDelta) > 0.0001f) { SetFactor(Factor + input.FieldDelta); Grabbing = true; }
                    else Grabbing = false;
                }
                if (debugGrab) Grabbing = true;
                if (Grabbing && Time.frameCount % 6 == 0) Bvr2Haptics.Both(0.05f + Mathf.Abs(1 - Factor) * 0.2f, 0.03f);
            }
            float eff = Effective;
            bool zoneUsed = on || Mathf.Abs(scenarioFactor - 1f) > 0.001f;
            if (zoneUsed) vessel.SetZone(zoneIndex, centre, halfWidth, eff);
            else vessel.ClearZone(zoneIndex);
            if (organism) organism.fieldConductance = zoneUsed ? Mathf.Pow(eff, 4f) : 1f;
            if (audioDirector && Grabbing) audioDirector.FieldLevel = Mathf.Abs(1 - Factor) + 0.3f;
            // energy bands on the wall of the controlled stretch (stronger while the player holds it)
            float glowTarget = on ? (Grabbing ? 1f : 0.35f) : 0f;
            glow = Mathf.MoveTowards(glow, glowTarget, Time.unscaledDeltaTime * 3f);
            var fc = Factor < 0.97f ? Bvr2Style.Danger : Factor > 1.03f ? Bvr2Style.Good : Bvr2Style.Ally;
            Shader.SetGlobalVector(FieldId, new Vector4(centre, halfWidth, glow, Factor < 0.99f ? 1f : -1f));
            Shader.SetGlobalColor(FieldColorId, fc * 0.9f);
            // visual rings at both ends of the controlled stretch
            if (ringA && ringB)
            {
                bool show = on;
                ringA.gameObject.SetActive(show); ringB.gameObject.SetActive(show);
                if (show)
                {
                    Place(ringA, centre - halfWidth * 0.7f, eff); Place(ringB, centre + halfWidth * 0.7f, eff);
                    var c = Factor < 0.92f ? Bvr2Style.Danger : Factor > 1.08f ? Bvr2Style.Good : Bvr2Style.Ally;
                    c.a = Grabbing ? 0.95f : 0.55f;
                    mpb.SetColor("_Color", c);
                    foreach (var r in ringRenderers) if (r) r.SetPropertyBlock(mpb);
                }
            }
        }

        void Place(Transform ring, float s, float factor)
        {
            vessel.Frame(s, out var c, out var t, out _, out _);
            ring.position = c; ring.rotation = Quaternion.LookRotation(t);
            ring.localScale = Vector3.one * vessel.BaseRadius(s) * factor * 0.93f;
        }
    }
}
