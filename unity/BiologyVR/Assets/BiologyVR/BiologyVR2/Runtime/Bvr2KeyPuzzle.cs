using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>
    /// "Ключ" (chapter 5): a big hologram of the armoured virion in slow motion. Its spikes end in different
    /// tips (epitopes); the antibody beside it has a paratope of one shape. Rotate the virion (stick / WASD)
    /// until a spike with the matching tip points at the antibody, then dock it (trigger). A wrong tip bounces
    /// off. Three matches give the scanner the exact key → specific antibodies.
    /// </summary>
    public sealed class Bvr2KeyPuzzle : MonoBehaviour
    {
        public Bvr2Input input; public Bvr2Fx fx; public Bvr2Audio audioDirector;
        public Mesh capsidMesh, sphereMesh, coneMesh, ringMesh, antibodyMesh, stalkMesh; public Material holoMaterial, glowMaterial, antibodyMaterial;
        public int needed = 3;
        public float turnSpeed = 95f, alignDeg = 24f;

        public int Locks { get; private set; }
        public int Mistakes { get; private set; }
        public bool Done => Locks >= needed;
        public bool Showing { get; private set; }

        class Spike { public Transform root, tip; public Renderer tipR; public int shape; public bool locked; public Vector3 dir; }
        readonly List<Spike> spikes = new List<Spike>();
        Transform root, virion, antibody, guide; Renderer guideR; MaterialPropertyBlock mpb; float flash; Color flashColor;
        const int KeyShape = 0; // ring tip = the epitope this antibody fits

        void Awake() { mpb = new MaterialPropertyBlock(); }

        public void Show(Transform parent, Vector3 position, Quaternion rotation)
        {
            Hide();
            Showing = true; Locks = 0; Mistakes = 0;
            root = new GameObject("Key Puzzle").transform; root.SetParent(parent, true);
            root.position = position; root.rotation = rotation;
            virion = new GameObject("Virion").transform; virion.SetParent(root, false); virion.localPosition = new Vector3(-0.12f, 0, 0);
            Part("Capsid", capsidMesh, holoMaterial, virion, Vector3.zero, Vector3.one * 0.1f);
            // 14 spikes, 3 tip shapes; exactly `needed` of them carry the key epitope, spread around the virion
            var dirs = Bvr2Meshes.IcoSphere(1, 0, 1, 1, "tmp").vertices;
            for (int i = 0; i < dirs.Length && spikes.Count < 14; i += 3)
            {
                var d = dirs[i].normalized;
                int shape = spikes.Count % 3 == 0 ? 1 : spikes.Count % 3 == 1 ? 2 : 1;
                spikes.Add(MakeSpike(d, shape));
            }
            // the key spikes: far apart, so each match needs a new turn
            Vector3[] keyDirs = { new Vector3(0, 0.3f, -1f).normalized, new Vector3(-0.8f, -0.5f, 0.4f).normalized, new Vector3(0.3f, 0.95f, 0.5f).normalized };
            for (int i = 0; i < needed && i < keyDirs.Length; i++) spikes.Add(MakeSpike(keyDirs[i], KeyShape));
            // antibody on the right, paratope (its arm tips) shows the key shape
            antibody = new GameObject("Antibody").transform; antibody.SetParent(root, false);
            antibody.localPosition = new Vector3(0.3f, 0, 0); antibody.localRotation = Quaternion.Euler(0, 0, 90f);
            Part("Y", antibodyMesh, antibodyMaterial, antibody, Vector3.zero, Vector3.one * 0.13f);
            for (int a = -1; a <= 1; a += 2)
            {
                var t = Part("Paratope", ringMesh, glowMaterial, antibody, new Vector3(a * 0.045f, 0.075f, 0), Vector3.one * 0.026f);
                t.transform.localRotation = Quaternion.Euler(90, 0, 0);
                Glow(t.GetComponent<Renderer>(), new Color(1f, 0.85f, 0.4f));
            }
            // guide ring between them: where a spike has to point
            var g = Part("Guide", ringMesh, glowMaterial, root, new Vector3(0.11f, 0, 0), Vector3.one * 0.045f);
            g.transform.localRotation = Quaternion.Euler(0, 90, 0); guide = g.transform; guideR = g.GetComponent<Renderer>();
            virion.localRotation = Quaternion.Euler(20, 140, 0);
            fx?.Shock(root.position, -root.forward, new Color(1f, 0.85f, 0.4f), 0.05f, 0.7f, 0.5f);
        }

        Spike MakeSpike(Vector3 d, int shape)
        {
            var sroot = new GameObject("Spike").transform; sroot.SetParent(virion, false);
            sroot.localRotation = Quaternion.FromToRotation(Vector3.up, d);
            Part("Stalk", stalkMesh, holoMaterial, sroot, new Vector3(0, 0.11f, 0), new Vector3(0.008f, 0.04f, 0.008f));
            GameObject tip;
            if (shape == 0) { tip = Part("Tip", ringMesh, glowMaterial, sroot, new Vector3(0, 0.165f, 0), Vector3.one * TipSize(0)); tip.transform.localRotation = Quaternion.Euler(90, 0, 0); }
            else if (shape == 1) tip = Part("Tip", sphereMesh, glowMaterial, sroot, new Vector3(0, 0.165f, 0), Vector3.one * TipSize(1));
            else { tip = Part("Tip", coneMesh, glowMaterial, sroot, new Vector3(0, 0.185f, 0), new Vector3(TipSize(2), TipSize(2), 0.04f)); tip.transform.localRotation = Quaternion.Euler(90, 0, 0); }
            var sp = new Spike { root = sroot, tip = tip.transform, tipR = tip.GetComponent<Renderer>(), shape = shape, dir = d };
            Glow(sp.tipR, new Color(0.85f, 0.95f, 1f));
            return sp;
        }

        static float TipSize(int shape) => shape == 0 ? 0.028f : shape == 1 ? 0.022f : 0.022f;

        GameObject Part(string name, Mesh mesh, Material mat, Transform parent, Vector3 pos, Vector3 scale)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        void Glow(Renderer r, Color c) { mpb.Clear(); mpb.SetColor("_Color", c); r.SetPropertyBlock(mpb); }

        public void Hide()
        {
            if (root) Destroy(root.gameObject);
            root = null; spikes.Clear(); Showing = false;
        }

        /// <summary>The spike now pointing at the antibody (within the alignment cone), if any.</summary>
        Spike Aligned(out float angle)
        {
            Spike best = null; angle = alignDeg;
            Vector3 axis = (antibody.position - virion.position).normalized;
            foreach (var s in spikes)
            {
                if (s.locked) continue;
                float a = Vector3.Angle(s.root.up, axis);
                if (a < angle) { angle = a; best = s; }
            }
            return best;
        }

        void Update()
        {
            if (!Showing || !root) return;
            float dt = Time.unscaledDeltaTime;
            Vector2 m = input ? input.Move : Vector2.zero;
            if (m.sqrMagnitude > 0.01f)
            {
                virion.Rotate(root.up, -m.x * turnSpeed * dt, Space.World);
                virion.Rotate(root.right, m.y * turnSpeed * dt, Space.World);
            }
            var al = Aligned(out _);
            foreach (var s in spikes)
            {
                Color c = s.locked ? new Color(1f, 0.8f, 0.3f) : s == al ? (s.shape == KeyShape ? new Color(1f, 0.9f, 0.5f) : new Color(1f, 0.5f, 0.4f)) : new Color(0.75f, 0.9f, 1f);
                Glow(s.tipR, c);
                s.tip.localScale = (s.shape == 2 ? new Vector3(TipSize(2), TipSize(2), 0.04f) : Vector3.one * TipSize(s.shape)) * (s == al ? 1.35f : 1f);
            }
            flash = Mathf.MoveTowards(flash, 0, dt * 2f);
            Color gc = al == null ? new Color(0.5f, 0.8f, 1f, 0.5f) : al.shape == KeyShape ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.45f, 0.35f);
            Glow(guideR, Color.Lerp(gc, flashColor, flash));
            guide.localScale = Vector3.one * 0.045f * (1f + 0.15f * flash);
            antibody.localPosition = new Vector3(0.3f + 0.02f * Mathf.Sin(Time.unscaledTime * 2f), 0, 0);
            if (input && (input.ToolDown || input.ScanDown)) TryDock();
        }

        /// <summary>Dock the antibody on the aligned spike (also used by tests).</summary>
        public bool TryDock()
        {
            var al = Aligned(out _);
            if (al == null) { flash = 0.6f; flashColor = new Color(0.5f, 0.8f, 1f); audioDirector?.Play("UI_Hover", 0.6f, 0.8f); return false; }
            if (al.shape != KeyShape)
            {
                Mistakes++; flash = 1f; flashColor = Bvr2Style.Danger;
                audioDirector?.Play("SFX_Error", 0.7f); Bvr2Haptics.Pulse(false, 0.4f, 0.1f);
                fx?.Burst(al.tip.position, Bvr2Style.Danger, 12, 0.6f, 0.012f, 0.4f);
                return false;
            }
            al.locked = true; Locks++; flash = 1f; flashColor = new Color(1f, 0.85f, 0.4f);
            var y = Part("Docked Antibody", antibodyMesh, antibodyMaterial, al.root, new Vector3(0, 0.23f, 0), Vector3.one * 0.06f);
            y.transform.localRotation = Quaternion.Euler(180, 0, 0);
            audioDirector?.Play("BVR2_Lock", 0.9f, 0.9f + Locks * 0.1f); Bvr2Haptics.Pulse(false, 0.6f, 0.12f);
            fx?.Burst(al.tip.position, new Color(1f, 0.85f, 0.4f), 24, 0.8f, 0.012f, 0.6f);
            return true;
        }

        /// <summary>Test hook: turn the next unlocked key spike toward the antibody.</summary>
        public void AutoAlign()
        {
            if (!root) return;
            foreach (var s in spikes)
            {
                if (s.locked || s.shape != KeyShape) continue;
                Vector3 axis = (antibody.position - virion.position).normalized;
                virion.rotation = Quaternion.FromToRotation(s.root.up, axis) * virion.rotation;
                return;
            }
        }

        /// <summary>Test hook: point a wrong spike at the antibody (to check the bounce).</summary>
        public void AutoAlignWrong()
        {
            if (!root) return;
            foreach (var s in spikes)
            {
                if (s.locked || s.shape == KeyShape) continue;
                Vector3 axis = (antibody.position - virion.position).normalized;
                virion.rotation = Quaternion.FromToRotation(s.root.up, axis) * virion.rotation;
                return;
            }
        }
    }
}
