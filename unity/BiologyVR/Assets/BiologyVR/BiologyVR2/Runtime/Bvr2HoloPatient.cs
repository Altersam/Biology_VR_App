using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// The patient hologram in the prologue lab: the heart beats with the organism's pulse (lub-dub), pulses of
    /// blood leave it along the arteries (shader globals _B2_HoloBeat / _B2_HoloRate, also used by the ECG strip),
    /// the projector rings turn and the labels face the viewer.
    /// </summary>
    public sealed class Bvr2HoloPatient : MonoBehaviour
    {
        public Transform heart; public Transform[] rings; public float[] ringSpeeds; public Transform[] labels;
        /// <summary>Per label: the point it names (local to this transform), its offset beside that point as seen by the
        /// viewer (metres: right, up) and an optional leader line from the point to the label's near edge.</summary>
        public Vector3[] labelAnchors; public Vector2[] labelOffsets; public LineRenderer[] leaders; public Vector2 labelHalfSize = new Vector2(0.23f, 0.04f);
        public Transform entryMarker;
        public Bvr2Organism organism; public Transform viewer;
        static readonly int BeatId = Shader.PropertyToID("_B2_HoloBeat"), RateId = Shader.PropertyToID("_B2_HoloRate");
        // one heart beat for every hologram (the lab and the menu stage); advanced once per frame
        static float beat = 1f; static int beatFrame = -1; Vector3 heartScale; bool init;

        void OnEnable()
        {
            if (heart && !init) { heartScale = heart.localScale; init = true; }
            if (!organism) organism = FindAnyObjectByType<Bvr2Organism>(); // the same pulse as the vitals panel
        }

        void Update()
        {
            float hr = organism ? organism.heartRate : 72f;
            float rate = Mathf.Clamp(hr, 30f, 200f) / 60f;
            if (beatFrame != Time.frameCount) { beatFrame = Time.frameCount; beat += Time.deltaTime * rate; Shader.SetGlobalFloat(BeatId, beat); Shader.SetGlobalFloat(RateId, rate); }
            if (heart && init)
            {
                float f = Mathf.Repeat(beat, 1f);
                // lub (systole) then a smaller dub
                float s = 0.13f * Mathf.Exp(-Mathf.Pow((f - 0.30f) / 0.05f, 2f)) + 0.06f * Mathf.Exp(-Mathf.Pow((f - 0.48f) / 0.05f, 2f));
                heart.localScale = heartScale * (1f + s);
            }
            if (rings != null)
                for (int i = 0; i < rings.Length; i++)
                    if (rings[i]) rings[i].Rotate(Vector3.up, (ringSpeeds != null && i < ringSpeeds.Length ? ringSpeeds[i] : 20f) * Time.deltaTime, Space.Self);
            if (entryMarker) { float p = 1f + 0.25f * Mathf.Sin(Time.time * 4f); entryMarker.localScale = Vector3.one * 0.05f * p; entryMarker.Rotate(Vector3.up, 120f * Time.deltaTime, Space.World); }
            var cam = viewer ? viewer : Camera.main ? Camera.main.transform : null;
            if (cam && labels != null)
                for (int i = 0; i < labels.Length; i++)
                {
                    var l = labels[i]; if (!l) continue;
                    bool anchored = labelAnchors != null && i < labelAnchors.Length && labelOffsets != null && i < labelOffsets.Length;
                    Vector3 a = anchored ? transform.TransformPoint(labelAnchors[i]) : l.position;
                    Vector3 d = a - cam.position; d.y = 0;
                    if (d.sqrMagnitude < 1e-4f) continue;
                    d.Normalize(); Vector3 right = Vector3.Cross(Vector3.up, d);
                    if (anchored)
                    {
                        // the label stays beside its point from wherever it is seen (the figure turns on the pad)
                        float k = transform.lossyScale.y; Vector2 o = labelOffsets[i] * k;
                        l.position = a + right * o.x + Vector3.up * o.y;
                        if (leaders != null && i < leaders.Length && leaders[i])
                        {
                            Vector3 end = Mathf.Abs(o.x) > 1e-3f ? l.position - right * Mathf.Sign(o.x) * labelHalfSize.x * k : l.position - Vector3.up * Mathf.Sign(o.y) * labelHalfSize.y * k;
                            leaders[i].SetPosition(0, a); leaders[i].SetPosition(1, end);
                            leaders[i].widthMultiplier = 0.009f * k;
                        }
                    }
                    l.rotation = Quaternion.LookRotation(d, Vector3.up);
                }
        }
    }
}
