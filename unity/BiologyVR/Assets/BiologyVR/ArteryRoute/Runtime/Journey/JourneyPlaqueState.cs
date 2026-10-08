using System;
using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>
    /// Scene06's three distinct pulse zones. World owns Accept/Apply; this component
    /// owns selection and presentation, and never counts a Mission.Changed as a pulse.
    /// Keep it on the always-active polish parent, outside the scene06 location root.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JourneyPlaqueState : MonoBehaviour
    {
        public const int ZoneCount = 3;
        public const float PulseDuration = .6f;

        [Serializable]
        sealed class LegacyContact
        {
            public JourneyTarget target;
            public Collider[] colliders;
            public bool[] enabled;
            public Renderer[] renderers;
        }

        [SerializeField] JourneyWorld world;
        [SerializeField] JourneyArtComposition art;
        [SerializeField] Transform geometryRoot;
        [SerializeField] Vector3 geometryPosition, geometryScale;
        [SerializeField] Quaternion geometryRotation;
        [SerializeField] JourneyTarget logicalLipid;
        [SerializeField] Vector3 logicalScale;
        [SerializeField] JourneyPlaqueZone[] zones;
        [SerializeField] Vector3[] zoneFullPositions, zoneReducedPositions;
        [SerializeField] MeshFilter capFilter;
        [SerializeField] Renderer capRenderer, coreRenderer;
        [SerializeField] Renderer[] compositionRenderers;
        [SerializeField] MeshCollider capCollider;
        [SerializeField] JourneyTarget capProxy;
        // 0 = untouched source asset; 1..3 = immutable, editor-baked collision poses.
        // Only the visual mesh is cloned at runtime, so animation cannot recook PhysX.
        [SerializeField] Mesh[] capStages;
        [SerializeField] LegacyContact[] legacy;

        static readonly int PatchColorId = Shader.PropertyToID("_PatchColor");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly int RevealId = Shader.PropertyToID("_Reveal");
        static readonly int PulseId = Shader.PropertyToID("_PulseAmplitude");
        static readonly int ToneId = Shader.PropertyToID("_Tone");

        readonly bool[] processed = new bool[ZoneCount];
        readonly float[] shrinkTime = new float[ZoneCount];
        MaterialPropertyBlock block;
        JourneyMission mission;
        Mesh runtimeCap;
        Vector3[] fullVertices, reducedVertices, fullNormals, reducedNormals;
        Vector3[] workingVertices, workingNormals;
        Vector4[] fullTangents, reducedTangents, workingTangents;
        Color capColor, coreColor;
        int capColorProperty, coreColorProperty;
        bool capEmission, coreReveal, subscribed, initialized, animating;
        int selected = -1, lastScene = -1, lastStep = -1;
        float displayedFraction, animationStart, animationTime, flashTime = PulseDuration;

        public int ProcessedCount { get; private set; }
        public float PathologyFraction => 1f - ProcessedCount / (float)ZoneCount;
        public int CapHits { get; private set; }
        public float IntroReveal { get; private set; }
        public int SelectedZoneIndex => selected;
        public JourneyTarget LogicalTarget => logicalLipid;
        public JourneyPlaqueZone[] Zones => zones;
        public bool CanPulse => initialized && PulseStep && selected >= 0 && selected < ZoneCount && !processed[selected];

        bool Scene06 => mission && mission.SceneNumber == 6;
        bool PulseStep => Scene06 && mission.Goals != null && mission.Current != null
            && mission.Current.action == StudyAction.Pulse && mission.Current.target == "lipid";
        bool Revealed => Scene06 && mission.Goals != null && mission.StepIndex >= 2;

        /// <summary>Editor builder configuration; preserves the registered World.targets.</summary>
        public void Configure(JourneyWorld configuredWorld, JourneyArtComposition configuredArt,
            Transform root, JourneyTarget lipid, JourneyPlaqueZone[] contacts,
            Vector3[] fullPositions, Vector3[] reducedPositions, MeshFilter capMesh,
            Renderer capSurface, Renderer coreSurface, Renderer[] composition,
            MeshCollider protectedContact, JourneyTarget protectedTarget, Mesh[] meshStages,
            JourneyTarget[] legacyTargets)
        {
            Unsubscribe();
            world = configuredWorld;
            art = configuredArt;
            geometryRoot = root;
            geometryPosition = root.localPosition;
            geometryRotation = root.localRotation;
            geometryScale = root.localScale;
            logicalLipid = lipid;
            logicalScale = lipid.transform.localScale;
            zones = contacts;
            zoneFullPositions = fullPositions;
            zoneReducedPositions = reducedPositions;
            capFilter = capMesh;
            capRenderer = capSurface;
            coreRenderer = coreSurface;
            compositionRenderers = composition;
            capCollider = protectedContact;
            capProxy = protectedTarget;
            capStages = meshStages;
            legacy = new LegacyContact[legacyTargets.Length];
            for (int i = 0; i < legacy.Length; i++)
            {
                var target = legacyTargets[i];
                var colliders = target.GetComponentsInChildren<Collider>(true);
                var enabled = new bool[colliders.Length];
                for (int j = 0; j < colliders.Length; j++) enabled[j] = colliders[j].enabled;
                legacy[i] = new LegacyContact { target = target, colliders = colliders,
                    enabled = enabled, renderers = target.GetComponentsInChildren<Renderer>(true) };
            }
            mission = world.mission;
            selected = 0;
            // Initial authored state: the cap can be observed, composition is hidden.
            SetComposition(false);
            foreach (var zone in zones) if (zone && zone.Contact) zone.Contact.enabled = false;
            if (capCollider) capCollider.enabled = false;
            if (Application.isPlaying)
            {
                Initialize();
                Subscribe();
                ApplyMissionState();
            }
        }

        void Awake() { Initialize(); }
        void OnEnable()
        {
            if (!Application.isPlaying) return;
            Initialize();
            Subscribe();
            ApplyMissionState();
        }
        void OnDisable()
        {
            Unsubscribe();
            SetProxyContacts(false);
        }
        void OnDestroy()
        {
            Unsubscribe();
            if (!runtimeCap) return;
            if (capFilter && capFilter.sharedMesh == runtimeCap) capFilter.sharedMesh = capStages[0];
            Destroy(runtimeCap);
        }

        void Initialize()
        {
            if(block==null)block=new MaterialPropertyBlock();
            if (initialized || !world || !capFilter || capStages == null || capStages.Length != 4
                || zones == null || zones.Length != ZoneCount || zoneFullPositions == null
                || zoneFullPositions.Length != ZoneCount || zoneReducedPositions == null
                || zoneReducedPositions.Length != ZoneCount) return;
            for (int i = 0; i < ZoneCount; i++)
                if (!zones[i] || !zones[i].Target || !zones[i].Contact) return;
            mission = world.mission;
            var source = capStages[0];
            var reduced = capStages[ZoneCount];
            if (!source || !reduced || !source.isReadable || !reduced.isReadable
                || source.vertexCount != reduced.vertexCount) return;
            fullVertices = source.vertices;
            reducedVertices = reduced.vertices;
            fullNormals = source.normals;
            reducedNormals = reduced.normals;
            if (fullNormals.Length != fullVertices.Length || reducedNormals.Length != fullVertices.Length) return;
            workingVertices = new Vector3[fullVertices.Length];
            workingNormals = new Vector3[fullVertices.Length];
            fullTangents = source.tangents;
            reducedTangents = reduced.tangents;
            if (fullTangents.Length == fullVertices.Length && reducedTangents.Length == fullVertices.Length)
                workingTangents = new Vector4[fullVertices.Length];
            runtimeCap = Instantiate(source);
            runtimeCap.name = "Scene06 Cap — runtime deformation";
            runtimeCap.MarkDynamic();
            var bounds = source.bounds;
            bounds.Encapsulate(reduced.bounds);
            runtimeCap.bounds = bounds;
            capFilter.sharedMesh = runtimeCap;
            CacheSurface(capRenderer, out capColorProperty, out capColor);
            CacheSurface(coreRenderer, out coreColorProperty, out coreColor);
            capEmission = capRenderer && capRenderer.sharedMaterial && capRenderer.sharedMaterial.HasProperty(EmissionId);
            coreReveal = coreRenderer && coreRenderer.sharedMaterial && coreRenderer.sharedMaterial.HasProperty(RevealId);
            initialized = true;
            ResetForScene();
        }

        void Subscribe()
        {
            if (subscribed || !mission) return;
            mission.Changed += OnMissionChanged;
            subscribed = true;
        }
        void Unsubscribe()
        {
            if (!subscribed || !mission) return;
            mission.Changed -= OnMissionChanged;
            subscribed = false;
        }
        void OnMissionChanged() { ApplyMissionState(); }

        void ApplyMissionState()
        {
            if (!initialized || !mission) return;
            // Changed comes AFTER ShowScene/Art.Stage and after Apply/Progress++.
            // Reset only on entry/restart, never in response to the next pulse's progress.
            if (Scene06 && !world.IsEpisodeCompleted(6) && (lastScene != 6 || (mission.StepIndex == 0 && lastStep > 0))) ResetForScene();
            lastScene = mission.SceneNumber;
            lastStep = mission.StepIndex;
            if (geometryRoot) geometryRoot.gameObject.SetActive(Scene06||world.IsEpisodeCompleted(6));
            RestoreGeometryPose();
            GateLegacyContacts();
            if (!Scene06)
            {
                SetComposition(world.IsEpisodeCompleted(6));
                SetProxyContacts(false);
                return;
            }
            if (capRenderer) capRenderer.enabled = true;
            if (mission.WrongCapHits != CapHits) ShowCapDamage(mission.WrongCapHits);
            SetComposition(Revealed);
            UpdateZonePresentation();
            SetProxyContacts(PulseStep);
            RetargetLogicalPointer();
            UpdateCapFeedback();
        }

        /// <summary>
        /// Select the hit proxy, or the first unprocessed zone for the registered
        /// logical root (desktop/HUD). Call BEFORE Mission.Accept in World.Primary.
        /// A reused proxy returns false with feedback and must not fall back to another zone.
        /// </summary>
        public bool SelectZone(JourneyTarget target)
        {
            if (!CanSelect()) return false;
            int index;
            if (!target || target == logicalLipid) index = FirstUnprocessed();
            else
            {
                var zone = target.GetComponent<JourneyPlaqueZone>();
                if (!zone || zone.Owner != this || zone.Target != target) return false;
                index = zone.ZoneIndex;
            }
            if (index < 0 || index >= ZoneCount || processed[index])
            {
                selected = -1;
                mission.FeedbackMessage("Эта липидная зона уже обработана. Выбери другую золотистую мишень.");
                return false;
            }
            selected = index;
            RetargetLogicalPointer();
            return true;
        }

        bool CanSelect() { return initialized && PulseStep; }
        public bool IsZoneProcessed(int index) { return index >= 0 && index < ZoneCount && processed[index]; }

        /// <summary>
        /// Called only from World.Apply(Pulse, "lipid", progress), BEFORE Mission.Changed.
        /// Next selection is synchronous, so three direct Accept calls also choose three zones.
        /// </summary>
        public void ApplySelectedPulse()
        {
            if (!initialized || !CanPulse) return;
            int index = selected;
            processed[index] = true;
            ProcessedCount++;
            shrinkTime[index] = 0f;
            if (zones[index].Contact) zones[index].Contact.enabled = false;
            animationStart = displayedFraction;
            animationTime = 0f;
            animating = true;
            selected = FirstUnprocessed();
            RetargetLogicalPointer();
        }

        public void ShowCapDamage(int hits)
        {
            CapHits = Mathf.Clamp(hits, 0, 3);
            flashTime = hits > 0 ? 0f : PulseDuration;
            UpdateCapFeedback();
        }

        /// <summary>Entry/restart/checkpoint restores vertices, zones, hits and reveal.</summary>
        public void ResetForScene()
        {
            ProcessedCount = 0;
            CapHits = 0;
            IntroReveal = 0f;
            displayedFraction = animationStart = animationTime = 0f;
            flashTime = PulseDuration;
            animating = false;
            for (int i = 0; i < ZoneCount; i++) { processed[i] = false; shrinkTime[i] = PulseDuration; }
            selected = 0;
            RestoreGeometryPose();
            if (initialized) WriteCapPose(0f);
            AssignCollisionStage(0);
            SetComposition(false);
            SetProxyContacts(false);
            UpdateZonePresentation();
            RetargetLogicalPointer();
            UpdateCapFeedback();
        }
        public void CheckPoint() { ResetForScene(); }

        void LateUpdate()
        {
            if (!initialized || !mission) return;
            // External ShowScene calls may not raise Changed. Reapply gates/poses, but
            // leave pulse counting exclusively in ApplySelectedPulse.
            if (lastScene != mission.SceneNumber || lastStep != mission.StepIndex) ApplyMissionState();
            if (!Scene06&&!world.IsEpisodeCompleted(6)) return;
            RestoreGeometryPose();
            IntroReveal = Mathf.MoveTowards(IntroReveal, Revealed||world.IsEpisodeCompleted(6) ? 1f : 0f, Time.deltaTime / PulseDuration);
            if (animating)
            {
                animationTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(animationTime / PulseDuration));
                displayedFraction = Mathf.Lerp(animationStart, ProcessedCount / (float)ZoneCount, t);
                WriteCapPose(displayedFraction);
                if (animationTime >= PulseDuration)
                {
                    animating = false;
                    // Exactly one assignment per settled pulse; never attach the mutable
                    // visual mesh to the collider. Rapid accepts coalesce into the last pose.
                    AssignCollisionStage(ProcessedCount);
                }
            }
            for (int i = 0; i < ZoneCount; i++)
                if (processed[i]) shrinkTime[i] = Mathf.Min(PulseDuration, shrinkTime[i] + Time.deltaTime);
            flashTime = Mathf.Min(PulseDuration, flashTime + Time.deltaTime);
            SetComposition(Revealed||world.IsEpisodeCompleted(6));
            UpdateZonePresentation();
            SetProxyContacts(PulseStep);
            RetargetLogicalPointer();
            UpdateCapFeedback();
        }

        void WriteCapPose(float fraction)
        {
            if (!runtimeCap) return;
            for (int i = 0; i < workingVertices.Length; i++)
            {
                workingVertices[i] = Vector3.Lerp(fullVertices[i], reducedVertices[i], fraction);
                workingNormals[i] = Vector3.Lerp(fullNormals[i], reducedNormals[i], fraction).normalized;
                if (workingTangents == null) continue;
                Vector4 tangent = Vector4.Lerp(fullTangents[i], reducedTangents[i], fraction);
                Vector3 direction = new Vector3(tangent.x, tangent.y, tangent.z);
                direction = Vector3.ProjectOnPlane(direction, workingNormals[i]).normalized;
                workingTangents[i] = new Vector4(direction.x, direction.y, direction.z, fullTangents[i].w);
            }
            runtimeCap.vertices = workingVertices;
            runtimeCap.normals = workingNormals;
            if (workingTangents != null) runtimeCap.tangents = workingTangents;
        }

        void AssignCollisionStage(int index)
        {
            if (!capCollider || capStages == null || capStages.Length != 4) return;
            var mesh = capStages[Mathf.Clamp(index, 0, ZoneCount)];
            if (capCollider.sharedMesh != mesh) capCollider.sharedMesh = mesh;
        }

        void RestoreGeometryPose()
        {
            if (!geometryRoot) return;
            geometryRoot.localPosition = geometryPosition;
            geometryRoot.localRotation = geometryRotation;
            geometryRoot.localScale = geometryScale;
        }

        void GateLegacyContacts()
        {
            if (legacy == null) return;
            foreach (var item in legacy)
            {
                if (!item.target) continue;
                string id = item.target.targetId;
                bool enabled = false;
                if (Scene06)
                {
                    enabled = id == "plaque" ? mission.StepIndex == 1
                        : id == "plaque-flow" ? mission.StepIndex == 0
                        : id == "pulse-mode" && mission.StepIndex == 2;
                    if (id == "lipid" || id == "cap" || id == "plaque")
                        foreach (var renderer in item.renderers) if (renderer) renderer.enabled = false;
                }
                else if (id == "lipid")
                    foreach (var renderer in item.renderers) if (renderer) renderer.enabled = false;
                for (int i = 0; i < item.colliders.Length; i++)
                    if (item.colliders[i]) item.colliders[i].enabled = enabled && item.enabled[i];
            }
        }

        void SetProxyContacts(bool enabled)
        {
            // ConfigureInputs runs AFTER the builder and creates/assigns the target layer.
            // Read the logical target's final layer at runtime rather than creating a layer.
            int layer = logicalLipid ? logicalLipid.gameObject.layer : gameObject.layer;
            if (zones != null) for (int i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                if (!zone) continue;
                zone.gameObject.layer = layer;
                if (zone.Target) zone.Target.mission = mission;
                if (zone.Contact) zone.Contact.enabled = enabled && i < ZoneCount && !processed[i];
            }
            if (capProxy) { capProxy.gameObject.layer = layer; capProxy.mission = mission; }
            if (capCollider) capCollider.enabled = enabled;
        }

        void SetComposition(bool visible)
        {
            if (coreRenderer) coreRenderer.enabled = visible && displayedFraction < .999f;
            if (compositionRenderers != null)
                foreach (var renderer in compositionRenderers) if (renderer) renderer.enabled = visible;
            if (zones != null) foreach (var zone in zones)
                if (zone && zone.VisualRenderer)
                    zone.VisualRenderer.enabled = visible && (!IsZoneProcessed(zone.ZoneIndex)
                        || shrinkTime[zone.ZoneIndex] < PulseDuration);
            if (initialized && coreRenderer)
            {
                coreRenderer.GetPropertyBlock(block);
                if (coreReveal) block.SetFloat(RevealId, IntroReveal);
                if (coreColorProperty != 0) block.SetColor(coreColorProperty,
                    Color.Lerp(coreColor, capColor, displayedFraction * .7f));
                block.SetFloat(PulseId, 0f);
                block.SetFloat(ToneId, 1f);
                coreRenderer.SetPropertyBlock(block);
            }
        }

        void UpdateZonePresentation()
        {
            if (zones == null || zoneFullPositions == null || zoneReducedPositions == null) return;
            for (int i = 0; i < Mathf.Min(ZoneCount, zones.Length); i++)
            {
                var zone = zones[i];
                if (!zone) continue;
                zone.transform.localPosition = Vector3.Lerp(zoneFullPositions[i], zoneReducedPositions[i], displayedFraction);
                if (!zone.Visual) continue;
                float shrink = processed[i] ? Mathf.SmoothStep(1f, .035f, shrinkTime[i] / PulseDuration) : 1f;
                zone.Visual.localScale = zone.VisualScale * (shrink * Mathf.SmoothStep(.035f, 1f, IntroReveal));
            }
        }

        int FirstUnprocessed()
        {
            for (int i = 0; i < ZoneCount; i++) if (!processed[i]) return i;
            return -1;
        }
        void RetargetLogicalPointer()
        {
            if (!Scene06 || !logicalLipid || zones == null || zones.Length != ZoneCount) return;
            int index = selected >= 0 ? selected : FirstUnprocessed();
            if (index < 0) index = ZoneCount - 1;
            var zone = zones[index];
            if (!zone) return;
            bool moved = (logicalLipid.transform.position - zone.transform.position).sqrMagnitude > .00000001f;
            logicalLipid.transform.SetPositionAndRotation(zone.transform.position, zone.transform.rotation);
            logicalLipid.transform.localScale = logicalScale;
            if (moved) logicalLipid.SetHomePose();
        }

        static void CacheSurface(Renderer renderer, out int property, out Color color)
        {
            var material = renderer ? renderer.sharedMaterial : null;
            property = !material ? 0 : material.HasProperty(PatchColorId) ? PatchColorId
                : material.HasProperty(BaseColorId) ? BaseColorId : material.HasProperty(ColorId) ? ColorId : 0;
            color = property != 0 ? material.GetColor(property) : Color.white;
        }
        void UpdateCapFeedback()
        {
            if (!initialized || !capRenderer) return;
            float flash = 1f - Mathf.SmoothStep(0f, 1f, flashTime / PulseDuration);
            var warning = new Color(1f, .14f, .06f, 1f);
            capRenderer.GetPropertyBlock(block);
            if (capColorProperty != 0) block.SetColor(capColorProperty,
                Color.Lerp(capColor, warning, Mathf.Clamp01(CapHits * .1f + flash * .65f)));
            if (capEmission) block.SetColor(EmissionId, warning * (flash * .055f));
            block.SetFloat(PulseId, 0f);
            block.SetFloat(ToneId, 1f);
            block.SetFloat(RevealId, 1f);
            capRenderer.SetPropertyBlock(block);
        }
    }
}
