using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BiologyVR.V2
{
    public enum CellKind { RedCell = 0, WhiteCell = 1, Platelet = 2 }

    /// <summary>
    /// The living bloodstream around the player: hundreds of cells drawn with GPU instancing,
    /// moving with the Poiseuille profile and the heartbeat, parting around the player.
    /// Cells can be picked (scanner / BioTool), highlighted and taken out of the stream.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class Bvr2Blood : MonoBehaviour
    {
        public Bvr2Vessel vessel;
        public Bvr2Organism organism;
        public Transform viewer;
        public Mesh redMesh, redMeshLow, whiteMesh, plateletMesh, moteMesh;
        public Material redMaterial, whiteMaterial, plateletMaterial, moteMaterial;
        public int redCount = 380, whiteCount = 7, plateletCount = 70, moteCount = 360;
        public float behind = 30f, ahead = 85f;
        public float redSize = 0.95f, whiteSize = 1.6f, plateletSize = 0.22f;
        public float drawDistance = 66f, lodDistance = 16f;
        [Range(0, 2)] public float density = 1f;

        struct Cell
        {
            public CellKind kind; public float s, r01, angle, speed, spin, spinSpeed, var, phase, highlight, morph;
            public Vector3 axis, push; public bool hidden; public float hiddenUntil;
        }
        Cell[] cells; Vector3[] positions; Quaternion[] rotations;
        struct Mote { public float s, r01, angle, speed, size, hue; }
        Mote[] motes;
        readonly Matrix4x4[] batch = new Matrix4x4[1023]; readonly Vector4[] instData = new Vector4[1023];
        readonly Matrix4x4[] batchLow = new Matrix4x4[1023]; readonly Vector4[] instLow = new Vector4[1023];
        MaterialPropertyBlock block, blockLow;
        float viewerS; int viewerHint;
        public int Count => cells == null ? 0 : cells.Length;
        public bool Running { get; private set; }
        public int Drawn { get; private set; }
        Vector3 focusPoint; float focusRadius;
        /// <summary>Cells drift out of a sphere (a hologram being shown) and out of the line of sight to it.</summary>
        public void SetFocus(Vector3 point, float radius) { focusPoint = point; focusRadius = radius; }
        public void ClearFocus() { focusRadius = 0; }

        Vector3 sinkPoint; float sinkRadius, sinkStrength;
        /// <summary>A leak: cells near the point are dragged toward it and out of the vessel (then recycled).</summary>
        public void SetSink(Vector3 point, float radius, float strength) { sinkPoint = point; sinkRadius = radius; sinkStrength = strength; }
        public void ClearSink() { sinkStrength = 0; }

        void Start() { Spawn(); }

        public void Spawn()
        {
            if (!vessel) return;
            int n = redCount + whiteCount + plateletCount;
            cells = new Cell[n]; positions = new Vector3[n]; rotations = new Quaternion[n];
            float s0 = ViewerS();
            int i = 0;
            for (int k = 0; k < redCount; k++) cells[i++] = NewCell(CellKind.RedCell, s0, true);
            for (int k = 0; k < whiteCount; k++) cells[i++] = NewCell(CellKind.WhiteCell, s0, true);
            for (int k = 0; k < plateletCount; k++) cells[i++] = NewCell(CellKind.Platelet, s0, true);
            motes = new Mote[moteCount];
            for (int k = 0; k < moteCount; k++) motes[k] = NewMote(s0, true);
            block = new MaterialPropertyBlock(); blockLow = new MaterialPropertyBlock();
            Running = true;
        }

        float ViewerS() { if (!viewer) return 0; return vessel.Locate(viewer.position, ref viewerHint, out _); }

        Cell NewCell(CellKind kind, float aroundS, bool anywhere)
        {
            var c = new Cell { kind = kind };
            c.s = anywhere ? aroundS + Random.Range(-behind, ahead) : aroundS - behind + Random.Range(0f, 8f);
            c.r01 = Mathf.Sqrt(Random.value) * (kind == CellKind.WhiteCell ? 0.78f : 0.9f);
            if (kind == CellKind.WhiteCell) c.r01 = Mathf.Max(c.r01, 0.45f); // leukocytes roll near the wall
            c.angle = Random.Range(0f, 360f); c.speed = Random.Range(0.85f, 1.12f) * (kind == CellKind.WhiteCell ? 0.55f : 1f);
            c.axis = Random.onUnitSphere; c.spin = Random.Range(0, 360f); c.spinSpeed = Random.Range(-70f, 70f);
            c.var = Random.Range(0.86f, 1.08f); c.phase = Random.value; c.highlight = 0; c.morph = 0;
            return c;
        }

        Mote NewMote(float aroundS, bool anywhere) => new Mote
        {
            s = anywhere ? aroundS + Random.Range(-behind * 0.5f, ahead * 0.6f) : aroundS - behind * 0.5f,
            r01 = Mathf.Sqrt(Random.value) * 0.93f, angle = Random.Range(0, 360f), speed = Random.Range(0.8f, 1.15f), size = Random.Range(0.05f, 0.13f), hue = Random.value
        };

        void Update()
        {
            if (!Running || !vessel) return;
            float dt = Bvr2Time.Delta;
            viewerS = ViewerS();
            float mean = organism ? organism.MeanSpeed : 2.6f;
            Vector3 viewPos = viewer ? viewer.position : Vector3.zero;
            for (int i = 0; i < cells.Length; i++)
            {
                ref var c = ref cells[i];
                if (c.hidden)
                {
                    if (c.hiddenUntil > 0 && Time.time > c.hiddenUntil) { var k = c.kind; c = NewCell(k, viewerS, false); }
                    continue;
                }
                float R = vessel.Radius(c.s);
                float cellR = c.kind == CellKind.WhiteCell ? whiteSize : c.kind == CellKind.RedCell ? redSize * 0.5f : plateletSize;
                float r = Mathf.Min(c.r01 * R, vessel.FreeRadius(c.s, c.angle) - cellR - 0.25f);
                if (r < 0.2f) r = 0.2f;
                Vector3 flow = vessel.FlowAt(c.s, r, mean);
                c.s += flow.magnitude * c.speed * dt;
                c.angle += Mathf.Sin(Bvr2Time.Now * 0.3f + c.phase * 6.28f) * dt * 6f;
                c.spin += c.spinSpeed * dt * (0.4f + flow.magnitude * 0.25f);
                // keep the population around the viewer (far cells are hidden by the lumen fog)
                if (c.s > viewerS + ahead) { c.s = viewerS - behind + Random.Range(0f, 6f); c.push = Vector3.zero; }
                else if (c.s < viewerS - behind - 2f) { c.s = viewerS + ahead - Random.Range(0f, 10f); c.push = Vector3.zero; }
                Vector3 p = vessel.PointAt(c.s, r, c.angle) + c.push;
                // part around the player's head so cells never fill the camera
                Vector3 away = p - viewPos; float d = away.magnitude;
                float personal = 1.2f + cellR;
                if (d < personal && d > 0.001f) c.push += away / d * (personal - d) * Mathf.Min(1f, dt * 8f);
                if (sinkStrength > 0)
                {
                    Vector3 toSink = sinkPoint - p; float ds2 = toSink.sqrMagnitude;
                    if (ds2 < sinkRadius * sinkRadius)
                    {
                        float dd = Mathf.Sqrt(ds2); float k = 1f - dd / sinkRadius;
                        // swirl around and into the tear
                        Vector3 swirl = Vector3.Cross(toSink, vessel.Tangent(c.s)).normalized * 0.6f;
                        c.push += (toSink / Mathf.Max(dd, 0.05f) + swirl) * sinkStrength * k * k * dt;
                        if (dd < 0.6f) { c.s = viewerS + ahead - Random.Range(0f, 10f); c.push = Vector3.zero; }
                    }
                }
                if (focusRadius > 0)
                {
                    // distance to the segment head → hologram (+ the hologram's sphere)
                    Vector3 ab = focusPoint - viewPos; float tt = Mathf.Clamp01(Vector3.Dot(p - viewPos, ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
                    Vector3 off = p - (viewPos + ab * tt); float od = off.magnitude, need = focusRadius + cellR;
                    if (od < need) c.push += (od > 1e-3f ? off / od : Vector3.up) * (need - od) * Mathf.Min(1f, Time.unscaledDeltaTime * 6f);
                }
                c.push *= Mathf.Exp(-dt * 0.6f);
                positions[i] = p;
                var tangent = vessel.Tangent(c.s);
                rotations[i] = Quaternion.AngleAxis(c.spin, c.axis) * Quaternion.LookRotation(tangent);
                c.highlight = Mathf.MoveTowards(c.highlight, 0, Time.deltaTime * 2.5f);
            }
            for (int i = 0; i < motes.Length; i++)
            {
                ref var m = ref motes[i];
                float R = vessel.Radius(m.s);
                m.s += vessel.FlowAt(m.s, m.r01 * R, mean).magnitude * m.speed * dt;
                m.angle += dt * 4f;
                if (m.s > viewerS + ahead * 0.6f) m = NewMote(viewerS, false);
                else if (m.s < viewerS - behind * 0.5f - 2f) { m.s = viewerS + ahead * 0.6f - Random.Range(0f, 6f); }
            }
            Draw();
        }

        void Draw()
        {
            var cam = viewer;
            Vector3 cp = cam ? cam.position : Vector3.zero; Vector3 cf = cam ? cam.forward : Vector3.forward;
            int drawn = 0;
            drawn += DrawKind(CellKind.RedCell, redMesh, redMeshLow, redMaterial, redSize, cp, cf);
            drawn += DrawKind(CellKind.WhiteCell, whiteMesh, null, whiteMaterial, whiteSize, cp, cf);
            drawn += DrawKind(CellKind.Platelet, plateletMesh, null, plateletMaterial, plateletSize, cp, cf);
            // plasma motes
            if (moteMesh && moteMaterial)
            {
                int n = 0;
                for (int i = 0; i < motes.Length && n < 1023; i++)
                {
                    var m = motes[i]; float R = vessel.Radius(m.s);
                    Vector3 p = vessel.PointAt(m.s, m.r01 * R, m.angle);
                    Vector3 to = p - cp; if (to.sqrMagnitude > drawDistance * drawDistance * 0.36f || Vector3.Dot(to, cf) < -1f) continue;
                    batch[n] = Matrix4x4.TRS(p, Quaternion.identity, Vector3.one * m.size);
                    instData[n] = m.hue < 0.75f ? new Vector4(1f, 0.85f, 0.75f, 1) : new Vector4(0.75f, 0.95f, 1f, 1.3f);
                    n++;
                }
                if (n > 0)
                {
                    block.Clear(); block.SetVectorArray("_Inst", instData);
                    var rp = new RenderParams(moteMaterial) { matProps = block, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false, worldBounds = new Bounds(cp, Vector3.one * 400) };
                    Graphics.RenderMeshInstanced(rp, moteMesh, 0, batch, n);
                }
            }
            Drawn = drawn;
        }

        int DrawKind(CellKind kind, Mesh mesh, Mesh low, Material mat, float size, Vector3 cp, Vector3 cf)
        {
            if (!mesh || !mat) return 0;
            int n = 0, nl = 0; float dd = drawDistance * drawDistance, ld = lodDistance * lodDistance;
            Vector3 scale = kind == CellKind.RedCell ? new Vector3(size * 0.5f, size * 0.5f, size * 0.5f) : Vector3.one * size * (kind == CellKind.Platelet ? 1f : 0.5f);
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].kind != kind || cells[i].hidden) continue;
                Vector3 to = positions[i] - cp; float sq = to.sqrMagnitude;
                if (sq > dd || (Vector3.Dot(to, cf) < -2f && sq > 9f)) continue;
                var data = new Vector4(cells[i].var, cells[i].highlight, cells[i].morph, cells[i].phase);
                if (low && sq > ld) { if (nl < 1023) { batchLow[nl] = Matrix4x4.TRS(positions[i], rotations[i], scale); instLow[nl++] = data; } }
                else if (n < 1023) { batch[n] = Matrix4x4.TRS(positions[i], rotations[i], scale); instData[n++] = data; }
            }
            var bounds = new Bounds(cp, Vector3.one * (drawDistance * 2.2f));
            if (n > 0) { block.Clear(); block.SetVectorArray("_Inst", instData); Graphics.RenderMeshInstanced(new RenderParams(mat) { matProps = block, shadowCastingMode = ShadowCastingMode.Off, worldBounds = bounds }, mesh, 0, batch, n); }
            if (nl > 0) { blockLow.Clear(); blockLow.SetVectorArray("_Inst", instLow); Graphics.RenderMeshInstanced(new RenderParams(mat) { matProps = blockLow, shadowCastingMode = ShadowCastingMode.Off, worldBounds = bounds }, low, 0, batchLow, nl); }
            return n + nl;
        }

        // ----- queries used by the tools ------------------------------------------------------
        public float PickRadius(CellKind kind) => kind == CellKind.RedCell ? redSize * 0.6f : kind == CellKind.WhiteCell ? whiteSize * 0.75f : 0.55f;

        /// <summary>Nearest cell along a ray, with an aim cone (degrees) that makes moving cells easy to hit.</summary>
        public bool Pick(Ray ray, float maxDistance, float coneDeg, out int index, out CellKind kind, out float distance, int onlyKind = -1)
        {
            index = -1; kind = CellKind.RedCell; distance = maxDistance; float bestScore = float.MaxValue;
            if (cells == null) return false;
            float coneTan = Mathf.Tan(coneDeg * Mathf.Deg2Rad);
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].hidden) continue;
                if (onlyKind >= 0 && (int)cells[i].kind != onlyKind) continue;
                Vector3 to = positions[i] - ray.origin; float along = Vector3.Dot(to, ray.direction);
                if (along < 0.3f || along > maxDistance) continue;
                float off = (to - ray.direction * along).magnitude;
                float allowed = PickRadius(cells[i].kind) + along * coneTan;
                if (off > allowed) continue;
                float score = off / allowed + along / maxDistance * 0.6f - (cells[i].kind == CellKind.WhiteCell ? 0.2f : 0);
                if (score < bestScore) { bestScore = score; index = i; kind = cells[i].kind; distance = along; }
            }
            return index >= 0;
        }

        public Vector3 PositionOf(int index) => positions[index];
        public Quaternion RotationOf(int index) => rotations[index];
        public CellKind KindOf(int index) => cells[index].kind;
        public bool IsHidden(int index) => cells[index].hidden;
        public void Highlight(int index, float amount) { if (index >= 0 && index < cells.Length) cells[index].highlight = Mathf.Max(cells[index].highlight, amount); }

        /// <summary>Take a cell out of the stream (the tool now shows its own copy).</summary>
        public void Take(int index, float respawnAfter = 6f) { cells[index].hidden = true; cells[index].hiddenUntil = respawnAfter > 0 ? Time.time + respawnAfter : -1; }

        /// <summary>Count of visible cells of a kind within a radius (for teaching proportions).</summary>
        public int CountNear(CellKind kind, Vector3 centre, float radius)
        {
            int n = 0; float r2 = radius * radius;
            for (int i = 0; i < cells.Length; i++) if (!cells[i].hidden && cells[i].kind == kind && (positions[i] - centre).sqrMagnitude < r2) n++;
            return n;
        }

        /// <summary>Spawn a cell of a kind just ahead of the viewer (scripted guarantees: a leukocyte to find).</summary>
        public int Inject(CellKind kind, float sAhead, float r01, float angle)
        {
            for (int i = 0; i < cells.Length; i++)
                if (cells[i].kind == kind)
                {
                    float d = cells[i].s - viewerS;
                    if (cells[i].hidden || d < -10 || d > 40)
                    {
                        cells[i] = NewCell(kind, viewerS, true); cells[i].s = viewerS + sAhead; cells[i].r01 = r01; cells[i].angle = angle; cells[i].push = Vector3.zero;
                        return i;
                    }
                }
            return -1;
        }
    }
}
