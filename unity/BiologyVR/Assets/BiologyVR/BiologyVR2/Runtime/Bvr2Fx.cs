using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BiologyVR.V2
{
    /// <summary>Pooled feedback effects: spark bursts, expanding shock rings, energy beams.
    /// Sparks are drawn instanced (one draw call); rings and beams reuse a small pool.</summary>
    public sealed class Bvr2Fx : MonoBehaviour
    {
        public Mesh quadMesh, ringMesh;
        public Material sparkMaterial, ringMaterial, beamMaterial;

        struct Spark { public Vector3 p, v; public float life, max, size; public Color c; }
        readonly List<Spark> sparks = new List<Spark>(512);
        readonly Matrix4x4[] mats = new Matrix4x4[1023]; readonly Vector4[] data = new Vector4[1023];
        MaterialPropertyBlock block;

        class Ring { public Transform t; public MeshRenderer r; public float age, life, r0, r1; public Color c; }
        readonly List<Ring> rings = new List<Ring>();
        public class Beam { public LineRenderer line; public bool used; }
        readonly List<Beam> beams = new List<Beam>();
        MaterialPropertyBlock ringBlock;

        void Awake() { block = new MaterialPropertyBlock(); ringBlock = new MaterialPropertyBlock(); }

        public void Burst(Vector3 at, Color color, int count = 24, float speed = 2.5f, float size = 0.06f, float life = 0.7f, Vector3 bias = default)
        {
            for (int i = 0; i < count && sparks.Count < 1000; i++)
                sparks.Add(new Spark { p = at, v = Random.onUnitSphere * speed * Random.Range(0.35f, 1f) + bias, life = 0, max = life * Random.Range(0.6f, 1.2f), size = size * Random.Range(0.6f, 1.4f), c = color });
        }

        public void Shock(Vector3 at, Vector3 normal, Color color, float r0 = 0.2f, float r1 = 2.4f, float life = 0.6f)
        {
            Ring ring = null;
            foreach (var r in rings) if (r.age >= r.life) { ring = r; break; }
            if (ring == null)
            {
                var go = new GameObject("FX Ring"); go.transform.SetParent(transform, false);
                ring = new Ring { t = go.transform, r = go.AddComponent<MeshRenderer>() };
                go.AddComponent<MeshFilter>().sharedMesh = ringMesh; ring.r.sharedMaterial = ringMaterial; ring.r.shadowCastingMode = ShadowCastingMode.Off;
                rings.Add(ring);
            }
            ring.t.position = at; ring.t.rotation = Quaternion.LookRotation(normal.sqrMagnitude > 0 ? normal : Vector3.forward);
            ring.age = 0; ring.life = life; ring.r0 = r0; ring.r1 = r1; ring.c = color; ring.r.enabled = true;
        }

        public Beam GetBeam()
        {
            foreach (var b in beams) if (!b.used) { b.used = true; b.line.enabled = true; return b; }
            var go = new GameObject("FX Beam"); go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>(); lr.sharedMaterial = beamMaterial; lr.positionCount = 12; lr.useWorldSpace = true;
            lr.shadowCastingMode = ShadowCastingMode.Off; lr.receiveShadows = false; lr.textureMode = LineTextureMode.Stretch; lr.alignment = LineAlignment.View;
            var beam = new Beam { line = lr, used = true }; beams.Add(beam); return beam;
        }

        public void ReleaseBeam(Beam b) { if (b == null) return; b.used = false; b.line.enabled = false; }

        /// <summary>Curved beam from tool to target with a little energy wobble.</summary>
        public static void ShapeBeam(Beam b, Vector3 from, Vector3 fromDir, Vector3 to, Color color, float width, float wobble = 0.05f)
        {
            var lr = b.line; int n = lr.positionCount;
            Vector3 mid = from + fromDir * Vector3.Distance(from, to) * 0.45f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                Vector3 p = Vector3.Lerp(Vector3.Lerp(from, mid, t), Vector3.Lerp(mid, to, t), t);
                p += new Vector3(Mathf.Sin(Time.time * 23f + i), Mathf.Sin(Time.time * 19f + i * 1.7f), Mathf.Sin(Time.time * 17f + i * 2.3f)) * wobble * Mathf.Sin(t * Mathf.PI);
                lr.SetPosition(i, p);
            }
            lr.startColor = color; lr.endColor = new Color(color.r, color.g, color.b, color.a * 0.6f);
            lr.startWidth = width * 0.6f; lr.endWidth = width;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            int n = 0;
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                var s = sparks[i]; s.life += dt;
                if (s.life >= s.max) { sparks.RemoveAt(i); continue; }
                s.v *= Mathf.Exp(-dt * 2.2f); s.p += s.v * dt; sparks[i] = s;
                if (n < 1023)
                {
                    float k = 1f - s.life / s.max;
                    mats[n] = Matrix4x4.TRS(s.p, Quaternion.identity, Vector3.one * s.size * (0.4f + 0.6f * k));
                    data[n] = new Vector4(s.c.r * k * 1.4f, s.c.g * k * 1.4f, s.c.b * k * 1.4f, 1); n++;
                }
            }
            if (n > 0 && sparkMaterial && quadMesh)
            {
                block.Clear(); block.SetVectorArray("_Inst", data);
                Graphics.RenderMeshInstanced(new RenderParams(sparkMaterial) { matProps = block, shadowCastingMode = ShadowCastingMode.Off, worldBounds = new Bounds(Camera.main ? Camera.main.transform.position : Vector3.zero, Vector3.one * 300) }, quadMesh, 0, mats, n);
            }
            foreach (var r in rings)
            {
                if (r.age >= r.life) { if (r.r.enabled) r.r.enabled = false; continue; }
                r.age += dt; float t = Mathf.Clamp01(r.age / r.life);
                r.t.localScale = Vector3.one * Mathf.Lerp(r.r0, r.r1, 1f - (1f - t) * (1f - t));
                ringBlock.SetColor("_Color", new Color(r.c.r, r.c.g, r.c.b, r.c.a * (1f - t)));
                r.r.SetPropertyBlock(ringBlock);
            }
        }
    }
}
