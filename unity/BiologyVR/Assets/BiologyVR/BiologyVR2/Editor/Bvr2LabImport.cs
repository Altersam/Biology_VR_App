using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BiologyVR.V2.Editor
{
    /// <summary>
    /// Turns the SecVarLab room (Assets/ThirdParty/SecVarLab, extracted from SecVarLab_Full.unitypackage
    /// by Tools/extract_secvar_lab.py) into a game prefab for the prologue: copies the lab and the capsule
    /// out of the source scene, centres the room on its floor, strips lights / cameras / colliders /
    /// animators, converts the toon materials (their shader is not in the package) into BVR2/LabToon
    /// with ramps baked from the original gradients, and stores markers (player start, capsule, lamps).
    /// The third-party files themselves are never modified.
    /// </summary>
    public static class Bvr2LabImport
    {
        public const string LabScene = "Assets/ThirdParty/SecVarLab/Scenes/SecVarLab.unity";
        public const string OutDir = Bvr2SceneBuilder.Root + "/Generated/Lab";
        public const string PrefabPath = OutDir + "/SecVarLab.prefab";

        public static bool Available => File.Exists(LabScene);

        [MenuItem("Biology VR 2/Lab/Rebuild SecVarLab Prefab", false, 300)]
        static void Menu() => Debug.Log("[BVR2] " + BuildPrefab());

        public static string BuildPrefab()
        {
            if (!Available) return "SecVarLab scene missing — run Tools/extract_secvar_lab.py";
            var active = SceneManager.GetActiveScene();
            EditorSceneManager.SaveOpenScenes();
            var lab = EditorSceneManager.OpenScene(LabScene, OpenSceneMode.Additive);
            try
            {
                var roots = lab.GetRootGameObjects();
                GameObject Find(string n) => roots.FirstOrDefault(r => r.name == n);
                var labRoot = Find("---Lab---"); var capsule = Find("BigCapsul"); var lights = Find("---Lights---");
                var rig = roots.FirstOrDefault(r => r.name.StartsWith("XR Origin"));
                if (!labRoot) return "---Lab--- not found in " + LabScene;

                var root = new GameObject("SecVarLab"); SceneManager.MoveGameObjectToScene(root, lab);
                var room = Object.Instantiate(labRoot, root.transform); room.name = "Room";
                GameObject caps = capsule ? Object.Instantiate(capsule, root.transform) : null; if (caps) caps.name = "Capsule";

                // centre on the room floor
                var b = WorldBounds(room);
                float floor = FloorHeight(room, b);
                Vector3 shift = new Vector3(-b.center.x, -floor, -b.center.z);
                foreach (Transform c in root.transform) c.position += shift;

                var markers = new GameObject("Markers").transform; markers.SetParent(root.transform, false);
                Marker(markers, "PlayerStart", rig ? rig.transform.position + shift : Vector3.zero, rig ? rig.transform.eulerAngles.y : 0);
                if (caps) Marker(markers, "CapsuleSpot", caps.transform.position, caps.transform.eulerAngles.y);
                var lighting = root.AddComponent<Bvr2LabLighting>();
                var lampList = new List<Transform>();
                if (lights)
                    foreach (var l in lights.GetComponentsInChildren<Light>(true))
                    {
                        if (l.type == LightType.Directional) { lighting.sunTravel = l.transform.forward; lighting.sunColor = Color.Lerp(Color.white, l.color, 0.45f); }
                        else if (lampList.Count < 4) lampList.Add(Marker(markers, "Lamp" + lampList.Count, l.transform.position + shift, 0));
                    }
                lighting.lamps = lampList.ToArray();

                int stripped = Strip(root);
                int culled = Cull(root);
                var used = new HashSet<string>(); var cache = new Dictionary<Material, Material>();
                Directory.CreateDirectory(OutDir + "/Materials"); Directory.CreateDirectory(OutDir + "/Ramps");
                int tris = 0, renderers = 0;
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    renderers++;
                    r.sharedMaterials = r.sharedMaterials.Select(m => Convert(m, cache, used)).ToArray();
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                    r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                    var mf = r.GetComponent<MeshFilter>(); if (mf && mf.sharedMesh) for (int sm = 0; sm < mf.sharedMesh.subMeshCount; sm++) tris += (int)(mf.sharedMesh.GetIndexCount(sm) / 3);
                }
                int batches = Combine(root);
                Directory.CreateDirectory(OutDir);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
                var size = b.size;
                return $"SecVarLab prefab: room {size.x:F1}×{size.z:F1}×{size.y:F1} m, floor {floor:F2}, {renderers} renderers → {batches} meshes, {tris} tris, {cache.Count} materials, stripped {stripped} components, culled {culled} detail renderers, lamps {lampList.Count}";
            }
            finally
            {
                EditorSceneManager.CloseScene(lab, true);
                if (active.IsValid()) SceneManager.SetActiveScene(active);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// One mesh per material (32-bit indices, positions + normals only — the toon materials use no textures):
        /// ~50 draw calls instead of ~1000, and unlike static batching it still moves and scales with the lab
        /// during the shrinking sequence. The source objects are removed; markers stay.
        /// </summary>
        static int Combine(GameObject root)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            var toWorldLocal = root.transform.worldToLocalMatrix;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh || !r.gameObject.activeInHierarchy) continue;
                var mats = r.sharedMaterials;
                for (int sm = 0; sm < mf.sharedMesh.subMeshCount; sm++)
                {
                    var m = mats[Mathf.Min(sm, mats.Length - 1)]; if (!m) continue;
                    if (!groups.TryGetValue(m, out var list)) groups[m] = list = new List<CombineInstance>();
                    list.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = sm, transform = toWorldLocal * r.transform.localToWorldMatrix });
                }
            }
            string dir = OutDir + "/Meshes"; Directory.CreateDirectory(dir);
            var holder = new GameObject("Combined").transform; holder.SetParent(root.transform, false);
            foreach (var g in groups.OrderBy(x => x.Key.renderQueue).ThenBy(x => x.Key.name))
            {
                var mesh = new Mesh { name = "Lab_" + g.Key.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(g.Value.ToArray(), true, true, false);
                mesh.uv = null; mesh.uv2 = null; mesh.tangents = null; mesh.colors = null;
                mesh.RecalculateBounds(); mesh.Optimize(); mesh.UploadMeshData(false);
                string path = $"{dir}/{mesh.name}.asset";
                var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (old) { EditorUtility.CopySerialized(mesh, old); Object.DestroyImmediate(mesh); mesh = old; } else AssetDatabase.CreateAsset(mesh, path);
                var go = new GameObject(g.Key.name); go.transform.SetParent(holder, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = g.Key;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }
            foreach (Transform c in root.transform.Cast<Transform>().ToArray())
                if (c != holder && c.name != "Markers") Object.DestroyImmediate(c.gameObject);
            return groups.Count;
        }

        static Transform Marker(Transform parent, string name, Vector3 pos, float yaw)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.position = pos; t.rotation = Quaternion.Euler(0, yaw, 0); return t;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        /// <summary>Floor = the lowest large horizontal surface: lowest renderer bounds that cover a big area.</summary>
        static float FloorHeight(GameObject room, Bounds all)
        {
            float best = all.min.y; float area = all.size.x * all.size.z;
            foreach (var r in room.GetComponentsInChildren<Renderer>(true))
            {
                var b = r.bounds;
                if (b.size.x * b.size.z > area * 0.25f && b.size.y < 0.6f) return b.max.y < all.min.y + 1f ? b.max.y : best;
            }
            return best;
        }

        /// <summary>Headset budget: drop the furnished bathroom behind the bay (never visible from the lab),
        /// a heavy translucent tarp and sub-3 cm details (screws, tiny knobs) — together ≈ a third of the triangles.</summary>
        static int Cull(GameObject root)
        {
            int n = 0;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true).ToArray())
            {
                var b = r.bounds; float big = Mathf.Max(b.size.x, b.size.y, b.size.z);
                bool bath = b.center.x < -3.6f && b.center.z < -0.3f && big < 1.5f;
                bool tiny = big < 0.03f;
                bool tarp = r.sharedMaterials.Any(m => m && m.name.Contains("Tarp"));
                if (!(bath || tiny || tarp)) continue;
                var mf = r.GetComponent<MeshFilter>(); Object.DestroyImmediate(r); if (mf) Object.DestroyImmediate(mf); n++;
            }
            return n;
        }

        static int Strip(GameObject root)
        {
            int n = 0;
            foreach (var c in root.GetComponentsInChildren<Component>(true).ToArray())
            {
                if (!c || c is Transform || c is MeshFilter || c is Renderer || c is Bvr2LabLighting || c is LODGroup) continue;
                Object.DestroyImmediate(c); n++;
            }
            return n;
        }

        // ---------------------------------------------------------------- materials
        static Material Convert(Material src, Dictionary<Material, Material> cache, HashSet<string> used)
        {
            if (!src) return null;
            if (cache.TryGetValue(src, out var done)) return done;
            string path = AssetDatabase.GetAssetPath(src);
            string yaml = path.EndsWith(".mat") ? File.ReadAllText(path) : null;
            Color baseCol = YamlColor(yaml, "_BaseColor") ?? (src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src.HasProperty("_Color") ? src.color : Color.white);
            bool emissive = yaml != null ? Regex.IsMatch(yaml, @"m_ValidKeywords:[^\n]*\n(\s+- [^\n]*\n)*?\s+- _EMISSION") : src.IsKeywordEnabled("_EMISSION");
            Color emission = emissive ? (YamlColor(yaml, "_EmissionColor") ?? (src.HasProperty("_EmissionColor") ? src.GetColor("_EmissionColor") : Color.black)) : Color.black;
            bool transparent = (YamlFloat(yaml, "_Surface") ?? (src.HasProperty("_Surface") ? src.GetFloat("_Surface") : 0)) > 0.5f;
            float smooth = YamlFloat(yaml, "_Smoothness") ?? 0.5f;
            var ramp = YamlRamp(yaml, out bool step) ?? new List<(float t, Color c)> { (0f, new Color(0.46f, 0.5f, 0.56f)), (0.62f, new Color(0.86f, 0.88f, 0.9f)), (1f, Color.white) };

            string name = Regex.Replace(src.name, @"[^A-Za-z0-9_\- ]", "_").Trim(); if (name.Length == 0) name = "Mat";
            string unique = name; int k = 2; while (!used.Add(unique)) unique = name + "_" + k++;
            var tex = BakeRamp(ramp, step, $"{OutDir}/Ramps/{unique}.png");
            string matPath = $"{OutDir}/Materials/{unique}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (!mat) { mat = new Material(Shader.Find("BVR2/LabToon")); AssetDatabase.CreateAsset(mat, matPath); }
            mat.shader = Shader.Find("BVR2/LabToon");
            mat.SetColor("_BaseColor", baseCol); mat.SetTexture("_Ramp", tex); mat.SetColor("_EmissionColor", emission * 0.6f);
            mat.SetFloat("_Gloss", smooth > 0.85f ? 0.55f : 0.12f);
            if (transparent) { mat.SetFloat("_SrcBlend", 5); mat.SetFloat("_DstBlend", 10); mat.SetFloat("_ZWrite", 0); mat.renderQueue = 3000; }
            else { mat.SetFloat("_SrcBlend", 1); mat.SetFloat("_DstBlend", 0); mat.SetFloat("_ZWrite", 1); mat.renderQueue = -1; baseCol.a = 1; mat.SetColor("_BaseColor", baseCol); }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            cache[src] = mat; return mat;
        }

        static Color? YamlColor(string yaml, string prop)
        {
            if (yaml == null) return null;
            var m = Regex.Match(yaml, "- " + prop + @": \{r: ([-\d.eE]+), g: ([-\d.eE]+), b: ([-\d.eE]+), a: ([-\d.eE]+)\}");
            if (!m.Success) return null;
            float F(int i) => float.Parse(m.Groups[i].Value, System.Globalization.CultureInfo.InvariantCulture);
            return new Color(F(1), F(2), F(3), F(4));
        }

        static float? YamlFloat(string yaml, string prop)
        {
            if (yaml == null) return null;
            var m = Regex.Match(yaml, "- " + prop + @": ([-\d.eE]+)\s*\n");
            return m.Success ? float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : (float?)null;
        }

        /// <summary>Toon gradients are stored as an embedded texture named z__GradientRampTex{json}.</summary>
        static List<(float t, Color c)> YamlRamp(string yaml, out bool step)
        {
            step = false;
            if (yaml == null) return null;
            var m = Regex.Match(yaml, @"m_Name: z__GradientRampTex(\{.*\})");
            if (!m.Success) return null;
            var j = JObject.Parse(m.Groups[1].Value);
            step = (int?)j["mode"] == 1;
            var list = new List<(float, Color)>();
            foreach (var key in j["colorKeys"])
            {
                var c = key["color"];
                list.Add(((float)key["time"], new Color((float)c["r"], (float)c["g"], (float)c["b"], 1)));
            }
            return list.Count > 0 ? list.OrderBy(x => x.Item1).ToList() : null;
        }

        static Texture2D BakeRamp(List<(float t, Color c)> keys, bool step, string path)
        {
            const int W = 64;
            var tex = new Texture2D(W, 1, TextureFormat.RGBA32, false, false);
            for (int x = 0; x < W; x++)
            {
                float t = x / (W - 1f); Color c = keys[0].c;
                if (t >= keys[keys.Count - 1].t) c = keys[keys.Count - 1].c;
                else for (int i = 1; i < keys.Count; i++)
                        if (t <= keys[i].t)
                        {
                            if (t <= keys[i - 1].t) { c = keys[i - 1].c; break; }
                            c = step ? keys[i].c : Color.Lerp(keys[i - 1].c, keys[i].c, Mathf.InverseLerp(keys[i - 1].t, keys[i].t, t)); break;
                        }
                tex.SetPixel(x, 0, c);
            }
            File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp.wrapMode != TextureWrapMode.Clamp || imp.mipmapEnabled || imp.textureCompression != TextureImporterCompression.Uncompressed)
            {
                imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = false; imp.filterMode = FilterMode.Bilinear;
                imp.textureCompression = TextureImporterCompression.Uncompressed; imp.npotScale = TextureImporterNPOTScale.None; imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------------------------------------------------------------- previews (bridge / tests)
        /// <summary>Renders the lab prefab alone (far below the world) from a pose relative to its floor centre.</summary>
        public static string Preview(string file, float x, float y, float z, float yaw, float pitch, float fov = 70, float ortho = 0)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab) return "no prefab";
            var origin = new Vector3(0, -3000, 0);
            var inst = (GameObject)Object.Instantiate(prefab, origin, Quaternion.identity); inst.hideFlags = HideFlags.HideAndDontSave;
            inst.GetComponent<Bvr2LabLighting>()?.Push();
            var go = new GameObject("LabPreviewCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = go.AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(origin + new Vector3(x, y, z), Quaternion.Euler(pitch, yaw, 0));
            cam.fieldOfView = fov; cam.nearClipPlane = 0.05f; cam.farClipPlane = 400; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
            if (ortho > 0) { cam.orthographic = true; cam.orthographicSize = ortho; }
            var rt = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32); rt.antiAliasing = 4;
            cam.targetTexture = rt; cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
            RenderTexture.active = prev; cam.targetTexture = null; RenderTexture.ReleaseTemporary(rt);
            string dir = Path.GetFullPath("ClaudeBridge/shots"); Directory.CreateDirectory(dir);
            string outPath = Path.Combine(dir, file);
            File.WriteAllBytes(outPath, tex.EncodeToJPG(88));
            Object.DestroyImmediate(tex); Object.DestroyImmediate(go); Object.DestroyImmediate(inst);
            return outPath;
        }

        /// <summary>Lists the room's main parts (name, centre, size) relative to the floor centre — for placing gameplay props.</summary>
        public static string Layout(float minSize = 0.6f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!asset) return "no prefab";
            var prefab = (GameObject)Object.Instantiate(asset); prefab.hideFlags = HideFlags.HideAndDontSave;
            var lines = new List<string>();
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
            {
                var b = r.bounds; if (Mathf.Max(b.size.x, b.size.y, b.size.z) < minSize) continue;
                var mf = r.GetComponent<MeshFilter>(); long tri = 0;
                if (mf && mf.sharedMesh) for (int sm = 0; sm < mf.sharedMesh.subMeshCount; sm++) tri += mf.sharedMesh.GetIndexCount(sm) / 3;
                lines.Add($"{r.name}|{b.center.x:F2},{b.center.y:F2},{b.center.z:F2}|{b.size.x:F2},{b.size.y:F2},{b.size.z:F2}|{tri}|{string.Join("+", r.sharedMaterials.Where(m => m).Select(m => m.name))}");
            }
            foreach (Transform m in prefab.transform.Find("Markers")) lines.Add($"MARKER {m.name}|{m.position.x:F2},{m.position.y:F2},{m.position.z:F2}|yaw {m.eulerAngles.y:F0}");
            File.WriteAllText(Path.GetFullPath("ClaudeBridge/shots/lab_layout.txt"), string.Join("\n", lines));
            Object.DestroyImmediate(prefab);
            return lines.Count + " parts";
        }
    }
}
