using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// Procedural blood clot: trapped red cells, activated (spiky) platelets and a net of fibrin threads.
    /// Used for the thrombus growing on a ruptured plaque (hemisphere on the wall) and for the embolus
    /// (a free-floating piece). <see cref="growth"/> 0..1 drives how much of it exists.
    /// </summary>
    public sealed class Bvr2Clot : MonoBehaviour
    {
        public Mesh redMesh, plateletMesh; public Material redMaterial, plateletMaterial, fibrinMaterial;
        public int redCount = 22, plateletCount = 26, fibrinCount = 26;
        public float size = 1.6f;
        [Tooltip("true: a dome on a surface (local +Z = away from the wall); false: a free lump.")]
        public bool dome = true;
        public int seed = 7;
        [Range(0, 1)] public float growth;
        [Range(0, 1)] public float dissolve; // thrombolysis: fibrin goes first, then the lump falls apart

        struct Piece { public Transform t; public Vector3 home, spin; public float appear, scale; }
        readonly List<Piece> reds = new List<Piece>(), plates = new List<Piece>();
        readonly List<(LineRenderer line, int a, int b, float appear)> fibrin = new List<(LineRenderer, int, int, float)>();
        bool built;

        void Awake() { Build(); }

        public void Build()
        {
            if (built || !redMesh) return; built = true;
            var rnd = new System.Random(seed);
            float R() => (float)rnd.NextDouble();
            Vector3 P(float spread)
            {
                var v = new Vector3(R() * 2 - 1, R() * 2 - 1, R() * 2 - 1); if (v.sqrMagnitude > 1) v.Normalize();
                v *= spread; if (dome) v.z = Mathf.Abs(v.z) * 0.8f; return v;
            }
            for (int i = 0; i < redCount; i++)
            {
                var home = P(size * 0.5f); float appear = Mathf.Clamp01(home.magnitude / (size * 0.5f)) * 0.7f + R() * 0.2f;
                reds.Add(new Piece { t = Make("Red", redMesh, redMaterial, new Vector4(R(), 0, 0, R())), home = home, appear = appear, scale = size * Mathf.Lerp(0.16f, 0.24f, R()), spin = new Vector3(R(), R(), R()) * 360f });
            }
            for (int i = 0; i < plateletCount; i++)
            {
                var home = P(size * 0.58f); float appear = R() * 0.55f;
                plates.Add(new Piece { t = Make("Platelet", plateletMesh, plateletMaterial, new Vector4(R(), 0.25f, 1f, R())), home = home, appear = appear, scale = size * Mathf.Lerp(0.05f, 0.08f, R()), spin = new Vector3(R(), R(), R()) * 360f });
            }
            for (int i = 0; i < fibrinCount && fibrinMaterial; i++)
            {
                var go = new GameObject("Fibrin"); go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = fibrinMaterial; lr.positionCount = 2; lr.useWorldSpace = true;
                lr.widthMultiplier = size * 0.012f; lr.numCapVertices = 0; lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.startColor = lr.endColor = new Color(1f, 0.93f, 0.75f, 0.85f);
                int a = rnd.Next(reds.Count + plates.Count), b = rnd.Next(reds.Count + plates.Count);
                fibrin.Add((lr, a, b, 0.25f + R() * 0.6f));
            }
            Apply();
        }

        Transform Make(string name, Mesh mesh, Material mat, Vector4 inst)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var b = new MaterialPropertyBlock(); b.SetVector("_Inst", inst); r.SetPropertyBlock(b);
            return go.transform;
        }

        Vector3 PosOf(int i) => i < reds.Count ? reds[i].t.position : plates[i - reds.Count].t.position;

        void LateUpdate() { if (built) Apply(); }

        void Apply()
        {
            float g = growth, d = dissolve, time = Time.time;
            foreach (var p in reds)
            {
                float k = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(p.appear, p.appear + 0.25f, g)) * (1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.45f, 1f, d)));
                p.t.gameObject.SetActive(k > 0.01f);
                p.t.localPosition = p.home * (1f + d * 1.2f) + Vector3.one * Mathf.Sin(time * 2f + p.spin.x) * 0.01f * size;
                p.t.localRotation = Quaternion.Euler(p.spin + new Vector3(0, time * 6f, 0));
                p.t.localScale = Vector3.one * p.scale * k;
            }
            foreach (var p in plates)
            {
                float k = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(p.appear, p.appear + 0.2f, g)) * (1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.3f, 0.9f, d)));
                p.t.gameObject.SetActive(k > 0.01f);
                // platelets fly in from outside before settling
                float arrive = Mathf.Clamp01(Mathf.InverseLerp(p.appear, p.appear + 0.2f, g));
                p.t.localPosition = p.home * Mathf.Lerp(2.4f, 1f, arrive) * (1f + d);
                p.t.localRotation = Quaternion.Euler(p.spin + new Vector3(time * 20f, 0, 0));
                p.t.localScale = Vector3.one * p.scale * k;
            }
            foreach (var f in fibrin)
            {
                float k = Mathf.InverseLerp(f.appear, f.appear + 0.15f, g) * (1f - Mathf.InverseLerp(0f, 0.4f, d));
                f.line.enabled = k > 0.02f;
                if (!f.line.enabled) continue;
                Vector3 a = PosOf(f.a), b = PosOf(f.b);
                f.line.SetPosition(0, a); f.line.SetPosition(1, Vector3.Lerp(a, b, k));
            }
        }
    }
}
