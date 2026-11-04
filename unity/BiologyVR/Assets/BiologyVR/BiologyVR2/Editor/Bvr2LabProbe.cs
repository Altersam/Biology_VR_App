using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BiologyVR.V2.Editor
{
    /// <summary>
    /// Tests / layout: raycasts inside the SecVarLab prefab (temporary colliders, far below the world) and
    /// writes ClaudeBridge/shots/lab_probe.txt — distances to the walls from given lab-local points, so props
    /// can be placed against free wall stretches. Lab-local = prefab coordinates − Bvr2SceneBuilder.SecVarMainRoom.
    /// </summary>
    public static class Bvr2LabProbe
    {
        public static string Probe(string pointsCsv, float height = 1.2f, int dirs = 72)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Bvr2LabImport.PrefabPath);
            if (!asset) return "no prefab";
            var origin = new Vector3(0, -3000, 0);
            var inst = (GameObject)Object.Instantiate(asset, origin, Quaternion.identity); inst.hideFlags = HideFlags.HideAndDontSave;
            foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true)) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; }
            Physics.SyncTransforms();
            var main = new Vector3(-0.6f, 0, -0.5f); // SecVarMainRoom
            var sb = new StringBuilder();
            var pts = pointsCsv.Split(';');
            foreach (var p in pts)
            {
                var f = p.Split(','); if (f.Length < 2) continue;
                float x = float.Parse(f[0], System.Globalization.CultureInfo.InvariantCulture), z = float.Parse(f[1], System.Globalization.CultureInfo.InvariantCulture);
                foreach (float h in new[] { 0.4f, height, 2.1f })
                {
                    sb.Append($"P {x:F2},{z:F2} h{h:F1}:");
                    for (int i = 0; i < dirs; i++)
                    {
                        float a = i * 360f / dirs; var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                        var o = origin + main + new Vector3(x, h, z);
                        float dist = Physics.Raycast(o, d, out var hit, 12f) ? hit.distance : 12f;
                        sb.Append($" {a:F0}={dist:F2}");
                    }
                    sb.AppendLine();
                }
                var up = origin + main + new Vector3(x, 1.2f, z);
                float ceil = Physics.Raycast(up, Vector3.up, out var hc, 10) ? hc.distance + 1.2f : -1;
                float floor = Physics.Raycast(up, Vector3.down, out var hf, 10) ? 1.2f - hf.distance : -1;
                sb.AppendLine($"P {x:F2},{z:F2} floor {floor:F2} ceiling {ceil:F2}");
            }
            Object.DestroyImmediate(inst);
            string dir = Path.GetFullPath("ClaudeBridge/shots"); Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "lab_probe.txt"), sb.ToString());
            return pts.Length + " points";
        }
            /// <summary>All hits (both faces) along a lab-local ray in the prefab: "name@distance" list.</summary>
        public static string Ray(float x, float y, float z, float dx, float dy, float dz, float max = 6f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Bvr2LabImport.PrefabPath);
            if (!asset) return "no prefab";
            var origin = new Vector3(0, -3000, 0);
            var inst = (GameObject)Object.Instantiate(asset, origin, Quaternion.identity); inst.hideFlags = HideFlags.HideAndDontSave;
            foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true)) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; }
            Physics.SyncTransforms();
            bool back = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
            var hits = Physics.RaycastAll(origin + new Vector3(-0.6f, 0, -0.5f) + new Vector3(x, y, z), new Vector3(dx, dy, dz).normalized, max);
            Physics.queriesHitBackfaces = back;
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            var sb = new StringBuilder();
            foreach (var h in hits) sb.Append($"{h.collider.name}@{h.distance:F2}(n {h.normal.x:F1},{h.normal.y:F1},{h.normal.z:F1}) ");
            Object.DestroyImmediate(inst);
            return sb.ToString();
        }

        /// <summary>
        /// Turntable of the professor in the open scene: 8 views around him at the player's eye height
        /// (front, 45, side, ... back), optional head yaw/pitch about the neck pivot - checks the neck / collar /
        /// strap / hair layers from every side; solid = 1 is the lab look (Bvr2Professor on stage), 0 the hologram.
        /// Writes a 4x2 sheet to ClaudeBridge/shots/{file}; the result lists other renderers in view.
        /// </summary>
        public static string ProfShots(string file, float headYaw = 0, float headPitch = 0, float dist = 1.25f, float aimY = 1.5f, float solid = 0)
        {
            Bvr2Professor prof = null;
            foreach (var p in Resources.FindObjectsOfTypeAll<Bvr2Professor>())
                if (p.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene()) { prof = p; break; }
            if (!prof || !prof.model) return "no professor";
            // activate the professor and its parents for the shot
            var toggled = new List<GameObject>();
            for (var t = prof.transform; t; t = t.parent) if (!t.gameObject.activeSelf) { t.gameObject.SetActive(true); toggled.Add(t.gameObject); }
            var posWas = prof.transform.position; prof.transform.position = new Vector3(0, -5000, 0);   // away from the lab props
            Quaternion pivotWas = prof.headPivot ? prof.headPivot.localRotation : Quaternion.identity;
            if (prof.headPivot) prof.headPivot.localRotation = pivotWas * Quaternion.Euler(headPitch, headYaw, 0);
            var rs = prof.model.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var mpb = new MaterialPropertyBlock();
            foreach (var r in rs)
            {
                r.GetPropertyBlock(mpb); mpb.SetFloat("_Reveal", 1f); mpb.SetFloat("_Solid", solid); mpb.SetVector("_RevealRange", new Vector4(b.min.y - 1f, b.max.y + 1f, 0, 0)); r.SetPropertyBlock(mpb);
            }
            float s = prof.transform.lossyScale.y;
            var others = new StringBuilder();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (r.enabled && r.bounds.Intersects(b) && System.Array.IndexOf(rs, r) < 0) others.Append(r.name + "(" + r.GetType().Name + ") ");
            var basePos = new Vector3(b.center.x, b.min.y, b.center.z);
            var fwd = prof.transform.forward; fwd.y = 0; fwd.Normalize();
            var cgo = new GameObject("ProfShotCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = cgo.AddComponent<Camera>();
            cam.fieldOfView = 34; cam.nearClipPlane = 0.03f; cam.farClipPlane = 50; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.42f, 0.45f, 0.5f);
            const int W = 420, H = 520;
            var sheet = new Texture2D(W * 4, H * 2, TextureFormat.RGB24, false);
            var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32); rt.antiAliasing = 4;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            for (int i = 0; i < 8; i++)
            {
                var dir = Quaternion.AngleAxis(i * 45f, Vector3.up) * fwd;
                var target = basePos + Vector3.up * aimY * s;
                cam.transform.position = target + dir * dist * s + Vector3.up * 0.15f * s;
                cam.transform.LookAt(target);
                cam.targetTexture = rt; cam.Render();
                var prev = RenderTexture.active; RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = prev;
                sheet.SetPixels((i % 4) * W, (1 - i / 4) * H, W, H, tex.GetPixels());
            }
            sheet.Apply(); cam.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
            string dirOut = Path.GetFullPath("ClaudeBridge/shots"); Directory.CreateDirectory(dirOut);
            File.WriteAllBytes(Path.Combine(dirOut, file), sheet.EncodeToJPG(88));
            Object.DestroyImmediate(tex); Object.DestroyImmediate(sheet); Object.DestroyImmediate(cgo);
            foreach (var r in rs) r.SetPropertyBlock(null);
            if (prof.headPivot) prof.headPivot.localRotation = pivotWas;
            prof.transform.position = posWas;
            for (int i = toggled.Count - 1; i >= 0; i--) toggled[i].SetActive(false);
            return file + (others.Length > 0 ? " | also in view: " + others : "");
        }

        /// <summary>
        /// Close-ups of the hand tools (bio-scanner, BioTool) away from the scene: row 1 the scanner, row 2 the BioTool,
        /// four views each (back three-quarter as held, side, top with the screen, front). Sheet in ClaudeBridge/shots.
        /// </summary>
        public static string ToolShots(string file, float dist = 0.32f, float spin = 0f)
        {
            var roots = new List<Transform>();
            foreach (var sc in Resources.FindObjectsOfTypeAll<Bvr2Scanner>()) if (sc.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene()) { roots.Add(sc.transform); break; }
            foreach (var bt in Resources.FindObjectsOfTypeAll<Bvr2BioTool>()) if (bt.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene()) { roots.Add(bt.transform); break; }
            if (roots.Count == 0) return "no tools";
            var cgo = new GameObject("ToolShotCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = cgo.AddComponent<Camera>();
            cam.fieldOfView = 30; cam.nearClipPlane = 0.01f; cam.farClipPlane = 20; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.16f, 0.18f, 0.22f);
            const int W = 420, H = 360;
            var sheet = new Texture2D(W * 4, H * 2, TextureFormat.RGB24, false);
            var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32); rt.antiAliasing = 4;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            var info = new StringBuilder();
            for (int row = 0; row < roots.Count && row < 2; row++)
            {
                var t = roots[row];
                var toggled = new List<GameObject>();
                for (var p = t; p; p = p.parent) if (!p.gameObject.activeSelf) { p.gameObject.SetActive(true); toggled.Add(p.gameObject); }
                var parentWas = t.parent; var posWas = t.localPosition; var rotWas = t.localRotation; var sclWas = t.localScale;
                t.SetParent(null, false); t.SetPositionAndRotation(new Vector3(0, -5000, 0), Quaternion.Euler(0, spin, 0)); t.localScale = Vector3.one;
                var rs = t.GetComponentsInChildren<Renderer>();
                var b = rs.Length > 0 ? rs[0].bounds : new Bounds(t.position, Vector3.one * 0.1f); foreach (var r in rs) if (!(r is ParticleSystemRenderer)) b.Encapsulate(r.bounds);
                info.Append($"{t.name}: {rs.Length} renderers, size {b.size.x:F3}x{b.size.y:F3}x{b.size.z:F3}; ");
                var target = b.center;
                Vector3[] dirs = { new Vector3(-0.35f, 0.45f, -1f), new Vector3(-1f, 0.15f, 0.05f), new Vector3(0.1f, 1f, -0.35f), new Vector3(0.45f, 0.25f, 1f) };
                for (int i = 0; i < 4; i++)
                {
                    var d = (t.rotation * dirs[i]).normalized;
                    cam.transform.position = target + d * dist; cam.transform.LookAt(target, t.up);
                    cam.targetTexture = rt; cam.Render();
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = prev;
                    sheet.SetPixels(i * W, (1 - row) * H, W, H, tex.GetPixels());
                }
                t.SetParent(parentWas, false); t.localPosition = posWas; t.localRotation = rotWas; t.localScale = sclWas;
                for (int i = toggled.Count - 1; i >= 0; i--) toggled[i].SetActive(false);
            }
            sheet.Apply(); cam.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
            string dirOut = Path.GetFullPath("ClaudeBridge/shots"); Directory.CreateDirectory(dirOut);
            File.WriteAllBytes(Path.Combine(dirOut, file), sheet.EncodeToJPG(90));
            Object.DestroyImmediate(tex); Object.DestroyImmediate(sheet); Object.DestroyImmediate(cgo);
            return file + " | " + info;
        }

        /// <summary>Lab-local positions / forward / renderer bounds of the scene objects whose names contain any of the given parts ("a|b").</summary>
        public static string Where(string names)
        {
            Transform lab = null;
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var c in go.GetComponentsInChildren<Transform>(true)) if (c.name == "Lab" && c.Find("Dressing")) { lab = c; break; }
            var sb = new StringBuilder();
            var parts = names.Split('|');
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                {
                    bool hit = false; foreach (var p in parts) if (t.name.Contains(p)) hit = true;
                    if (!hit) continue;
                    Vector3 lp = lab ? lab.InverseTransformPoint(t.position) : t.position, lf = lab ? lab.InverseTransformDirection(t.forward) : t.forward;
                    sb.Append($"{t.name} @ {lp:F2} fwd {lf:F2}");
                    var r = t.GetComponent<Renderer>(); if (r) sb.Append($" bounds {r.bounds.center:F2}/{r.bounds.size:F2} q{r.sharedMaterial?.renderQueue}");
                    var cv = t.GetComponent<Canvas>(); if (cv) sb.Append($" canvas order {cv.sortingOrder}");
                    sb.Append(" ; ");
                }
            return sb.ToString();
        }

        /// <summary>Renders the built lab in the open scene (activates it for the shot) from a lab-local pose.</summary>
        public static string Shot(string file, float x, float y, float z, float yaw, float pitch, float fov = 80, bool hideRoom = false)
        {
            GameObject lab = null;
            foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var t = go.transform.Find("Lab"); if (go.name == "Lab") t = go.transform;
                if (!t) foreach (var c in go.GetComponentsInChildren<Transform>(true)) if (c.name == "Lab" && c.Find("Dressing")) { t = c; break; }
                if (t) { lab = t.gameObject; break; }
            }
            if (!lab) return "no Lab";
            bool was = lab.activeSelf; lab.SetActive(true);
            foreach (var l in lab.GetComponentsInChildren<Bvr2LabLighting>(true)) l.Push();
            var hidden = new System.Collections.Generic.List<Renderer>();
            if (hideRoom) foreach (var r in lab.GetComponentsInChildren<Renderer>()) if (r.transform.parent && r.transform.parent.name == "Combined" && r.enabled) { r.enabled = false; hidden.Add(r); }
            var cgo = new GameObject("LabShotCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = cgo.AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(lab.transform.TransformPoint(new Vector3(x, y, z)), lab.transform.rotation * Quaternion.Euler(pitch, yaw, 0));
            cam.fieldOfView = fov; cam.nearClipPlane = 0.03f; cam.farClipPlane = 200; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
            var rt = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32); rt.antiAliasing = 4;
            cam.targetTexture = rt; cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            RenderTexture.active = prev; cam.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
            string dir = Path.GetFullPath("ClaudeBridge/shots"); Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, file), tex.EncodeToJPG(88));
            Object.DestroyImmediate(tex); Object.DestroyImmediate(cgo);
            foreach (var r in hidden) r.enabled = true;
            lab.SetActive(was);
            return file;
        }
    }
}
