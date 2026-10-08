using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    /// <summary>
    /// Call after ConfigureAnatomy and before ConfigureInputs. Reuses Plaque_Cap,
    /// Plaque_Exposed_Core and the original lipid mesh/material look. No target is
    /// replaced in World.targets, and proxy targets are deliberately not registered.
    /// Generated assets are written only when Main invokes this builder in Unity.
    /// </summary>
    public static class ArteryPlaqueStateBuilder
    {
        public const string Folder = JourneyGeometry.Root + "/Generated/Polish/Plaque";
        const string ZonePrefix = "Scene06_LipidZone_";
        const float ResidualBulge = .20f;

        public static JourneyPlaqueState Apply(JourneyWorld w, JourneyArtComposition art,
            Transform parent, Material lipidMat, Material capMat)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before building plaque state.");
            if (!w || !w.mission || !w.mover || !w.mover.path || w.targets == null)
                throw new InvalidOperationException("Plaque state requires a configured JourneyWorld, mission and path.");
            if (!art || !parent) throw new InvalidOperationException("Plaque state requires art and an always-active polish parent.");
            var path = w.mover.path;
            var location = w.locationRoots[path.locationIds[6 - 3]];
            if (!location || parent == location.transform || parent.IsChildOf(location.transform))
                throw new InvalidOperationException("Plaque state controller must be outside the scene06 location root.");

            var logical = RequiredTarget(w, "lipid");
            var originalCap = RequiredTarget(w, "cap");
            var plaque = RequiredTarget(w, "plaque");
            var flow = RequiredTarget(w, "plaque-flow");
            var mode = RequiredTarget(w, "pulse-mode");
            var capFilter = RequiredFilter(location.transform, "Plaque_Cap");
            var coreFilter = RequiredFilter(location.transform, "Plaque_Exposed_Core");
            var capRenderer = capFilter.GetComponent<Renderer>();
            var coreRenderer = coreFilter.GetComponent<Renderer>();
            if (!capRenderer || !coreRenderer || !capFilter.transform.parent)
                throw new InvalidOperationException("ConfigureAnatomy's embedded cap/core renderers are required.");
            var geometryRoot = capFilter.transform.parent;
            if (coreFilter.transform.parent != geometryRoot)
                throw new InvalidOperationException("Cap and exposed core must share the embedded plaque geometry root.");
            if (!capFilter.sharedMesh.isReadable || capFilter.sharedMesh.uv.Length != capFilter.sharedMesh.vertexCount
                || capFilter.sharedMesh.normals.Length != capFilter.sharedMesh.vertexCount)
                throw new InvalidOperationException("The source cap must retain readable vertices, normals and its UV0 footprint.");
            var geometryScale = geometryRoot.lossyScale;
            if (Mathf.Abs(geometryScale.x) < .0001f || Mathf.Abs(geometryScale.y) < .0001f || Mathf.Abs(geometryScale.z) < .0001f)
                throw new InvalidOperationException("The embedded plaque root must have a nonzero scale.");

            // Capture the source BEFORE suppressing the old logical target's visual.
            // Resolve original prefab material look separately: ConfigureModels/Variant
            // may already have substituted the scene instance's materials.
            var sourceFilter = LipidSource(logical);
            var sourcePrefabOriginal = AssetDatabase.LoadAssetAtPath<GameObject>(
                BuildArteryVrScene.Root + "/Imported/Visual/HybridArteryVisual.prefab");
            var originalFilter = sourcePrefabOriginal
                ? sourcePrefabOriginal.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f =>
                    f.sharedMesh && f.GetComponent<Renderer>() && f.name == "HD_S06_Lipid_Droplet_0") : null;
            if (!sourceFilter) sourceFilter = originalFilter;
            if (!sourceFilter || !sourceFilter.sharedMesh || !sourceFilter.sharedMesh.isReadable)
                throw new InvalidOperationException("Original lipid droplet MeshFilter child is missing or unreadable.");
            var sourceRenderer = originalFilter ? originalFilter.GetComponent<Renderer>() : sourceFilter.GetComponent<Renderer>();
            Material sourceMaterial = sourceRenderer && sourceRenderer.sharedMaterial ? sourceRenderer.sharedMaterial : lipidMat;
            if (!sourceMaterial) throw new InvalidOperationException("A lipid source material is required.");

            EnsureFolder(Folder);
            var lipidMesh = CheapLipidMesh(sourceFilter.sharedMesh);
            var gold = GoldenMaterial(sourceMaterial);
            capRenderer.sharedMaterial = ProtectedMaterial(capRenderer.sharedMaterial ? capRenderer.sharedMaterial : capMat);
            var stages = CapStages(path, capFilter);

            var state = parent.GetComponent<JourneyPlaqueState>();
            if (!state) state = parent.gameObject.AddComponent<JourneyPlaqueState>();
            // Idempotent rebuilding removes only this builder's generated zone objects.
            for (int i = geometryRoot.childCount - 1; i >= 0; i--)
            {
                var child = geometryRoot.GetChild(i);
                if (child.name.StartsWith(ZonePrefix, StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            var capProxy = capFilter.GetComponent<JourneyTarget>();
            if (!capProxy) capProxy = capFilter.gameObject.AddComponent<JourneyTarget>();
            capProxy.targetId = "cap";
            capProxy.label = "Защищённая фиброзная покрышка";
            capProxy.mission = w.mission;
            capProxy.hoverEmission = false;
            var capCollider = capFilter.GetComponent<MeshCollider>();
            if (!capCollider) capCollider = capFilter.gameObject.AddComponent<MeshCollider>();
            foreach (var collider in capFilter.GetComponents<Collider>()) collider.enabled = false;
            capCollider.convex = false;
            capCollider.isTrigger = false;
            capCollider.sharedMesh = stages[0];
            capFilter.gameObject.layer = logical.gameObject.layer;

            var contacts = new JourneyPlaqueZone[JourneyPlaqueState.ZoneCount];
            var fullPositions = new Vector3[contacts.Length];
            var reducedPositions = new Vector3[contacts.Length];
            // UV0 is the existing circular cap footprint, NOT a spatial centre channel.
            // These three contacts occupy the exposed core's small right-wall region.
            var footprints = new[] { new Vector2(.322f, .284f), new Vector2(.689f, .419f), new Vector2(.378f, .568f) };
            var diameters = new[] { .16f, .19f, .17f };
            var uv = stages[0].uv;
            var triangles = stages[0].triangles;
            var full = stages[0].vertices;
            var reduced = stages[3].vertices;
            for (int i = 0; i < contacts.Length; i++)
            {
                FindFootprintTriangle(uv, triangles, footprints[i], out int triangle, out Vector3 barycentric);
                var surface = capFilter.transform.TransformPoint(Sample(full, triangles, triangle, barycentric));
                var reducedSurface = capFilter.transform.TransformPoint(Sample(reduced, triangles, triangle, barycentric));
                float s = NearestDistance(path, surface);
                var radial = (surface - path.Centre(s)).normalized;
                // Keep the gold contact fully in front of the protected cap from the
                // lumen side. A shallow embedded sphere was physically occluded by
                // the cap mesh for some footprints, making a valid zone unreachable.
                // The visual remains attached to the exposed core while its contact
                // has a readable inward hemisphere for precision BioTool aiming.
                var lumen=path.Centre(s);
                // Use a normalized depth rather than a fixed metric offset: the cap
                // thickness varies with each sampled footprint and station.
                var centre = Vector3.Lerp(surface,lumen,.72f);
                var reducedCentre = Vector3.Lerp(reducedSurface,lumen,.72f);
                fullPositions[i] = geometryRoot.InverseTransformPoint(centre);
                reducedPositions[i] = geometryRoot.InverseTransformPoint(reducedCentre);

                var go = new GameObject(ZonePrefix + i);
                go.transform.SetParent(geometryRoot, false);
                go.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(-radial, path.Forward(s)));
                var parentScale = geometryRoot.lossyScale;
                go.transform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
                go.layer = logical.gameObject.layer;
                var proxy = go.AddComponent<JourneyTarget>();
                proxy.targetId = "lipid";
                proxy.label = "Липидная зона " + (i + 1) + " / 3";
                proxy.mission = w.mission;
                proxy.hoverEmission = false;
                var sphere = go.AddComponent<SphereCollider>();
                sphere.center = Vector3.zero;
                sphere.radius = diameters[i] * .46f;
                sphere.enabled = false;

                // A small wrapper keeps off-centre source mesh bounds centred while
                // the entire visual shrinks. Never instantiate a sampled source GO.
                var visual = new GameObject("Golden source lipid lobule").transform;
                visual.SetParent(go.transform, false);
                var model = new GameObject("Shared source mesh");
                model.transform.SetParent(visual, false);
                float scale = diameters[i] / MaxSize(lipidMesh.bounds.size);
                model.transform.localScale = Vector3.one * scale;
                model.transform.localPosition = -lipidMesh.bounds.center * scale;
                model.AddComponent<MeshFilter>().sharedMesh = lipidMesh;
                var renderer = model.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = gold;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                var zone = go.AddComponent<JourneyPlaqueZone>();
                zone.Configure(state, i, proxy, sphere, visual, renderer);
                contacts[i] = zone;
            }
            ValidateSpacing(contacts, diameters);

            RemoveLipidAttachment(art);
            var composition = geometryRoot.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name.StartsWith("Embedded foam cell", StringComparison.Ordinal)).ToArray();
            state.Configure(w, art, geometryRoot, logical, contacts, fullPositions, reducedPositions,
                capFilter, capRenderer, coreRenderer, composition, capCollider, capProxy, stages,
                new[] { logical, originalCap, plaque, flow, mode });
            foreach (var collider in logical.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var renderer in logical.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (var renderer in originalCap.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (var collider in originalCap.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            logical.transform.SetPositionAndRotation(contacts[0].transform.position, contacts[0].transform.rotation);
            logical.SetHomePose();
            capProxy.SetHomePose();
            EditorUtility.SetDirty(art);
            EditorUtility.SetDirty(state);
            AssetDatabase.SaveAssets();
            return state;
        }

        static JourneyTarget RequiredTarget(JourneyWorld w, string id)
        {
            var target = w.targets.FirstOrDefault(t => t && t.targetId == id);
            if (!target) throw new InvalidOperationException("Missing registered target: " + id);
            return target;
        }
        static MeshFilter RequiredFilter(Transform root, string name)
        {
            var filter = root.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f => f.name == name && f.sharedMesh);
            if (!filter) throw new InvalidOperationException("Call ConfigureAnatomy first: missing " + name);
            return filter;
        }
        static MeshFilter LipidSource(JourneyTarget target)
        {
            return target.GetComponentsInChildren<MeshFilter>(true)
                .Where(f => f.sharedMesh && f.GetComponent<Renderer>())
                .OrderByDescending(f => f.name.IndexOf("Lipid", StringComparison.OrdinalIgnoreCase) >= 0)
                .FirstOrDefault();
        }
        static float MaxSize(Vector3 size) { return Mathf.Max(.0001f, Mathf.Max(size.x, Mathf.Max(size.y, size.z))); }

        static Material GoldenMaterial(Material source)
        {
            var material = new Material(source) { name = "Scene06_SourceLipid_Gold", enableInstancing = true };
            var color = new Color(1f, .65f, .14f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_PatchColor")) material.SetColor("_PatchColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .32f);
            if (material.HasProperty("_PulseAmplitude")) material.SetFloat("_PulseAmplitude", 0f);
            if (material.HasProperty("_EmissionColor"))
            {
                if (material.shader.name == "Universal Render Pipeline/Lit") material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(.012f, .006f, 0f, 1f));
            }
            return Save(material, material.name + ".mat");
        }

        static Material ProtectedMaterial(Material source)
        {
            if (!source) throw new InvalidOperationException("Protected cap material is missing.");
            bool stopPulse = source.HasProperty("_PulseAmplitude") && Mathf.Abs(source.GetFloat("_PulseAmplitude")) > .00001f;
            bool enableEmission = source.shader.name == "Universal Render Pipeline/Lit"
                && source.HasProperty("_EmissionColor") && !source.IsKeywordEnabled("_EMISSION");
            if (!stopPulse && !enableEmission) return source;
            // One protected variant only if required. Retain the tissue shader,
            // textures, UV transforms and _PatchColor rather than replacing with URP Lit.
            var material = new Material(source) { name = "Scene06_ProtectedCap", enableInstancing = true };
            if (stopPulse) material.SetFloat("_PulseAmplitude", 0f);
            if (enableEmission) material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
            return Save(material, material.name + ".mat");
        }

        static Mesh[] CapStages(ArteryJourneyPath path, MeshFilter filter)
        {
            var source = filter.sharedMesh;
            var full = source.vertices;
            var centres = new List<Vector3>();
            source.GetUVs(1, centres); // Source UV1 is a Vector3 centre channel; UV0 remains untouched.
            var reduced = UnityEngine.Object.Instantiate(source);
            reduced.name = "Scene06_Cap_Reduced";
            var vertices = new Vector3[full.Length];
            var safeCentres = new List<Vector3>(full.Length);
            for (int i = 0; i < full.Length; i++)
            {
                var position = filter.transform.TransformPoint(full[i]);
                bool hasCentre = centres.Count == full.Length;
                var sourceCentre = hasCentre ? filter.transform.TransformPoint(centres[i]) : position;
                float s = NearestDistance(path, sourceCentre);
                // Reject malformed centre channels instead of interpreting ordinary UVs as positions.
                if (hasCentre && (sourceCentre - path.Centre(s)).sqrMagnitude > .01f) s = NearestDistance(path, position);
                s = Mathf.Clamp(s, 0f, path.Length);
                var centre = path.Centre(s);
                var radial = position - centre;
                float radius = radial.magnitude;
                float protrusion = Mathf.Max(0f, path.Radius(s) - .012f - radius);
                // Outward displacement follows EACH vertex's own wall centre. The
                // cap footprint/edge is fixed; only its local inward bulge decreases.
                position += radial.normalized * (protrusion * (1f - ResidualBulge));
                vertices[i] = filter.transform.InverseTransformPoint(position);
                safeCentres.Add(filter.transform.InverseTransformPoint(centre));
            }
            reduced.vertices = vertices;
            reduced.SetUVs(1, safeCentres);
            reduced.RecalculateNormals();
            reduced.RecalculateTangents();
            reduced.RecalculateBounds();
            var stages = new Mesh[4];
            stages[0] = source;
            stages[3] = Save(reduced, reduced.name + ".asset");
            var reducedNormals = stages[3].normals;
            var fullNormals = source.normals;
            for (int step = 1; step < 3; step++)
            {
                var stage = UnityEngine.Object.Instantiate(source);
                stage.name = "Scene06_Cap_Pulse" + step;
                var positions = new Vector3[full.Length];
                var normals = new Vector3[full.Length];
                float fraction = step / 3f;
                for (int i = 0; i < full.Length; i++)
                {
                    positions[i] = Vector3.Lerp(full[i], vertices[i], fraction);
                    normals[i] = Vector3.Lerp(fullNormals[i], reducedNormals[i], fraction).normalized;
                }
                stage.vertices = positions;
                stage.normals = normals;
                stage.SetUVs(1, safeCentres);
                stage.RecalculateTangents();
                stage.RecalculateBounds();
                stages[step] = Save(stage, stage.name + ".asset");
            }
            return stages;
        }

        static float NearestDistance(ArteryJourneyPath path, Vector3 position)
        {
            float best = float.MaxValue, distance = 0f;
            for (int i = 0; i < path.points.Length - 1; i++)
            {
                Vector3 delta = path.points[i + 1] - path.points[i];
                float t = Mathf.Clamp01(Vector3.Dot(position - path.points[i], delta) / Mathf.Max(.000001f, delta.sqrMagnitude));
                float squared = (position - path.points[i] - delta * t).sqrMagnitude;
                if (squared >= best) continue;
                best = squared;
                distance = Mathf.Lerp(path.distances[i], path.distances[i + 1], t);
            }
            return distance;
        }

        static void FindFootprintTriangle(Vector2[] uv, int[] triangles, Vector2 point, out int triangle, out Vector3 barycentric)
        {
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = uv[triangles[i]];
                var b = uv[triangles[i + 1]] - a;
                var c = uv[triangles[i + 2]] - a;
                float determinant = b.x * c.y - b.y * c.x;
                if (Mathf.Abs(determinant) < .00000001f) continue;
                var p = point - a;
                float v = (p.x * c.y - p.y * c.x) / determinant;
                float z = (b.x * p.y - b.y * p.x) / determinant;
                float u = 1f - v - z;
                if (u < -.00001f || v < -.00001f || z < -.00001f) continue;
                triangle = i;
                barycentric = new Vector3(u, v, z);
                return;
            }
            throw new InvalidOperationException("Lipid contact lies outside the existing cap footprint: " + point);
        }
        static Vector3 Sample(Vector3[] vertices, int[] triangles, int triangle, Vector3 barycentric)
        {
            return vertices[triangles[triangle]] * barycentric.x + vertices[triangles[triangle + 1]] * barycentric.y
                + vertices[triangles[triangle + 2]] * barycentric.z;
        }

        static Mesh CheapLipidMesh(Mesh source)
        {
            if (source.subMeshCount == 1 && source.GetIndexCount(0) / 3 >= 50 && source.GetIndexCount(0) / 3 <= 200) return source;
            // The original HD droplet is 288 triangles. This shared 100-triangle LOD
            // samples only its small vertex buffer in the EDITOR: every position and
            // UV comes from that imported blob, preserving its silhouette/material look.
            const int sides = 10, rows = 6;
            var old = source.vertices;
            var oldUV = source.uv;
            var size = source.bounds.extents;
            var directions = old.Select(v => new Vector3((v.x - source.bounds.center.x) / Mathf.Max(.0001f, size.x),
                (v.y - source.bounds.center.y) / Mathf.Max(.0001f, size.y),
                (v.z - source.bounds.center.z) / Mathf.Max(.0001f, size.z)).normalized).ToArray();
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            void Pick(Vector3 direction)
            {
                int best = 0;
                float dot = -2f;
                for (int i = 0; i < directions.Length; i++)
                {
                    float value = Vector3.Dot(direction, directions[i]);
                    if (value > dot) { dot = value; best = i; }
                }
                vertices.Add(old[best]);
                uv.Add(oldUV.Length == old.Length ? oldUV[best] : Vector2.zero);
            }
            Pick(Vector3.up);
            for (int row = 1; row < rows; row++) for (int side = 0; side < sides; side++)
            {
                float angle = side * Mathf.PI * 2f / sides, latitude = row * Mathf.PI / rows;
                Pick(new Vector3(Mathf.Sin(latitude) * Mathf.Cos(angle), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(angle)));
            }
            Pick(Vector3.down);
            int bottom = vertices.Count - 1;
            var triangles = new List<int>();
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                triangles.AddRange(new[] { 0, 1 + next, 1 + side });
                for (int row = 0; row < rows - 2; row++)
                {
                    int a = 1 + row * sides + side, b = 1 + row * sides + next;
                    int c = a + sides, d = b + sides;
                    triangles.AddRange(new[] { a, b, c, b, d, c });
                }
                triangles.AddRange(new[] { 1 + (rows - 2) * sides + side, 1 + (rows - 2) * sides + next, bottom });
            }
            var mesh = new Mesh { name = "Scene06_SourceLipid_LOD100", indexFormat = IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            // Safe centre channel for a source shader that expects TEXCOORD1.
            mesh.SetUVs(1, Enumerable.Repeat(source.bounds.center, vertices.Count).ToList());
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return Save(mesh, mesh.name + ".asset");
        }

        static void ValidateSpacing(JourneyPlaqueZone[] contacts, float[] diameters)
        {
            for (int i = 0; i < contacts.Length; i++) for (int j = i + 1; j < contacts.Length; j++)
                if (Vector3.Distance(contacts[i].transform.position, contacts[j].transform.position)
                    <= (diameters[i] + diameters[j]) * .5f + .025f)
                    throw new InvalidOperationException("Plaque lipid zones overlap: " + i + " / " + j);
        }
        static void RemoveLipidAttachment(JourneyArtComposition art)
        {
            if (art.attachmentIds == null || art.wallAttachments == null) return;
            var ids = new List<string>();
            var anchors = new List<Transform>();
            int count = Mathf.Min(art.attachmentIds.Length, art.wallAttachments.Length);
            for (int i = 0; i < count; i++)
            {
                if (art.attachmentIds[i] == "lipid") continue;
                ids.Add(art.attachmentIds[i]);
                anchors.Add(art.wallAttachments[i]);
            }
            art.attachmentIds = ids.ToArray();
            art.wallAttachments = anchors.ToArray();
        }
        static T Save<T>(T asset, string filename) where T : UnityEngine.Object
        {
            string path = Folder + "/" + filename;
            JourneyGeometry.Save(asset, path);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
        static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
