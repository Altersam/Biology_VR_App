using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.V2
{
    /// <summary>
    /// "Knowledge moment": after the first scan of something, time slows, a small hologram of it
    /// unfolds next to the player with three callouts, and the professor says one sentence.
    /// Replaces the old wall of text: 4–6 seconds, then the entry is in the codex.
    /// </summary>
    public sealed class Bvr2Diorama : MonoBehaviour
    {
        public Bvr2Rig rig;
        public Bvr2Input input;
        public Bvr2Audio audioDirector;
        public Bvr2Fx fx;
        public Material holoMaterial, cellRed, cellWhite, cellPlatelet, tissueMaterial, glowMaterial, fibrinMaterial, lipidMaterial;
        public Mesh redMesh, whiteMesh, plateletMesh, sphereMesh, ringMesh, torusMesh, virusMesh, virusHalfA, virusHalfB, antibodyMesh;
        public Material virusMaterial, antibodyMaterial, macrophageMaterial;
        public float duration = 6f;

        public bool Showing { get; private set; }
        /// <summary>World position of the hologram (valid while Showing) – the professor presents it.</summary>
        public Vector3 Anchor { get; private set; }
        /// <summary>Which side of the hologram the professor should stand on (-1 left, +1 right).</summary>
        public float PresenterSide { get; private set; } = -1;
        public string CurrentId { get; private set; } = "";
        Transform root; readonly List<GameObject> spawned = new List<GameObject>();
        Action<float> animate;

        public IEnumerator Show(Bvr2Codex.Entry e, Vector3 near)
        {
            Showing = true; CurrentId = e.id;
            var head = rig.head.transform;
            Vector3 toNear = near - head.position;
            Vector3 fwd = Vector3.ProjectOnPlane(head.forward, head.up).normalized; if (fwd.sqrMagnitude < 0.01f) fwd = head.forward;
            Vector3 flat = fwd; PresenterSide = -1;
            // the thing just scanned is right in front (a wall layer, a close cell): stand beside it, not on top of it
            Vector3 nearFlat = Vector3.ProjectOnPlane(toNear, head.up);
            if (toNear.magnitude < 3.5f && nearFlat.sqrMagnitude > 0.01f && Vector3.Angle(fwd, nearFlat) < 30f)
            {
                flat = Quaternion.AngleAxis(28f, head.up) * nearFlat.normalized; PresenterSide = 1;
            }
            Vector3 pos = head.position + flat * 1.15f - head.up * 0.05f;
            Anchor = pos;
            root = new GameObject("Diorama " + e.id).transform; root.SetParent(rig.transform, false);
            root.position = pos; root.rotation = Quaternion.LookRotation(pos - head.position, head.up);
            Bvr2Time.SlowMo(0.12f, duration);
            audioDirector?.Play("SFX_Reveal", 0.8f);
            fx?.Shock(pos, root.forward, e.color, 0.05f, 0.6f, 0.5f);
            // a wall layer is already in front of the player as a big exhibit: it *is* the model,
            // so only the callouts appear, right next to it
            bool modelInWorld = e.diorama == "layers" && PresenterSide > 0;
            if (!modelInWorld) BuildModel(e); else animate = null;
            var labels = BuildCallouts(e, modelInWorld);
            // focus: the world beyond the hologram darkens, nearby cells drift aside
            if (Bvr2Environment.Current) Bvr2Environment.Current.focusDimTarget = 1f;
            var blood = Bvr2Game.Instance ? Bvr2Game.Instance.blood : null;
            if (blood) blood.SetFocus(pos, 0.9f);
            float t = 0; bool skipped = false;
            while (t < duration && !skipped)
            {
                t += Time.unscaledDeltaTime;
                float appear = Mathf.Clamp01(t / 0.45f);
                root.localScale = Vector3.one * Mathf.SmoothStep(0.2f, 1f, appear);
                animate?.Invoke(t);
                for (int i = 0; i < labels.Count; i++) labels[i].alpha = Mathf.Clamp01((t - 0.5f - i * 0.55f) / 0.35f);
                if (t > 1.6f && (input.ToolDown || input.ScanDown)) skipped = true;
                // gently keep it in front if the player turns away
                Vector3 want = head.position + Vector3.ProjectOnPlane(head.forward, head.up).normalized * 1.15f;
                if (Vector3.Angle(root.position - head.position, head.forward) > 42f) root.position = Vector3.Lerp(root.position, want, Time.unscaledDeltaTime * 2f);
                root.rotation = Quaternion.LookRotation(root.position - head.position, head.up);
                Anchor = root.position; if (blood) blood.SetFocus(root.position, 0.9f);
                yield return null;
            }
            Bvr2Time.Release();
            if (Bvr2Environment.Current) Bvr2Environment.Current.focusDimTarget = 0f;
            if (blood) blood.ClearFocus();
            float f = 0;
            while (f < 0.35f) { f += Time.unscaledDeltaTime; root.localScale = Vector3.one * Mathf.Lerp(1f, 0.01f, f / 0.35f); yield return null; }
            Destroy(root.gameObject); spawned.Clear(); animate = null;
            Showing = false; CurrentId = "";
        }

        /// <summary>Drops a knowledge moment that was interrupted (chapter restart).</summary>
        public void Cleanup()
        {
            if (root) Destroy(root.gameObject);
            spawned.Clear(); animate = null; Showing = false; CurrentId = "";
            Bvr2Time.Release();
            if (Bvr2Environment.Current) Bvr2Environment.Current.focusDimTarget = 0f;
            var blood = Bvr2Game.Instance ? Bvr2Game.Instance.blood : null; if (blood) blood.ClearFocus();
        }

        GameObject Part(string name, Mesh mesh, Material mat, Vector3 local, float scale, Transform parent = null)
        {
            var go = new GameObject(name); go.transform.SetParent(parent ? parent : root, false);
            go.transform.localPosition = local; go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; spawned.Add(go); return go;
        }

        void BuildModel(Bvr2Codex.Entry e)
        {
            var pivot = new GameObject("Model").transform; pivot.SetParent(root, false); pivot.localPosition = new Vector3(-0.27f, 0, 0);
            var pedestal = Part("Pedestal", ringMesh, glowMaterial, new Vector3(0, -0.2f, 0), 0.2f, pivot); pedestal.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var mpb = new MaterialPropertyBlock(); mpb.SetColor("_Color", e.color); pedestal.GetComponent<Renderer>().SetPropertyBlock(mpb);
            animate = null;
            switch (e.diorama)
            {
                case "rbc":
                {
                    var cell = Part("RBC", redMesh, cellRed, Vector3.zero, 0.16f, pivot); Glow(cell, 0.35f);
                    var o2 = new List<Transform>();
                    var blue = new MaterialPropertyBlock(); blue.SetColor("_Color", new Color(0.45f, 0.75f, 1f, 1f));
                    for (int i = 0; i < 10; i++) { var d = Part("O2", sphereMesh, glowMaterial, Vector3.zero, 0.009f, pivot); d.GetComponent<Renderer>().SetPropertyBlock(blue); o2.Add(d.transform); }
                    animate = t =>
                    {
                        cell.transform.localRotation = Quaternion.Euler(60 + Mathf.Sin(t) * 20, t * 50, 0);
                        for (int i = 0; i < o2.Count; i++)
                        {
                            float a = i / (float)o2.Count * Mathf.PI * 2 + t * 0.8f;
                            float k = Mathf.Clamp01((t - 1.2f - i * 0.12f) / 0.8f); // oxygen settles onto the cell
                            o2[i].localPosition = Vector3.Lerp(new Vector3(Mathf.Cos(a) * 0.32f, Mathf.Sin(a * 1.3f) * 0.12f, Mathf.Sin(a) * 0.32f), new Vector3(Mathf.Cos(a) * 0.13f, 0.03f * Mathf.Sign(Mathf.Sin(a * 3)), Mathf.Sin(a) * 0.13f), k);
                        }
                    };
                    break;
                }
                case "wbc":
                {
                    var cell = Part("WBC", whiteMesh, holoMaterial, Vector3.zero, 0.17f, pivot);
                    var nuc = new List<Transform>();
                    for (int i = 0; i < 3; i++) nuc.Add(Part("Nucleus", sphereMesh, cellPlatelet, new Vector3((i - 1) * 0.055f, Mathf.Sin(i * 2f) * 0.03f, 0), 0.05f, pivot).transform);
                    animate = t => { cell.transform.localRotation = Quaternion.Euler(0, t * 30, 0); cell.transform.localScale = Vector3.one * 0.17f * (1f + 0.06f * Mathf.Sin(t * 3f)); foreach (var n in nuc) n.localRotation = Quaternion.Euler(0, t * 30, 0); };
                    break;
                }
                case "platelet":
                {
                    var cell = Part("Platelet", plateletMesh, cellPlatelet, Vector3.zero, 0.13f, pivot);
                    var r = cell.GetComponent<Renderer>(); var b = new MaterialPropertyBlock();
                    animate = t =>
                    {
                        float morph = Mathf.Clamp01((t - 1.5f) / 1.2f);
                        b.SetVector("_Inst", new Vector4(1, 0.3f, morph, 0)); r.SetPropertyBlock(b);
                        cell.transform.localRotation = Quaternion.Euler(70, t * 40, 0);
                    };
                    break;
                }
                case "plasma":
                {
                    var bits = new List<Transform>(); var colors = new[] { new Color(0.5f, 0.85f, 1f), new Color(1f, 0.85f, 0.4f), new Color(0.6f, 1f, 0.6f), new Color(1f, 0.6f, 0.9f) };
                    for (int i = 0; i < 26; i++)
                    {
                        var p = Part("Molecule", sphereMesh, glowMaterial, UnityEngine.Random.insideUnitSphere * 0.2f, i < 14 ? 0.012f : 0.022f, pivot);
                        var m = new MaterialPropertyBlock(); m.SetColor("_Color", colors[i % colors.Length]); p.GetComponent<Renderer>().SetPropertyBlock(m);
                        bits.Add(p.transform);
                    }
                    animate = t => { for (int i = 0; i < bits.Count; i++) bits[i].localPosition += new Vector3(Mathf.Sin(t * 2 + i), Mathf.Cos(t * 1.7f + i * 2), Mathf.Sin(t * 1.3f + i * 3)) * 0.0015f; };
                    break;
                }
                case "layers":
                {
                    var a = Part("Adventitia", Bvr2Meshes.Shell(0.17f, 0.21f, 0.18f, 200, 340, 24), tissueMaterial, Vector3.zero, 1f, pivot);
                    var m = Part("Media", Bvr2Meshes.Shell(0.11f, 0.17f, 0.18f, 200, 340, 24), tissueMaterial, Vector3.zero, 1f, pivot);
                    var i = Part("Intima", Bvr2Meshes.Shell(0.095f, 0.11f, 0.18f, 200, 340, 24), tissueMaterial, Vector3.zero, 1f, pivot);
                    Tint(a, new Color(0.95f, 0.88f, 0.7f)); Tint(m, new Color(0.9f, 0.32f, 0.3f)); Tint(i, new Color(1f, 0.6f, 0.62f));
                    string focus = e.id;
                    animate = t =>
                    {
                        pivot.localRotation = Quaternion.Euler(-20, 20 + Mathf.Sin(t * 0.6f) * 20, 0);
                        float pop = Mathf.Clamp01((t - 0.6f) / 0.6f) * 0.05f;
                        a.transform.localPosition = new Vector3(0, focus == "adventitia" ? -pop : 0, 0);
                        m.transform.localPosition = new Vector3(0, focus == "media" ? -pop : 0, 0);
                        i.transform.localPosition = new Vector3(0, focus == "intima" ? -pop : 0, 0);
                    };
                    break;
                }
                case "flow":
                {
                    // two vessels side by side: normal and narrowed by 20 %; the dots are blood going through.
                    var normal = Tube(pivot, new Vector3(0, 0.1f, 0), 9, 0.055f, Bvr2Style.Ally, null);
                    var narrow = Tube(pivot, new Vector3(0, -0.1f, 0), 9, 0.055f, Bvr2Style.Danger, i => i >= 3 && i <= 5 ? 0.8f : 1f);
                    var dotsA = Dots(pivot, 14, new Vector3(0, 0.1f, 0), new Color(1f, 0.35f, 0.35f));
                    var dotsB = Dots(pivot, 6, new Vector3(0, -0.1f, 0), new Color(1f, 0.35f, 0.35f));
                    pivot.localRotation = Quaternion.Euler(8, -28, 0);
                    animate = t =>
                    {
                        float squeeze = Mathf.Clamp01((t - 0.6f) / 1.0f);
                        for (int i = 3; i <= 5; i++) narrow[i].localScale = Vector3.one * 0.055f * Mathf.Lerp(1f, 0.8f, squeeze);
                        MoveDots(dotsA, t, 0.13f, 0.03f);
                        MoveDots(dotsB, t, 0.13f, 0.024f);
                    };
                    break;
                }
                case "plaque":
                {
                    // a slice of artery wall with the plaque: yellow fat under a see-through fibrous cap
                    var wall = Part("Wall", Bvr2Meshes.Shell(0.15f, 0.2f, 0.26f, 200, 340, 24), tissueMaterial, Vector3.zero, 1f, pivot); Tint(wall, new Color(0.85f, 0.3f, 0.3f));
                    var core = Part("Lipid core", sphereMesh, lipidMaterial ? lipidMaterial : tissueMaterial, new Vector3(0, -0.13f, 0), 1f, pivot);
                    core.transform.localScale = new Vector3(0.13f, 0.05f, 0.1f);
                    var cap = Part("Fibrous cap", sphereMesh, holoMaterial, new Vector3(0, -0.125f, 0), 1f, pivot);
                    cap.transform.localScale = new Vector3(0.17f, 0.075f, 0.13f);
                    pivot.localRotation = Quaternion.Euler(-25, 25, 0); pivot.localPosition += new Vector3(0, 0.08f, 0);
                    animate = t =>
                    {
                        float grow = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.4f) / 1.6f));
                        core.transform.localScale = new Vector3(0.13f, 0.05f, 0.1f) * Mathf.Lerp(0.3f, 1f, grow);
                        cap.transform.localScale = new Vector3(0.17f, 0.075f, 0.13f) * Mathf.Lerp(0.4f, 1f, grow);
                        pivot.localRotation = Quaternion.Euler(-25, 25 + Mathf.Sin(t * 0.6f) * 15f, 0);
                    };
                    break;
                }
                case "foam":
                {
                    var cell = Part("Macrophage", whiteMesh, holoMaterial, Vector3.zero, 0.17f, pivot);
                    var drops = new List<Transform>();
                    for (int i = 0; i < 9; i++)
                        drops.Add(Part("Fat", sphereMesh, lipidMaterial ? lipidMaterial : tissueMaterial, UnityEngine.Random.insideUnitSphere * 0.06f, UnityEngine.Random.Range(0.025f, 0.045f), pivot).transform);
                    animate = t => { cell.transform.localRotation = Quaternion.Euler(0, t * 25, 0); for (int i = 0; i < drops.Count; i++) drops[i].localPosition += new Vector3(Mathf.Sin(t * 1.3f + i), Mathf.Cos(t + i * 2), Mathf.Sin(t * 0.9f + i * 3)) * 0.0004f; };
                    break;
                }
                case "thrombus":
                case "lysis":
                {
                    var go = new GameObject("Clot"); go.transform.SetParent(pivot, false); spawned.Add(go);
                    var clot = go.AddComponent<Bvr2Clot>();
                    clot.redMesh = redMesh; clot.plateletMesh = plateletMesh; clot.redMaterial = cellRed; clot.plateletMaterial = cellPlatelet; clot.fibrinMaterial = fibrinMaterial;
                    clot.size = 0.3f; clot.dome = false; clot.redCount = 14; clot.plateletCount = 16; clot.fibrinCount = 18; clot.Build();
                    bool lysis = e.diorama == "lysis";
                    clot.growth = lysis ? 1f : 0f;
                    animate = t =>
                    {
                        if (lysis) clot.dissolve = Mathf.Clamp01((t - 1.0f) / 3f); else clot.growth = Mathf.Clamp01((t - 0.3f) / 2.6f);
                        go.transform.localRotation = Quaternion.Euler(10, t * 20, 0);
                    };
                    break;
                }
                case "embolus":
                case "ischemia":
                {
                    // the vessel narrows to the right; a clot drifts along and gets stuck
                    var rings = Tube(pivot, Vector3.zero, 11, 0.06f, new Color(1f, 0.45f, 0.5f), i => Mathf.Lerp(1f, 0.38f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(5, 9, i))));
                    var lump = Part("Embolus", sphereMesh, tissueMaterial, new Vector3(-0.2f, 0, 0), 0.035f, pivot); Tint(lump, new Color(0.55f, 0.08f, 0.1f));
                    bool isch = e.diorama == "ischemia";
                    pivot.localRotation = Quaternion.Euler(8, -28, 0);
                    animate = t =>
                    {
                        float x = isch ? 0.12f : Mathf.Lerp(-0.2f, 0.12f, Mathf.Clamp01((t - 0.3f) / 2.4f));
                        lump.transform.localPosition = new Vector3(x, Mathf.Sin(t * 5f) * 0.004f, 0);
                        float stuck = isch ? 1f : Mathf.Clamp01((t - 2.7f) / 0.8f);
                        // beyond the plug the tissue goes dark: no oxygen
                        for (int i = 0; i < rings.Count; i++)
                        {
                            var b = new MaterialPropertyBlock();
                            float dark = i >= 9 ? stuck : 0f;
                            b.SetColor("_Color", Color.Lerp(new Color(1f, 0.45f, 0.5f), new Color(0.3f, 0.35f, 0.55f), dark * (0.6f + 0.4f * Mathf.Sin(t * 4f))));
                            rings[i].GetComponent<Renderer>().SetPropertyBlock(b);
                        }
                    };
                    break;
                }
                case "virus":
                {
                    // the capsid opens: two halves part and the genetic program glows inside
                    var a = Part("Capsid A", virusHalfA ? virusHalfA : virusMesh, virusMaterial, Vector3.zero, 0.13f, pivot);
                    var b = Part("Capsid B", virusHalfB ? virusHalfB : virusMesh, virusMaterial, Vector3.zero, 0.13f, pivot);
                    var rna = new List<Transform>(); var gb = new MaterialPropertyBlock(); gb.SetColor("_Color", new Color(1f, 0.6f, 0.9f));
                    for (int i = 0; i < 14; i++) { var d = Part("RNA", sphereMesh, glowMaterial, Vector3.zero, 0.012f, pivot); d.GetComponent<Renderer>().SetPropertyBlock(gb); rna.Add(d.transform); }
                    animate = t =>
                    {
                        float open = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.8f) / 1.2f)) * 0.07f;
                        a.transform.localPosition = Vector3.up * open; b.transform.localPosition = Vector3.down * open;
                        pivot.localRotation = Quaternion.Euler(15, t * 25, 0);
                        for (int i = 0; i < rna.Count; i++) { float u = i / (float)rna.Count * Mathf.PI * 3f + t; rna[i].localPosition = new Vector3(Mathf.Cos(u) * 0.035f, (i / (float)rna.Count - 0.5f) * 0.09f, Mathf.Sin(u) * 0.035f); }
                    };
                    break;
                }
                case "antigen":
                {
                    // a close-up of the crown: an antibody docks on the tip of one spike (the epitope)
                    var v = Part("Virus", virusMesh, virusMaterial, new Vector3(0, -0.22f, 0), 0.24f, pivot);
                    var y = Part("Antibody", antibodyMesh, antibodyMaterial ? antibodyMaterial : glowMaterial, new Vector3(0, 0.2f, 0), 0.08f, pivot);
                    y.transform.localRotation = Quaternion.Euler(180, 0, 0);
                    animate = t =>
                    {
                        float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.6f) / 1.6f));
                        y.transform.localPosition = Vector3.Lerp(new Vector3(0.05f, 0.24f, 0), new Vector3(0, 0.1f, 0), k);
                        v.transform.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 0.5f) * 10f, 0);
                    };
                    break;
                }
                case "antibody":
                {
                    var v = Part("Virus", virusMesh, virusMaterial, Vector3.zero, 0.1f, pivot);
                    var ys = new List<Transform>(); var dirs = new List<Vector3>();
                    for (int i = 0; i < 8; i++)
                    {
                        var d = UnityEngine.Random.onUnitSphere; dirs.Add(d);
                        var y = Part("Antibody", antibodyMesh, antibodyMaterial ? antibodyMaterial : glowMaterial, d * 0.3f, 0.045f, pivot);
                        y.transform.localRotation = Quaternion.FromToRotation(Vector3.up, -d); ys.Add(y.transform);
                    }
                    animate = t =>
                    {
                        for (int i = 0; i < ys.Count; i++) { float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.5f - i * 0.25f) / 0.6f)); ys[i].localPosition = dirs[i] * Mathf.Lerp(0.3f, 0.145f, k); }
                        pivot.localRotation = Quaternion.Euler(10, t * 20, 0);
                    };
                    break;
                }
                case "phagocytosis":
                case "macrophage":
                {
                    var cell = Part("Macrophage", whiteMesh, macrophageMaterial ? macrophageMaterial : cellWhite, new Vector3(-0.05f, 0, 0), 0.13f, pivot);
                    var vir = Part("Virus", virusMesh, virusMaterial, new Vector3(0.16f, 0.02f, 0), 0.035f, pivot);
                    var cb = new MaterialPropertyBlock();
                    bool mac = e.diorama == "macrophage";
                    animate = t =>
                    {
                        float k = Mathf.Clamp01((t - 0.8f) / 2f);
                        vir.transform.localPosition = Vector3.Lerp(new Vector3(0.16f, 0.02f, 0), new Vector3(-0.03f, 0, 0), Mathf.SmoothStep(0, 1, k));
                        vir.transform.localScale = Vector3.one * 0.035f * Mathf.Lerp(1f, 0.3f, Mathf.Clamp01((t - 2.2f) / 1.5f));
                        cb.SetVector("_Inst", new Vector4(0.6f, 0f, mac ? 0.6f + 0.4f * Mathf.Sin(t * 2f) : 0.3f + 0.7f * k, 0.3f)); cell.GetComponent<Renderer>().SetPropertyBlock(cb);
                        cell.transform.localRotation = Quaternion.Euler(0, t * 15, 0);
                    };
                    break;
                }
                case "fever":
                {
                    // a thermometer of glowing rings fills up; the virus beside it slows down
                    var rings = new List<Transform>(); 
                    for (int i = 0; i < 10; i++) { var r = Part("Mercury", sphereMesh, glowMaterial, new Vector3(-0.08f, -0.16f + i * 0.033f, 0), 0.022f, pivot); rings.Add(r.transform); }
                    var v = Part("Virus", virusMesh, virusMaterial, new Vector3(0.1f, 0, 0), 0.05f, pivot);
                    animate = t =>
                    {
                        float fill = Mathf.Clamp01((t - 0.4f) / 2.4f);
                        for (int i = 0; i < rings.Count; i++)
                        {
                            bool on = i / (float)rings.Count < fill;
                            var b = new MaterialPropertyBlock(); b.SetColor("_Color", on ? Color.Lerp(new Color(1f, 0.8f, 0.3f), new Color(1f, 0.25f, 0.15f), i / 9f) : new Color(0.3f, 0.4f, 0.5f, 0.4f));
                            rings[i].GetComponent<Renderer>().SetPropertyBlock(b);
                        }
                        v.transform.localPosition = new Vector3(0.1f, Mathf.Sin(t * Mathf.Lerp(6f, 1.5f, fill)) * 0.03f, 0);
                        v.transform.localRotation = Quaternion.Euler(0, t * Mathf.Lerp(120f, 20f, fill), 0);
                    };
                    break;
                }
                case "pulse":
                {
                    // a little artery with the systolic bulge running along it, and the heart that sends it
                    var rings = Tube(pivot, Vector3.zero, 13, 0.05f, new Color(1f, 0.45f, 0.5f), null);
                    var heart = Part("Heart", sphereMesh, glowMaterial, new Vector3(-0.27f, 0, 0), 0.05f, pivot);
                    var hb = new MaterialPropertyBlock(); hb.SetColor("_Color", new Color(1f, 0.25f, 0.3f)); heart.GetComponent<Renderer>().SetPropertyBlock(hb);
                    pivot.localRotation = Quaternion.Euler(10, -30, 0);
                    animate = t =>
                    {
                        float beat = t * 1.15f;
                        heart.transform.localScale = Vector3.one * 0.05f * (1f + 0.45f * Bvr2Math.PulseShape(beat));
                        for (int i = 0; i < rings.Count; i++) rings[i].localScale = Vector3.one * 0.05f * (1f + 0.4f * Bvr2Math.PulseShape(beat - 0.06f - i * 0.045f));
                    };
                    break;
                }
                case "wound":
                case "plug":
                case "healing":
                {
                    // a slice of wall with a tear: collagen fibres in the gap; cells leak (wound),
                    // platelets stick rims-first (plug) or the gap zips closed (healing)
                    string kind = e.diorama;
                    var left = Part("Wall L", sphereMesh, tissueMaterial, new Vector3(-0.12f, 0, 0), 1f, pivot); left.transform.localScale = new Vector3(0.19f, 0.03f, 0.22f);
                    var right = Part("Wall R", sphereMesh, tissueMaterial, new Vector3(0.12f, 0, 0), 1f, pivot); right.transform.localScale = new Vector3(0.19f, 0.03f, 0.22f);
                    Tint(left, new Color(1f, 0.6f, 0.58f)); Tint(right, new Color(1f, 0.6f, 0.58f));
                    var fibres = new List<Transform>(); var cb = new MaterialPropertyBlock(); cb.SetColor("_Color", new Color(1f, 0.9f, 0.72f, 0.9f));
                    for (int i = 0; i < 7; i++)
                    {
                        var f = Part("Collagen", sphereMesh, glowMaterial, new Vector3((i - 3) * 0.007f, -0.03f - (i % 2) * 0.006f, 0), 1f, pivot);
                        f.transform.localScale = new Vector3(0.004f, 0.004f, 0.19f); f.transform.localRotation = Quaternion.Euler(0, (i - 3) * 0.8f, 0);
                        f.GetComponent<Renderer>().SetPropertyBlock(cb); fibres.Add(f.transform);
                    }
                    var cells = new List<Transform>();
                    if (kind == "wound")
                        for (int i = 0; i < 7; i++) { var c = Part("Leak", redMesh, cellRed, Vector3.zero, 0.028f, pivot); cells.Add(c.transform); }
                    var plates = new List<Transform>(); var homes = new List<Vector3>(); var starts = new List<Vector3>();
                    if (kind == "plug")
                        for (int i = 0; i < 14; i++)
                        {
                            // rims first (|x| near the edge), then towards the middle
                            float rank = i / 13f, side = i % 2 == 0 ? -1 : 1;
                            homes.Add(new Vector3(side * Mathf.Lerp(0.055f, 0.008f, rank), 0.012f + rank * 0.01f, Mathf.Sin(i * 1.9f) * 0.07f));
                            starts.Add(new Vector3(Mathf.Sin(i * 2.7f) * 0.15f, 0.2f + (i % 3) * 0.04f, Mathf.Cos(i * 1.3f) * 0.12f));
                            var p = Part("Platelet", plateletMesh, cellPlatelet, starts[i], 0.022f, pivot); plates.Add(p.transform);
                        }
                    pivot.localRotation = Quaternion.Euler(28, -20, 0);
                    animate = t =>
                    {
                        float gap = 0.06f;
                        if (kind == "healing") gap = Mathf.Lerp(0.06f, 0f, Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.8f) / 2.8f)));
                        left.transform.localPosition = new Vector3(-0.095f - gap * 0.5f, 0, 0); right.transform.localPosition = new Vector3(0.095f + gap * 0.5f, 0, 0);
                        for (int i = 0; i < cells.Count; i++)
                        {
                            float k = Mathf.Repeat(t * 0.6f + i / (float)cells.Count, 1f);
                            cells[i].localPosition = new Vector3(Mathf.Sin(i * 3.1f) * 0.012f, Mathf.Lerp(0.16f, -0.16f, k), Mathf.Sin(i * 1.7f) * 0.08f);
                            cells[i].localRotation = Quaternion.Euler(t * 90f + i * 30f, i * 50f, 0);
                        }
                        for (int i = 0; i < plates.Count; i++)
                        {
                            float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.4f - i * 0.22f) / 0.6f));
                            plates[i].localPosition = Vector3.Lerp(starts[i], homes[i], k);
                            plates[i].localScale = Vector3.Lerp(Vector3.one * 0.022f, new Vector3(0.03f, 0.012f, 0.03f), k); // flattens when it sticks
                        }
                        if (kind == "healing")
                        {
                            var fb = new MaterialPropertyBlock(); fb.SetColor("_Color", new Color(1f, 0.9f, 0.72f, Mathf.Lerp(0.9f, 0.05f, Mathf.Clamp01((t - 1.5f) / 2.5f))));
                            foreach (var f in fibres) f.GetComponent<Renderer>().SetPropertyBlock(fb);
                        }
                    };
                    break;
                }
                case "spasm":
                {
                    // the muscle ring squeezes the middle of a vessel: less blood gets through
                    var rings = Tube(pivot, Vector3.zero, 11, 0.06f, new Color(1f, 0.5f, 0.45f), null);
                    var dots = Dots(pivot, 12, Vector3.zero, new Color(1f, 0.35f, 0.35f));
                    pivot.localRotation = Quaternion.Euler(8, -28, 0);
                    animate = t =>
                    {
                        float squeeze = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.6f) / 1.4f));
                        for (int i = 0; i < rings.Count; i++)
                        {
                            float mid = Mathf.Exp(-Mathf.Pow((i - 5) / 2.2f, 2f));
                            rings[i].localScale = Vector3.one * 0.06f * (1f - 0.5f * squeeze * mid);
                        }
                        MoveDots(dots, t, Mathf.Lerp(0.12f, 0.04f, squeeze), Mathf.Lerp(0.035f, 0.015f, squeeze));
                    };
                    break;
                }
                case "fibrin":
                {
                    // three links light up in order (thromboplastin → thrombin → fibrin), then threads weave the clot
                    var nodes = new List<GameObject>();
                    for (int i = 0; i < 3; i++) nodes.Add(Part("Link", sphereMesh, glowMaterial, new Vector3(-0.13f + i * 0.13f, 0.17f, 0), 0.03f, pivot));
                    var cluster = new List<Transform>();
                    for (int i = 0; i < 7; i++) cluster.Add(Part("Platelet", plateletMesh, cellPlatelet, UnityEngine.Random.insideUnitSphere * 0.05f, 0.03f, pivot).transform);
                    var threads = new List<GameObject>();
                    for (int i = 0; i < 12; i++)
                    {
                        var th = Part("Fibrin", sphereMesh, glowMaterial, Vector3.zero, 1f, pivot);
                        th.transform.localScale = new Vector3(0.004f, 0.004f, 0.0001f); th.transform.localRotation = UnityEngine.Random.rotation; threads.Add(th);
                    }
                    var trapped = new List<GameObject>();
                    for (int i = 0; i < 4; i++) { var r = Part("Trapped", redMesh, cellRed, UnityEngine.Random.insideUnitSphere * 0.08f, 0.0001f, pivot); trapped.Add(r); }
                    animate = t =>
                    {
                        for (int i = 0; i < nodes.Count; i++)
                        {
                            bool on = t > 0.5f + i * 0.7f;
                            var b = new MaterialPropertyBlock(); b.SetColor("_Color", on ? new Color(1f, 0.85f, 0.4f) : new Color(0.35f, 0.45f, 0.55f, 0.5f));
                            nodes[i].GetComponent<Renderer>().SetPropertyBlock(b);
                            nodes[i].transform.localScale = Vector3.one * 0.03f * (on ? 1f + 0.15f * Mathf.Sin(t * 6f + i) : 0.8f);
                        }
                        var tb = new MaterialPropertyBlock(); tb.SetColor("_Color", new Color(1f, 0.93f, 0.7f));
                        for (int i = 0; i < threads.Count; i++)
                        {
                            float k = Mathf.Clamp01((t - 2.4f - i * 0.12f) / 0.5f);
                            threads[i].transform.localScale = new Vector3(0.004f, 0.004f, Mathf.Max(0.0001f, 0.2f * k));
                            threads[i].GetComponent<Renderer>().SetPropertyBlock(tb);
                        }
                        for (int i = 0; i < trapped.Count; i++) trapped[i].transform.localScale = Vector3.one * 0.026f * Mathf.Clamp01((t - 3.6f - i * 0.2f) / 0.4f);
                        pivot.localRotation = Quaternion.Euler(10, t * 18f, 0);
                    };
                    break;
                }
                case "shock":
                {
                    // the heart beats faster and faster while the blood volume column drains
                    var heart = Part("Heart", sphereMesh, glowMaterial, new Vector3(-0.1f, 0, 0), 0.06f, pivot);
                    var column = new List<GameObject>();
                    for (int i = 0; i < 10; i++) column.Add(Part("Volume", sphereMesh, glowMaterial, new Vector3(0.12f, -0.16f + i * 0.034f, 0), 0.024f, pivot));
                    float beat = 0;
                    animate = t =>
                    {
                        float loss = Mathf.Clamp01((t - 0.4f) / 3.2f);
                        beat += Time.unscaledDeltaTime * Mathf.Lerp(1.2f, 2.4f, loss);
                        var hb = new MaterialPropertyBlock(); hb.SetColor("_Color", new Color(1f, 0.25f, 0.3f)); heart.GetComponent<Renderer>().SetPropertyBlock(hb);
                        heart.transform.localScale = Vector3.one * 0.06f * (1f + 0.4f * Bvr2Math.PulseShape(beat));
                        for (int i = 0; i < column.Count; i++)
                        {
                            bool full = i / (float)column.Count < 1f - loss * 0.35f;
                            var b = new MaterialPropertyBlock(); b.SetColor("_Color", full ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 0.35f, 0.45f, 0.4f));
                            column[i].GetComponent<Renderer>().SetPropertyBlock(b);
                        }
                    };
                    break;
                }
                case "infectedcell":
                {
                    // a swollen cell on a strip of wall; virions bud off its top, flags appear on its surface
                    var wall = Part("Wall", sphereMesh, tissueMaterial, new Vector3(0, -0.06f, 0), 1f, pivot); wall.transform.localScale = new Vector3(0.26f, 0.025f, 0.16f); Tint(wall, new Color(1f, 0.6f, 0.58f));
                    var cell = Part("Cell", sphereMesh, tissueMaterial, new Vector3(0, -0.03f, 0), 1f, pivot); cell.transform.localScale = new Vector3(0.1f, 0.07f, 0.1f); Tint(cell, new Color(0.72f, 0.5f, 0.82f));
                    var buds = new List<Transform>(); for (int i = 0; i < 4; i++) buds.Add(Part("Virion", virusMesh, virusMaterial, Vector3.zero, 0.0001f, pivot).transform);
                    var flagsList = new List<GameObject>(); var fb = new MaterialPropertyBlock(); fb.SetColor("_Color", new Color(0.75f, 1f, 0.4f));
                    for (int i = 0; i < 4; i++) { var f = Part("Flag", sphereMesh, glowMaterial, new Vector3(Mathf.Cos(i * 1.57f) * 0.06f, 0.035f, Mathf.Sin(i * 1.57f) * 0.06f), 0.0001f, pivot); f.GetComponent<Renderer>().SetPropertyBlock(fb); flagsList.Add(f); }
                    pivot.localRotation = Quaternion.Euler(22, -15, 0);
                    animate = t =>
                    {
                        float swell = Mathf.Clamp01((t - 0.3f) / 1.5f);
                        cell.transform.localScale = new Vector3(0.1f, 0.07f, 0.1f) * (1f + 0.35f * swell + 0.04f * Mathf.Sin(t * 6f));
                        for (int i = 0; i < buds.Count; i++)
                        {
                            float k = Mathf.Repeat(t * 0.45f + i / (float)buds.Count, 1f);
                            buds[i].localPosition = new Vector3(Mathf.Sin(i * 2.1f) * 0.03f, 0.04f + k * 0.18f, Mathf.Cos(i * 2.1f) * 0.03f);
                            buds[i].localScale = Vector3.one * 0.022f * Mathf.Clamp01(k * 4f) * swell;
                        }
                        for (int i = 0; i < flagsList.Count; i++) flagsList[i].transform.localScale = Vector3.one * 0.012f * Mathf.Clamp01((t - 2f - i * 0.25f) / 0.3f);
                    };
                    break;
                }
                case "neutrophil":
                {
                    var cell = Part("Neutrophil", whiteMesh, macrophageMaterial ? macrophageMaterial : cellWhite, Vector3.zero, 0.12f, pivot);
                    var lobes = new List<Transform>();
                    for (int i = 0; i < 4; i++) lobes.Add(Part("Lobe", sphereMesh, cellPlatelet, new Vector3((i - 1.5f) * 0.035f, Mathf.Sin(i * 1.7f) * 0.02f, 0), 0.028f, pivot).transform);
                    var vir = Part("Virus", virusMesh, virusMaterial, new Vector3(0.2f, 0.03f, 0), 0.03f, pivot);
                    animate = t =>
                    {
                        float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 1f) / 1.8f));
                        vir.transform.localPosition = Vector3.Lerp(new Vector3(0.2f, 0.03f, 0), new Vector3(0.02f, 0, 0), k);
                        vir.transform.localScale = Vector3.one * 0.03f * Mathf.Lerp(1f, 0.3f, Mathf.Clamp01((t - 2.4f) / 1.2f));
                        cell.transform.localRotation = Quaternion.Euler(0, t * 20f, 0);
                        foreach (var l in lobes) l.localRotation = Quaternion.Euler(0, t * 20f, 0);
                    };
                    break;
                }
                case "specificity":
                {
                    // two antibodies try the same spike: the wrong one bounces off, the right one locks
                    var v = Part("Virus", virusMesh, virusMaterial, new Vector3(-0.05f, -0.05f, 0), 0.12f, pivot);
                    var right = Part("Antibody", antibodyMesh, antibodyMaterial ? antibodyMaterial : glowMaterial, Vector3.zero, 0.06f, pivot);
                    var wrong = Part("Antibody", antibodyMesh, glowMaterial, Vector3.zero, 0.06f, pivot);
                    var wb = new MaterialPropertyBlock(); wb.SetColor("_Color", new Color(1f, 0.45f, 0.4f)); wrong.GetComponent<Renderer>().SetPropertyBlock(wb);
                    right.transform.localRotation = Quaternion.Euler(0, 0, 135); wrong.transform.localRotation = Quaternion.Euler(0, 0, 135);
                    animate = t =>
                    {
                        float a = Mathf.Clamp01((t - 0.4f) / 0.9f), back = Mathf.Clamp01((t - 1.3f) / 0.6f);
                        wrong.transform.localPosition = Vector3.Lerp(new Vector3(0.22f, 0.2f, 0.02f), new Vector3(0.06f, 0.06f, 0.02f), a - back * 0.9f);
                        wrong.SetActive(t < 2.2f);
                        float r = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 2.2f) / 1f));
                        right.transform.localPosition = Vector3.Lerp(new Vector3(0.24f, 0.22f, 0), new Vector3(0.055f, 0.055f, 0), r);
                        right.SetActive(t > 2f);
                        v.transform.localRotation = Quaternion.Euler(0, Mathf.Sin(t) * 8f, 0);
                    };
                    break;
                }
                case "tkiller":
                case "apoptosis":
                {
                    // a flagged cell; a T-killer docks (tkiller), the cell shrinks and falls apart into vesicles (apoptosis)
                    bool apo = e.diorama == "apoptosis";
                    var cell = Part("Cell", sphereMesh, tissueMaterial, Vector3.zero, 0.1f, pivot); Tint(cell, new Color(0.72f, 0.5f, 0.82f));
                    var flagsList = new List<GameObject>(); var fb = new MaterialPropertyBlock(); fb.SetColor("_Color", new Color(0.75f, 1f, 0.4f));
                    for (int i = 0; i < 5; i++) { var d = Quaternion.Euler(0, i * 72f, 30f) * Vector3.up; var f = Part("Flag", sphereMesh, glowMaterial, d * 0.11f, 0.012f, pivot); f.GetComponent<Renderer>().SetPropertyBlock(fb); flagsList.Add(f); }
                    var tk = Part("T-killer", whiteMesh, cellWhite, new Vector3(0.24f, 0.1f, 0), 0.07f, pivot);
                    var tb = new MaterialPropertyBlock(); tb.SetColor("_BaseColor", new Color(0.7f, 0.78f, 1f)); tk.GetComponent<Renderer>().SetPropertyBlock(tb);
                    var ves = new List<Transform>(); var vd = new List<Vector3>();
                    for (int i = 0; i < 9; i++) { ves.Add(Part("Vesicle", sphereMesh, glowMaterial, Vector3.zero, 0.0001f, pivot).transform); vd.Add(UnityEngine.Random.onUnitSphere); }
                    animate = t =>
                    {
                        float dock = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - (apo ? 0f : 0.5f)) / 1.2f));
                        tk.transform.localPosition = Vector3.Lerp(new Vector3(0.24f, 0.1f, 0), new Vector3(0.14f, 0.05f, 0), apo ? 1f : dock);
                        float die = apo ? Mathf.Clamp01((t - 0.6f) / 1.8f) : Mathf.Clamp01((t - 2.4f) / 1.6f);
                        cell.transform.localScale = Vector3.one * 0.1f * Mathf.Lerp(1f, 0.25f, die) * (1f + 0.06f * Mathf.Sin(t * 20f) * die * (1f - die));
                        foreach (var f in flagsList) f.SetActive(die < 0.6f);
                        float burst = Mathf.Clamp01((die - 0.8f) / 0.2f);
                        for (int i = 0; i < ves.Count; i++) { ves[i].localPosition = vd[i] * (0.03f + 0.12f * burst * (1f + 0.3f * Mathf.Sin(t + i))); ves[i].localScale = Vector3.one * 0.016f * burst; }
                    };
                    break;
                }
                case "memory":
                {
                    // a few memory cells glow on; a returning virus is caught at once
                    var cells = new List<Transform>();
                    for (int i = 0; i < 5; i++) { var c = Part("Memory cell", whiteMesh, cellWhite, new Vector3((i - 2) * 0.08f, Mathf.Sin(i * 2f) * 0.04f, 0), 0.035f, pivot); cells.Add(c.transform); }
                    var vir = Part("Virus", virusMesh, virusMaterial, new Vector3(0.3f, 0.1f, 0), 0.025f, pivot);
                    var ys = new List<Transform>(); for (int i = 0; i < 6; i++) ys.Add(Part("Antibody", antibodyMesh, antibodyMaterial ? antibodyMaterial : glowMaterial, Vector3.zero, 0.0001f, pivot).transform);
                    animate = t =>
                    {
                        for (int i = 0; i < cells.Count; i++)
                        {
                            var b = new MaterialPropertyBlock(); b.SetVector("_Inst", new Vector4(0.5f, 0.3f + 0.3f * Mathf.Sin(t * 3f + i), 0.2f, i * 0.2f)); b.SetColor("_BaseColor", new Color(0.75f, 1f, 0.8f));
                            cells[i].GetComponent<Renderer>().SetPropertyBlock(b);
                        }
                        float come = Mathf.Clamp01((t - 1f) / 1.2f);
                        vir.transform.localPosition = Vector3.Lerp(new Vector3(0.3f, 0.1f, 0), new Vector3(0.12f, 0.08f, 0), come);
                        float hit = Mathf.Clamp01((t - 2.2f) / 0.5f);
                        for (int i = 0; i < ys.Count; i++)
                        {
                            Vector3 d = Quaternion.Euler(0, 0, i * 60f) * Vector3.up;
                            ys[i].localPosition = vir.transform.localPosition + d * Mathf.Lerp(0.15f, 0.035f, hit);
                            ys[i].localRotation = Quaternion.FromToRotation(Vector3.up, -d); ys[i].localScale = Vector3.one * 0.03f * hit;
                        }
                    };
                    break;
                }
                case "internal":
                {
                    // blood in a capillary → tissue fluid around cells → lymph collected in a lymph vessel
                    var cap = Tube(pivot, new Vector3(0, 0.1f, 0), 9, 0.03f, new Color(1f, 0.4f, 0.4f), null);
                    var lym = Tube(pivot, new Vector3(0, -0.12f, 0), 9, 0.022f, new Color(1f, 0.95f, 0.6f), null);
                    var cells = new List<Transform>();
                    for (int i = 0; i < 4; i++) { var c = Part("Cell", sphereMesh, tissueMaterial, new Vector3(-0.15f + i * 0.1f, -0.01f, 0.02f), 0.035f, pivot); Tint(c, new Color(1f, 0.7f, 0.65f)); cells.Add(c.transform); }
                    var blood = Dots(pivot, 9, new Vector3(0, 0.1f, 0), new Color(1f, 0.25f, 0.3f));
                    var fluid = new List<Transform>(); var fb = new MaterialPropertyBlock(); fb.SetColor("_Color", new Color(0.6f, 0.9f, 1f));
                    for (int i = 0; i < 10; i++) { var d = Part("Fluid", sphereMesh, glowMaterial, Vector3.zero, 0.008f, pivot); d.GetComponent<Renderer>().SetPropertyBlock(fb); fluid.Add(d.transform); }
                    var lymph = Dots(pivot, 7, new Vector3(0, -0.12f, 0), new Color(1f, 0.95f, 0.55f));
                    pivot.localRotation = Quaternion.Euler(10, -25, 0);
                    animate = t =>
                    {
                        for (int i = 0; i < blood.Count; i++) blood[i].localPosition = new Vector3(Mathf.Repeat(i / 9f * 0.44f + t * 0.1f, 0.44f) - 0.22f, 0.1f, 0);
                        for (int i = 0; i < lymph.Count; i++) lymph[i].localPosition = new Vector3(Mathf.Repeat(i / 7f * 0.44f + t * 0.06f, 0.44f) - 0.22f, -0.12f, 0);
                        for (int i = 0; i < fluid.Count; i++)
                        {
                            float u = Mathf.Repeat(t * 0.35f + i / (float)fluid.Count, 1f);
                            float x = -0.18f + i * 0.04f;
                            fluid[i].localPosition = new Vector3(x + Mathf.Sin(u * 6f + i) * 0.01f, Mathf.Lerp(0.08f, -0.1f, u), 0.03f * Mathf.Sin(i * 2f));
                        }
                        foreach (var c in cells) c.localScale = Vector3.one * 0.035f * (1f + 0.05f * Mathf.Sin(t * 2f + c.localPosition.x * 20f));
                    };
                    break;
                }
                case "feedback":
                {
                    // a beam on a pivot: pushed off balance, it swings back to level (negative feedback)
                    var pivotDot = Part("Pivot", sphereMesh, glowMaterial, new Vector3(0, -0.04f, 0), 0.02f, pivot);
                    var beam = Part("Beam", sphereMesh, glowMaterial, Vector3.zero, 1f, pivot); beam.transform.localScale = new Vector3(0.36f, 0.012f, 0.03f);
                    var left = Part("Pressure", sphereMesh, glowMaterial, Vector3.zero, 0.035f, pivot); var right = Part("Pulse", sphereMesh, glowMaterial, Vector3.zero, 0.035f, pivot);
                    var lb = new MaterialPropertyBlock(); lb.SetColor("_Color", new Color(1f, 0.5f, 0.3f)); left.GetComponent<Renderer>().SetPropertyBlock(lb);
                    var rb = new MaterialPropertyBlock(); rb.SetColor("_Color", new Color(1f, 0.3f, 0.4f)); right.GetComponent<Renderer>().SetPropertyBlock(rb);
                    animate = t =>
                    {
                        float tilt = t < 0.8f ? 0f : 22f * Mathf.Exp(-(t - 0.8f) * 0.9f) * Mathf.Cos((t - 0.8f) * 4f);
                        beam.transform.localRotation = Quaternion.Euler(0, 0, tilt);
                        Vector3 axis = beam.transform.localRotation * Vector3.right;
                        left.transform.localPosition = -axis * 0.17f + Vector3.up * 0.03f; right.transform.localPosition = axis * 0.17f + Vector3.up * 0.03f;
                        pivotDot.transform.localScale = Vector3.one * 0.02f;
                    };
                    break;
                }
                case "thermo":
                {
                    // a strip of skin: its vessels widen and sweat drops rise as the heat leaves
                    var skin = Part("Skin", sphereMesh, tissueMaterial, new Vector3(0, -0.02f, 0), 1f, pivot); skin.transform.localScale = new Vector3(0.28f, 0.03f, 0.14f); Tint(skin, new Color(1f, 0.8f, 0.7f));
                    var vessels = Tube(pivot, new Vector3(0, -0.07f, 0), 9, 0.02f, new Color(1f, 0.35f, 0.35f), null);
                    var drops = new List<Transform>(); var db = new MaterialPropertyBlock(); db.SetColor("_Color", new Color(0.6f, 0.85f, 1f));
                    for (int i = 0; i < 6; i++) { var d = Part("Sweat", sphereMesh, glowMaterial, Vector3.zero, 0.0001f, pivot); d.GetComponent<Renderer>().SetPropertyBlock(db); drops.Add(d.transform); }
                    pivot.localRotation = Quaternion.Euler(25, -20, 0);
                    animate = t =>
                    {
                        float k = Mathf.Clamp01((t - 0.5f) / 1.5f);
                        foreach (var v in vessels) v.localScale = Vector3.one * 0.02f * (1f + 0.6f * k);
                        for (int i = 0; i < drops.Count; i++)
                        {
                            float u = Mathf.Repeat(t * 0.5f + i / (float)drops.Count, 1f);
                            drops[i].localPosition = new Vector3(-0.12f + i * 0.05f, 0.01f + u * 0.12f * k, Mathf.Sin(i * 2f) * 0.04f);
                            drops[i].localScale = Vector3.one * 0.012f * k * (1f - u);
                        }
                    };
                    break;
                }
                case "glucose":
                {
                    // glucose dots around a cell; an insulin "key" opens the door and they go in
                    var cell = Part("Cell", sphereMesh, tissueMaterial, Vector3.zero, 0.09f, pivot); Tint(cell, new Color(1f, 0.7f, 0.6f));
                    var key = Part("Insulin", antibodyMesh, glowMaterial, new Vector3(0.2f, 0.12f, 0), 0.05f, pivot);
                    var kb = new MaterialPropertyBlock(); kb.SetColor("_Color", new Color(1f, 0.85f, 0.4f)); key.GetComponent<Renderer>().SetPropertyBlock(kb);
                    var dots = new List<Transform>(); var gb = new MaterialPropertyBlock(); gb.SetColor("_Color", new Color(1f, 0.95f, 0.6f));
                    for (int i = 0; i < 10; i++) { var d = Part("Glucose", sphereMesh, glowMaterial, Vector3.zero, 0.012f, pivot); d.GetComponent<Renderer>().SetPropertyBlock(gb); dots.Add(d.transform); }
                    animate = t =>
                    {
                        float dock = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 0.5f) / 1f));
                        key.transform.localPosition = Vector3.Lerp(new Vector3(0.2f, 0.12f, 0), new Vector3(0.085f, 0.04f, 0), dock);
                        float inK = Mathf.Clamp01((t - 1.6f) / 1.8f);
                        for (int i = 0; i < dots.Count; i++)
                        {
                            float a = i / (float)dots.Count * Mathf.PI * 2f + t * 0.6f;
                            Vector3 outP = new Vector3(Mathf.Cos(a) * 0.2f, Mathf.Sin(a * 1.3f) * 0.08f, Mathf.Sin(a) * 0.2f);
                            dots[i].localPosition = Vector3.Lerp(outP, outP * 0.25f, Mathf.Clamp01(inK * 1.5f - i * 0.05f));
                        }
                    };
                    break;
                }
                case "homeostasis":
                {
                    // six little gauges around a heart; their needles wobble and always come back to the middle
                    var heart = Part("Heart", sphereMesh, glowMaterial, Vector3.zero, 0.045f, pivot);
                    var hb = new MaterialPropertyBlock(); hb.SetColor("_Color", new Color(1f, 0.3f, 0.35f)); heart.GetComponent<Renderer>().SetPropertyBlock(hb);
                    var needles = new List<Transform>();
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 60f * Mathf.Deg2Rad; Vector3 c = new Vector3(Mathf.Cos(a) * 0.17f, Mathf.Sin(a) * 0.17f, 0);
                        var dial = Part("Dial", ringMesh, glowMaterial, c, 0.04f, pivot);
                        var nb = new MaterialPropertyBlock(); nb.SetColor("_Color", Bvr2Style.Good); dial.GetComponent<Renderer>().SetPropertyBlock(nb);
                        var n = Part("Needle", sphereMesh, glowMaterial, c, 1f, pivot); n.transform.localScale = new Vector3(0.004f, 0.03f, 0.004f); needles.Add(n.transform);
                    }
                    animate = t =>
                    {
                        heart.transform.localScale = Vector3.one * 0.045f * (1f + 0.3f * Bvr2Math.PulseShape(t * 1.2f));
                        for (int i = 0; i < needles.Count; i++)
                        {
                            float ang = 35f * Mathf.Sin(t * (1.3f + i * 0.2f) + i) * Mathf.Exp(-Mathf.Repeat(t + i * 0.4f, 2.5f));
                            needles[i].localRotation = Quaternion.Euler(0, 0, ang);
                        }
                        pivot.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 0.4f) * 15f, 0);
                    };
                    break;
                }
                default:
                {
                    var ring = Part("Pulse", ringMesh, glowMaterial, Vector3.zero, 0.15f, pivot);
                    animate = t => ring.transform.localScale = Vector3.one * (0.15f + 0.03f * Bvr2Math.PulseShape(t * 1.2f));
                    break;
                }
            }
        }

        /// <summary>A short vessel drawn as glowing rings along local X (ring axis = X).</summary>
        List<Transform> Tube(Transform parent, Vector3 offset, int count, float radius, Color color, Func<int, float> profile)
        {
            var list = new List<Transform>(); var b = new MaterialPropertyBlock(); b.SetColor("_Color", color);
            var mesh = torusMesh ? torusMesh : ringMesh;
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(-0.2f, 0.2f, i / (float)(count - 1));
                float k = profile != null ? profile(i) : 1f;
                var r = Part("Ring", mesh, glowMaterial, offset + new Vector3(x, 0, 0), radius * k, parent);
                r.transform.localRotation = Quaternion.Euler(0, 90, 0); r.GetComponent<Renderer>().SetPropertyBlock(b);
                list.Add(r.transform);
            }
            return list;
        }

        List<Transform> Dots(Transform parent, int count, Vector3 offset, Color color)
        {
            var list = new List<Transform>(); var b = new MaterialPropertyBlock(); b.SetColor("_Color", color);
            for (int i = 0; i < count; i++)
            {
                var d = Part("Blood", sphereMesh, glowMaterial, offset, 0.011f, parent); d.GetComponent<Renderer>().SetPropertyBlock(b);
                d.transform.localPosition = offset + new Vector3(Mathf.Lerp(-0.2f, 0.2f, i / (float)count), 0, 0); list.Add(d.transform);
            }
            return list;
        }

        static void MoveDots(List<Transform> dots, float t, float speed, float spread)
        {
            for (int i = 0; i < dots.Count; i++)
            {
                var p = dots[i].localPosition; float x = Mathf.Repeat(i / (float)dots.Count * 0.44f + t * speed, 0.44f) - 0.22f;
                dots[i].localPosition = new Vector3(x, p.y > 0 ? 0.1f + Mathf.Sin(i * 2.3f) * spread : -0.1f + Mathf.Sin(i * 2.3f) * spread, Mathf.Cos(i * 1.7f) * spread);
            }
        }

        // a soft highlight so the hologram reads against the dark lumen (cell shader: _Inst.y)
        static void Glow(GameObject go, float amount) { var b = new MaterialPropertyBlock(); b.SetVector("_Inst", new Vector4(0.5f, amount, 0, 0)); go.GetComponent<Renderer>().SetPropertyBlock(b); }

        static void Tint(GameObject go, Color c) { var b = new MaterialPropertyBlock(); b.SetColor("_BaseColor", c); go.GetComponent<Renderer>().SetPropertyBlock(b); }

        List<CanvasGroup> BuildCallouts(Bvr2Codex.Entry e, bool centred = false)
        {
            var list = new List<CanvasGroup>();
            var canvas = Bvr2Ui.Canvas("Callouts", root, new Vector2(760, 520), 0.00074f, 90);
            canvas.localPosition = new Vector3(centred ? 0.07f : 0.23f, 0.02f, 0);
            var titleRect = Bvr2Ui.Rect(canvas, "Title", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -80), new Vector2(0, -6));
            var title = Bvr2Ui.Label(titleRect, e.title.ToUpperInvariant(), 58, e.color, TextAnchor.MiddleLeft, FontStyle.Bold);
            Bvr2Ui.Fit(title, 28, 58); // the rect is one line tall, so it shrinks instead of wrapping
            title.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            var tag = Bvr2Ui.Label(Bvr2Ui.Rect(canvas, "Tag", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -136), new Vector2(0, -92)), "+ ЗНАНИЕ В СПРАВОЧНИКЕ", 26, Bvr2Style.Knowledge, TextAnchor.MiddleLeft, FontStyle.Bold);
            for (int i = 0; i < e.callouts.Length; i++)
            {
                var row = Bvr2Ui.Rect(canvas, "Callout " + i, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -230 - i * 104), new Vector2(0, -150 - i * 104));
                var g = row.gameObject.AddComponent<CanvasGroup>(); g.alpha = 0;
                Bvr2Ui.Panel(row, Bvr2Style.Panel, "Back");
                var bar = Bvr2Ui.Panel(row, e.color, "Bar"); bar.rectTransform.anchorMax = new Vector2(0, 1); bar.rectTransform.offsetMax = new Vector2(8, 0);
                var line = Bvr2Ui.Panel(row, new Color(e.color.r, e.color.g, e.color.b, 0.6f), "Leader"); var lr = line.rectTransform; lr.anchorMin = lr.anchorMax = new Vector2(0, 0.5f); lr.sizeDelta = new Vector2(70, 3); lr.anchoredPosition = new Vector2(-36, 0);
                var t = Bvr2Ui.Label(row, e.callouts[i], 40, Bvr2Style.Text, TextAnchor.MiddleLeft, FontStyle.Bold); t.rectTransform.offsetMin = new Vector2(26, 0); t.rectTransform.offsetMax = new Vector2(-14, 0);
                Bvr2Ui.Fit(t, 30, 40);
                list.Add(g);
            }
            return list;
        }
    }
}
