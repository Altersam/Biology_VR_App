using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BiologyVR.V2;

namespace BiologyVR.V2.Editor
{
    /// <summary>
    /// Builds the new Biology VR 2 scene (chapter 1) from code: procedural textures, meshes,
    /// materials, the artery, the player rig, tools, professor, gates and the exhibit.
    /// Safe to re-run: generated assets are overwritten, the scene is rebuilt from scratch.
    /// The working version (ArteryNarrativeVR_VisualRework) is not touched.
    /// </summary>
    public static class Bvr2SceneBuilder
    {
        public const string Root = "Assets/BiologyVR/BiologyVR2";
        public const string Gen = Root + "/Generated";
        public const string ScenePath = Root + "/Scenes/BiologyVR2.unity";
        static Mesh toolBody;
        const string GameFeelAudio = "Assets/BiologyVR/ArteryJourney/Generated/Audio/GameFeel";
        const string FontPath = "Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf";

        [MenuItem("Biology VR 2/Build Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first");
            foreach (var d in new[] { Gen, Gen + "/Textures", Gen + "/Materials", Gen + "/Meshes", Root + "/Scenes" }) Directory.CreateDirectory(d);
            AssetDatabase.Refresh();
            ConfigureImports();
            // the prologue room (SecVarLab) is converted before the new scene replaces the open one
            if (Bvr2LabImport.Available && !File.Exists(Bvr2LabImport.PrefabPath)) Debug.Log("[BVR2] " + Bvr2LabImport.BuildPrefab());

            // ---------- textures & sprites ----------
            var pattern = SaveTexture(EndotheliumPattern(512, out var normal), "Endothelium_Pattern", false);
            var normalTex = SaveTexture(normal, "Endothelium_Normal", true);
            var rounded = SaveSprite(RoundedRect(64, 20), "UI_Rounded", new Vector4(24, 24, 24, 24));
            var ring = SaveSprite(RingTex(256, 0.8f), "UI_Ring", Vector4.zero);
            var dot = SaveSprite(Radial(64, 0.85f, 1f), "UI_Dot", Vector4.zero);
            var glow = SaveSprite(Radial(128, 0f, 1f), "UI_Glow", Vector4.zero);

            // ---------- meshes ----------
            var redMesh = SaveMesh(Bvr2Meshes.RedCell(8, 20), "RedCell");
            var redLow = SaveMesh(Bvr2Meshes.RedCell(4, 12), "RedCell_Low");
            var whiteMesh = SaveMesh(Bvr2Meshes.IcoSphere(3, 0.12f, 7, 1f, "BVR2_WhiteCell"), "WhiteCell");
            var plateletMesh = SaveMesh(Bvr2Meshes.IcoSphere(1, 0.22f, 3, 0.42f, "BVR2_Platelet"), "Platelet");
            var sphere = SaveMesh(Bvr2Meshes.IcoSphere(2, 0, 1, 1, "BVR2_Sphere"), "Sphere");
            var quad = SaveMesh(Bvr2Meshes.Quad(), "Quad");
            var torus = SaveMesh(Bvr2Meshes.Torus(1f, 0.035f, 64, 8), "Torus");
            var torusThick = SaveMesh(Bvr2Meshes.Torus(1f, 0.07f, 48, 8, "BVR2_TorusThick"), "TorusThick");
            var disc = SaveMesh(Bvr2Meshes.Ring(0f, 1f, 0, 360, 48, "BVR2_Disc"), "Disc");
            var ringMesh = SaveMesh(Bvr2Meshes.Ring(0.86f, 1f, 0, 360, 64), "Ring");
            var cone = SaveMesh(Bvr2Meshes.Cone(24), "Cone");
            toolBody = SaveMesh(Bvr2Meshes.IcoSphere(2, 0, 1, 1, "BVR2_ToolBody"), "ToolBody");
            var virusMesh = SaveMesh(Bvr2Meshes.Virus(0), "Virus");
            var virusHalfA = SaveMesh(Bvr2Meshes.Virus(1, "BVR2_VirusHalfA"), "VirusHalfA");
            var virusHalfB = SaveMesh(Bvr2Meshes.Virus(-1, "BVR2_VirusHalfB"), "VirusHalfB");
            var antibodyMesh = SaveMesh(Bvr2Meshes.Antibody(), "Antibody");
            var debrisMesh = SaveMesh(Bvr2Meshes.IcoSphere(2, 0.38f, 19, 0.7f, "BVR2_Debris"), "Debris");
            var hedgehogMesh = SaveMesh(Bvr2Meshes.Hedgehog(), "Hedgehog");

            // ---------- materials ----------
            var endo = Mat("Endothelium", "BVR2/Endothelium"); endo.SetTexture("_PatternTex", pattern); endo.SetTexture("_NormalTex", normalTex);
            endo.SetVector("_Tiling", new Vector4(10f, 12f, 0, 0)); endo.SetFloat("_NormalStrength", 0.6f); endo.SetFloat("_Gloss", 0.86f);
            // coral-pink living tissue (lighter than raw blood red, so the scene reads as biology, not horror)
            endo.SetColor("_Base", new Color(0.8f, 0.28f, 0.29f)); endo.SetColor("_Deep", new Color(0.42f, 0.09f, 0.12f)); endo.SetColor("_Sheen", new Color(1f, 0.86f, 0.8f)); endo.SetColor("_Nucleus", new Color(0.68f, 0.3f, 0.45f));
            endo.SetFloat("_Translucency", 0.6f);
            var cellRed = Mat("Cell_Red", "BVR2/Cell"); cellRed.enableInstancing = true;
            cellRed.SetColor("_BaseColor", new Color(0.86f, 0.13f, 0.14f)); cellRed.SetColor("_RimColor", new Color(1f, 0.6f, 0.55f)); cellRed.SetColor("_CoreColor", new Color(0.55f, 0.04f, 0.08f)); cellRed.SetFloat("_Dimple", 0.65f); cellRed.SetFloat("_Gloss", 0.78f);
            var cellWhite = Mat("Cell_White", "BVR2/Cell"); cellWhite.enableInstancing = true;
            cellWhite.SetColor("_BaseColor", new Color(0.86f, 0.88f, 0.95f)); cellWhite.SetColor("_RimColor", new Color(0.6f, 0.95f, 1f)); cellWhite.SetColor("_CoreColor", new Color(0.55f, 0.5f, 0.8f)); cellWhite.SetFloat("_Dimple", 0f); cellWhite.SetFloat("_Gloss", 0.55f); cellWhite.SetColor("_Emission", new Color(0.06f, 0.12f, 0.16f));
            var cellPlatelet = Mat("Cell_Platelet", "BVR2/Cell"); cellPlatelet.enableInstancing = true;
            cellPlatelet.SetColor("_BaseColor", new Color(0.95f, 0.72f, 0.32f)); cellPlatelet.SetColor("_RimColor", new Color(1f, 0.9f, 0.55f)); cellPlatelet.SetColor("_CoreColor", new Color(0.7f, 0.4f, 0.5f)); cellPlatelet.SetFloat("_Dimple", 0.2f); cellPlatelet.SetColor("_Emission", new Color(0.12f, 0.07f, 0.02f));
            var streak = Mat("Speed_Streak", "BVR2/Particle"); streak.SetFloat("_Billboard", 0f); streak.SetColor("_Color", new Color(1f, 0.9f, 0.85f, 1f)); streak.SetFloat("_Softness", 0.95f);
            var mote = Mat("Plasma_Mote", "BVR2/Particle"); mote.enableInstancing = true; mote.SetColor("_Color", new Color(1f, 0.82f, 0.7f, 0.45f)); mote.SetFloat("_NearFade", 1.3f);
            var spark = Mat("Spark", "BVR2/Particle"); spark.enableInstancing = true; spark.SetColor("_Color", new Color(1, 1, 1, 1)); spark.SetFloat("_Softness", 0.8f);
            var glowMat = Mat("Glow", "BVR2/Glow"); glowMat.SetColor("_Color", Bvr2Style.Ally); glowMat.SetFloat("_Intensity", 1.6f);
            var gateMat = Mat("Gate", "BVR2/Glow"); gateMat.SetColor("_Color", Bvr2Style.Knowledge); gateMat.SetFloat("_Intensity", 2.2f); gateMat.SetFloat("_Core", 0.7f);
            var gateInner = Mat("Gate_Inner", "BVR2/Glow"); gateInner.SetColor("_Color", Bvr2Style.Knowledge); gateInner.SetFloat("_Intensity", 0.6f); gateInner.SetFloat("_Core", 1f); gateInner.SetVector("_Bands", new Vector4(6, 0.6f, 3, 0.7f));
            var beamMat = Mat("Beam", "BVR2/Glow"); beamMat.SetColor("_Color", Color.white); beamMat.SetFloat("_Intensity", 1.6f); beamMat.SetFloat("_Core", 1f);
            var coneMat = Mat("ScanCone", "BVR2/Glow"); coneMat.SetColor("_Color", Bvr2Style.Ally); coneMat.SetFloat("_Intensity", 0.9f); coneMat.SetFloat("_Core", 0.05f); coneMat.SetFloat("_Fresnel", 2.5f); coneMat.SetVector("_Bands", new Vector4(5, 2.5f, 4, 0.6f));
            var holo = Mat("Hologram", "BVR2/Hologram"); holo.SetColor("_Tint", new Color(0.8f, 0.95f, 1f)); holo.SetFloat("_Holo", 0.8f); holo.SetFloat("_Alpha", 0.55f);
            var tissue = Mat("Tissue", "BVR2/Tissue"); tissue.SetColor("_BaseColor", new Color(0.85f, 0.4f, 0.4f)); tissue.SetFloat("_Studio", 1f);
            var media = Mat("Tissue_Media", "BVR2/Tissue"); media.SetColor("_BaseColor", new Color(0.82f, 0.26f, 0.26f)); media.SetColor("_StripeColor", new Color(0.55f, 0.08f, 0.12f)); media.SetVector("_Stripes", new Vector4(36, 0, 3, 0.55f)); media.SetColor("_Rim", new Color(0.4f, 0.1f, 0.1f)); media.SetFloat("_Gloss", 0.45f);
            var intimaMat = Mat("Tissue_Intima", "BVR2/Tissue"); intimaMat.SetColor("_BaseColor", new Color(1f, 0.62f, 0.62f)); intimaMat.SetFloat("_Gloss", 0.9f); intimaMat.SetColor("_Rim", new Color(0.3f, 0.6f, 0.7f));
            var adv = Mat("Tissue_Adventitia", "BVR2/Tissue"); adv.SetColor("_BaseColor", new Color(0.95f, 0.85f, 0.68f)); adv.SetColor("_StripeColor", new Color(0.75f, 0.62f, 0.45f)); adv.SetVector("_Stripes", new Vector4(14, 9, 2, 0.45f)); adv.SetFloat("_Gloss", 0.25f);
            foreach (var tm in new[] { media, intimaMat, adv }) tm.SetFloat("_Studio", 1f);
            var device = Mat("Device", "BVR2/Tissue"); device.SetColor("_BaseColor", new Color(0.82f, 0.86f, 0.9f)); device.SetFloat("_Gloss", 0.8f); device.SetFloat("_Unfogged", 1f); device.SetColor("_Rim", new Color(0.2f, 0.5f, 0.6f));
            var deviceDark = Mat("Device_Dark", "BVR2/Tissue"); deviceDark.SetColor("_BaseColor", new Color(0.12f, 0.14f, 0.17f)); deviceDark.SetFloat("_Gloss", 0.7f); deviceDark.SetFloat("_Unfogged", 1f);
            var lipid = Mat("Lipid", "BVR2/Tissue"); lipid.SetColor("_BaseColor", new Color(1f, 0.78f, 0.28f)); lipid.SetFloat("_Gloss", 0.85f); lipid.SetColor("_Emission", new Color(0.25f, 0.15f, 0.02f)); lipid.SetColor("_Rim", new Color(0.6f, 0.4f, 0.05f)); lipid.SetFloat("_Studio", 0.4f);
            var capMat = Mat("Plaque_Cap", "BVR2/Tissue"); capMat.SetColor("_BaseColor", new Color(0.93f, 0.85f, 0.7f)); capMat.SetFloat("_Gloss", 0.9f); capMat.SetColor("_Rim", new Color(0.4f, 0.3f, 0.2f));
            var fibrin = Mat("Fibrin", "BVR2/Particle"); fibrin.SetFloat("_Billboard", 0f); fibrin.SetColor("_Color", new Color(1f, 0.92f, 0.75f, 1f)); fibrin.SetFloat("_Softness", 0.7f);
            var bolt = Mat("Impulse_Bolt", "BVR2/Glow"); bolt.SetColor("_Color", new Color(1f, 0.8f, 0.35f)); bolt.SetFloat("_Intensity", 2.4f); bolt.SetFloat("_Core", 1f);
            var lipidRing = Mat("Lipid_Window", "BVR2/Glow"); lipidRing.SetColor("_Color", new Color(0.5f, 1f, 0.55f)); lipidRing.SetFloat("_Intensity", 1.8f); lipidRing.SetFloat("_Core", 0.8f);
            var foamMat = Mat("Foam_Cell", "BVR2/Cell"); foamMat.SetColor("_BaseColor", new Color(0.95f, 0.9f, 0.72f)); foamMat.SetColor("_RimColor", new Color(1f, 0.95f, 0.7f)); foamMat.SetColor("_CoreColor", new Color(0.95f, 0.7f, 0.25f)); foamMat.SetFloat("_Dimple", 0f); foamMat.SetFloat("_Gloss", 0.6f); foamMat.SetColor("_Emission", new Color(0.12f, 0.09f, 0.02f));
            var virusMat = Mat("Virus", "BVR2/Cell"); virusMat.enableInstancing = true;
            virusMat.SetColor("_BaseColor", new Color(0.55f, 0.86f, 0.32f)); virusMat.SetColor("_RimColor", new Color(0.85f, 1f, 0.5f)); virusMat.SetColor("_CoreColor", new Color(0.25f, 0.45f, 0.12f)); virusMat.SetFloat("_Dimple", 0f); virusMat.SetFloat("_Gloss", 0.6f); virusMat.SetColor("_Emission", new Color(0.06f, 0.12f, 0.02f));
            var macroMat = Mat("Macrophage", "BVR2/Cell"); macroMat.enableInstancing = true;
            macroMat.SetColor("_BaseColor", new Color(0.93f, 0.91f, 0.88f)); macroMat.SetColor("_RimColor", new Color(0.75f, 0.95f, 1f)); macroMat.SetColor("_CoreColor", new Color(0.72f, 0.62f, 0.82f)); macroMat.SetFloat("_Dimple", 0f); macroMat.SetFloat("_Gloss", 0.5f); macroMat.SetColor("_Emission", new Color(0.07f, 0.09f, 0.11f));
            var antibodyMat = Mat("Antibody", "BVR2/Glow"); antibodyMat.SetColor("_Color", new Color(0.5f, 0.95f, 1f)); antibodyMat.SetFloat("_Intensity", 1.8f); antibodyMat.SetFloat("_Core", 0.9f);
            var spotMat = Mat("Infected_Spot", "BVR2/Tissue"); spotMat.SetColor("_BaseColor", new Color(0.45f, 0.75f, 0.25f)); spotMat.SetColor("_Emission", new Color(0.12f, 0.3f, 0.04f)); spotMat.SetFloat("_Gloss", 0.7f);
            var rnaMat = Mat("Virus_RNA", "BVR2/Glow"); rnaMat.SetColor("_Color", new Color(1f, 0.55f, 0.9f)); rnaMat.SetFloat("_Intensity", 2f); rnaMat.SetFloat("_Core", 1f);
            var debrisMat = Mat("Debris", "BVR2/Cell"); debrisMat.enableInstancing = true; debrisMat.SetColor("_BaseColor", new Color(0.52f, 0.4f, 0.36f)); debrisMat.SetColor("_RimColor", new Color(0.75f, 0.62f, 0.5f)); debrisMat.SetColor("_CoreColor", new Color(0.32f, 0.22f, 0.22f)); debrisMat.SetFloat("_Gloss", 0.2f); debrisMat.SetFloat("_Dimple", 0f);
            var patchMat = Mat("Collagen_Patch", "BVR2/Tissue"); patchMat.SetColor("_BaseColor", new Color(0.95f, 0.88f, 0.78f)); patchMat.SetFloat("_Gloss", 0.6f); patchMat.SetFloat("_Studio", 0.5f); patchMat.SetColor("_Rim", new Color(0.5f, 0.45f, 0.35f));
            var collagenMat = Mat("Collagen_Fibre", "BVR2/Particle"); collagenMat.SetFloat("_Billboard", 0f); collagenMat.SetColor("_Color", new Color(1f, 0.93f, 0.8f, 1f)); collagenMat.SetFloat("_Softness", 0.6f);
            var woundRing = Mat("Wound_Ring", "BVR2/Glow"); woundRing.SetColor("_Color", new Color(0.55f, 0.95f, 1f)); woundRing.SetFloat("_Intensity", 1.6f); woundRing.SetFloat("_Core", 0.8f);
            var nodeMat = Mat("Cascade_Node", "BVR2/Glow"); nodeMat.SetColor("_Color", new Color(0.6f, 0.9f, 1f)); nodeMat.SetFloat("_Intensity", 2f); nodeMat.SetFloat("_Core", 0.9f);
            var hedgehogMat = Mat("Hedgehog", "BVR2/Cell"); hedgehogMat.enableInstancing = true;
            hedgehogMat.SetColor("_BaseColor", new Color(0.62f, 0.42f, 0.9f)); hedgehogMat.SetColor("_RimColor", new Color(1f, 0.6f, 1f)); hedgehogMat.SetColor("_CoreColor", new Color(0.25f, 0.12f, 0.4f)); hedgehogMat.SetFloat("_Dimple", 0f); hedgehogMat.SetFloat("_Gloss", 0.85f); hedgehogMat.SetColor("_Emission", new Color(0.1f, 0.03f, 0.14f));
            var wallCellMat = Mat("Wall_Cell", "BVR2/Tissue"); wallCellMat.SetColor("_BaseColor", new Color(1f, 0.62f, 0.6f)); wallCellMat.SetFloat("_Gloss", 0.75f); wallCellMat.SetColor("_Rim", new Color(0.45f, 0.2f, 0.3f));
            var flagMat = Mat("Antigen_Flag", "BVR2/Glow"); flagMat.SetColor("_Color", new Color(0.55f, 1f, 0.2f)); flagMat.SetFloat("_Intensity", 1.1f); flagMat.SetFloat("_Core", 0.6f);
            var tkMat = Mat("T_Killer", "BVR2/Cell"); tkMat.enableInstancing = true;
            tkMat.SetColor("_BaseColor", new Color(0.72f, 0.8f, 1f)); tkMat.SetColor("_RimColor", new Color(0.85f, 0.95f, 1f)); tkMat.SetColor("_CoreColor", new Color(0.35f, 0.4f, 0.75f)); tkMat.SetFloat("_Dimple", 0f); tkMat.SetFloat("_Gloss", 0.6f); tkMat.SetColor("_Emission", new Color(0.06f, 0.08f, 0.16f));
            var labFloor = Mat("Lab_Floor", "BVR2/Tissue"); labFloor.SetColor("_BaseColor", new Color(0.16f, 0.19f, 0.24f)); labFloor.SetFloat("_Gloss", 0.85f); labFloor.SetFloat("_Unfogged", 1f); labFloor.SetFloat("_Studio", 1f); labFloor.SetColor("_StripeColor", new Color(0.25f, 0.55f, 0.65f)); labFloor.SetVector("_Stripes", new Vector4(36, 36, 9, 0.35f));
            var labWall = Mat("Lab_Wall", "BVR2/Tissue"); labWall.SetColor("_BaseColor", new Color(0.2f, 0.24f, 0.3f)); labWall.SetFloat("_Gloss", 0.4f); labWall.SetFloat("_Unfogged", 1f); labWall.SetFloat("_Studio", 1f); labWall.SetColor("_StripeColor", new Color(0.3f, 0.42f, 0.5f)); labWall.SetVector("_Stripes", new Vector4(40, 14, 6, 0.4f)); labWall.SetColor("_Rim", new Color(0.2f, 0.3f, 0.4f));
            var diveSkin = Mat("Dive_Skin", "BVR2/Tissue"); diveSkin.SetColor("_BaseColor", new Color(0.93f, 0.62f, 0.6f)); diveSkin.SetColor("_StripeColor", new Color(0.8f, 0.48f, 0.46f)); diveSkin.SetVector("_Stripes", new Vector4(60, 60, 3, 0.35f)); diveSkin.SetFloat("_Unfogged", 1f); diveSkin.SetFloat("_Studio", 0.7f);
            var diveFat = Mat("Dive_Fat", "BVR2/Tissue"); diveFat.SetColor("_BaseColor", new Color(1f, 0.86f, 0.45f)); diveFat.SetColor("_StripeColor", new Color(0.9f, 0.7f, 0.3f)); diveFat.SetVector("_Stripes", new Vector4(22, 22, 4, 0.45f)); diveFat.SetFloat("_Unfogged", 1f); diveFat.SetFloat("_Studio", 0.7f); diveFat.SetColor("_Emission", new Color(0.1f, 0.07f, 0.01f));
            var diveWall = Mat("Dive_Wall", "BVR2/Tissue"); diveWall.SetColor("_BaseColor", new Color(0.88f, 0.32f, 0.32f)); diveWall.SetColor("_StripeColor", new Color(0.55f, 0.1f, 0.12f)); diveWall.SetVector("_Stripes", new Vector4(70, 0, 3, 0.5f)); diveWall.SetFloat("_Unfogged", 1f); diveWall.SetFloat("_Studio", 0.6f);
            var screen = Mat("Screen", "BVR2/Screen");
            var uiOverlay = Mat("UI_Overlay", "BVR2/UI Overlay");
            AssetDatabase.SaveAssets();

            // ---------- scene ----------
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.fog = false; RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.5f, 0.25f, 0.26f); RenderSettings.skybox = null;

            var gameGo = new GameObject("Biology VR 2 · Game");
            var game = gameGo.AddComponent<Bvr2Game>();
            game.font = AssetDatabase.LoadAssetAtPath<Font>(FontPath); game.uiOverlay = uiOverlay; game.rounded = rounded; game.ringSprite = ring; game.dot = dot; game.glow = glow;
            game.holoBackdrop = HoloBackdropMaterial(); game.quadMesh = quad;
            game.organism = Child<Bvr2Organism>(gameGo, "Organism");
            game.codex = Child<Bvr2Codex>(gameGo, "Codex");
            game.environment = Child<Bvr2Environment>(gameGo, "Environment");

            // vessel
            var vesselGo = new GameObject("Artery"); var vessel = vesselGo.AddComponent<Bvr2Vessel>(); game.vessel = vessel;
            vessel.controlPoints = new List<Vector3> {
                new(0,0,0), new(0,0,60), new(7,2,120), new(4,-3,180), new(-6,0,240), new(-3,4,300), new(6,1,360), new(3,-4,420),
                new(-5,0,480), new(-1,3,540), new(6,0,600), new(2,-3,660), new(-5,1,720), new(0,3,780), new(5,0,840), new(0,0,900),
                // chapter 2 «Бляшка»: plaque, unstable neighbour, then the artery narrows into a small branch
                new(-4,2,960), new(-1,-2,1020), new(5,0,1080), new(2,3,1140), new(-4,1,1200), new(-2,-2,1260), new(1,0,1320),
                // chapter 3 «Первый контакт»: the vessel widens again into a calm stretch (the arena)
                new(3,2,1380), new(-2,-1,1440), new(4,1,1500), new(0,-2,1560), new(-3,1,1620), new(0,0,1680),
                // chapter 4 «Прорыв»: a calm, almost straight stretch with the tear
                new(2,1,1740), new(1,0,1800), new(0,0,1860), new(-2,1,1920), new(-1,-1,1980), new(0,0,2040),
                // chapter 5 «Очаг»: the artery widens at a fork — a round arena around the infection focus
                new(2,1,2100), new(3,-1,2160), new(0,0,2220), new(-3,1,2280), new(-1,0,2340), new(0,0,2400),
                // chapter 6 «Гомеостаз»: a calm stretch with six sensor rings and the exit
                new(1,1,2460), new(0,0,2520), new(0,0,2580), new(0,0,2640) };
            vessel.baseRadius = 6f; vessel.radiusKeys = new List<Vector2> { new(0, 1.15f), new(30, 1f), new(1196, 1f), new(1224, 0.42f), new(1318, 0.42f), new(1350, 1.05f), new(1680, 1.05f), new(2040, 1.05f), new(2120, 1.05f), new(2162, 1.5f), new(2234, 1.5f), new(2276, 1.05f), new(2400, 1.05f), new(2640, 1.05f) };
            vessel.plaques = new Bvr2Vessel.Plaque[2];
            vessel.plaques[0] = new Bvr2Vessel.Plaque { active = true, s = 1010f, halfLength = 9f, angleDeg = 200f, halfAngleDeg = 62f, height = 2.6f, reveal = 0.45f };
            vessel.plaques[1] = new Bvr2Vessel.Plaque { active = true, s = 1036f, halfLength = 5.5f, angleDeg = 35f, halfAngleDeg = 48f, height = 1.5f, reveal = 0.35f };
            vessel.Rebuild();
            int chunk = 0;
            foreach (var m in vessel.BuildWallMeshes(0.6f, 56, 36f))
            {
                var mesh = SaveMesh(m, "Wall_" + chunk.ToString("00"));
                var w = new GameObject("Wall " + chunk++); w.transform.SetParent(vesselGo.transform, false);
                w.AddComponent<MeshFilter>().sharedMesh = mesh; var r = w.AddComponent<MeshRenderer>(); r.sharedMaterial = endo; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            }

            // player
            var playerGo = new GameObject("Player");
            var input = playerGo.AddComponent<Bvr2Input>(); game.input = input;
            var rig = playerGo.AddComponent<Bvr2Rig>(); rig.input = input; rig.Build(screen); game.rig = rig;
            var swimmer = playerGo.AddComponent<Bvr2Swimmer>(); swimmer.input = input; swimmer.rig = rig; swimmer.vessel = vessel; swimmer.organism = game.organism; game.swimmer = swimmer;
            var streaks = playerGo.AddComponent<Bvr2SpeedLines>(); streaks.swimmer = swimmer; streaks.head = rig.head.transform; streaks.material = streak;
            playerGo.transform.position = vessel.Centre(14f) - Vector3.up * 0f;
            playerGo.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(vessel.Tangent(14f), Vector3.up));

            game.environment.organism = game.organism; game.environment.vessel = vessel; game.environment.viewCamera = rig.head;

            // blood
            var blood = Child<Bvr2Blood>(gameGo, "Bloodstream"); game.blood = blood;
            blood.vessel = vessel; blood.organism = game.organism; blood.viewer = rig.head.transform;
            blood.redMesh = redMesh; blood.redMeshLow = redLow; blood.whiteMesh = whiteMesh; blood.plateletMesh = plateletMesh; blood.moteMesh = quad;
            blood.redMaterial = cellRed; blood.whiteMaterial = cellWhite; blood.plateletMaterial = cellPlatelet; blood.moteMaterial = mote;

            // fx, audio
            var fx = Child<Bvr2Fx>(gameGo, "FX"); game.fx = fx; fx.quadMesh = quad; fx.ringMesh = torusThick; fx.sparkMaterial = spark; fx.ringMaterial = glowMat; fx.beamMaterial = beamMat;
            var sound = Child<Bvr2Audio>(gameGo, "Audio"); game.sound = sound; sound.organism = game.organism; sound.swimmer = swimmer;
            sound.heartThump = Clip(Root + "/Audio/BVR2_HeartThump.wav"); sound.heartLoop = Clip(GameFeelAudio + "/Amb_Heartbeat_72.wav");
            sound.flowLoop = Clip(GameFeelAudio + "/Amb_BloodFlow_Loop.wav"); sound.musicCalm = Clip(GameFeelAudio + "/Mus_Calm_Pad_Loop.wav");
            sound.musicTension = Clip(GameFeelAudio + "/Mus_Tension_Loop.wav"); sound.musicRecovery = Clip(GameFeelAudio + "/Mus_Recovery_Pad_Loop.wav");
            sound.scanLoop = Clip(GameFeelAudio + "/SFX_Scan_Loop.wav"); sound.fieldLoop = Clip(GameFeelAudio + "/SFX_Field_Loop.wav");
            sound.thrustLoop = Clip(Root + "/Audio/BVR2_Thrust_Loop.wav"); sound.ambienceLoop = Clip(Root + "/Audio/BVR2_Ambience_Loop.wav");
            sound.clips = AssetDatabase.FindAssets("t:AudioClip", new[] { GameFeelAudio, Root + "/Audio" }).Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).Where(c => c).ToList();

            // HUD
            var hud = Child<Bvr2Hud>(gameGo, "HUD"); game.hud = hud; hud.rig = rig; hud.organism = game.organism;

            // tools
            var scanner = BuildScanner(rig, device, deviceDark, glowMat, coneMat, cone, torus, sphere, ring, uiOverlay, game.font);
            scanner.input = input; scanner.blood = blood; scanner.codex = game.codex; scanner.audioDirector = sound; scanner.fx = fx; game.scanner = scanner;
            var bio = BuildBioTool(rig, device, deviceDark, glowMat, torus, sphere, uiOverlay, game.font, out var modeText, out var coils);
            bio.input = input; bio.blood = blood; bio.audioDirector = sound; bio.fx = fx; game.bioTool = bio;
            var impulse = bio.gameObject.AddComponent<Bvr2Impulse>(); game.impulse = impulse;
            impulse.rig = rig; impulse.input = input; impulse.vessel = vessel; impulse.fx = fx; impulse.audioDirector = sound; impulse.tip = bio.tip; impulse.boltMesh = sphere; impulse.boltMaterial = bolt;
            var abilities = bio.gameObject.AddComponent<Bvr2Abilities>(); game.abilities = abilities;
            abilities.input = input; abilities.attract = bio; abilities.impulse = impulse; abilities.hud = hud; abilities.audioDirector = sound; abilities.screenText = modeText; abilities.tint = coils;
            bio.redMesh = redMesh; bio.whiteMesh = whiteMesh; bio.plateletMesh = plateletMesh; bio.redMaterial = cellRed; bio.whiteMaterial = cellWhite; bio.plateletMaterial = cellPlatelet;

            // vascular field
            var field = Child<Bvr2VascularField>(gameGo, "Vascular Field"); game.field = field;
            field.rig = rig; field.input = input; field.vessel = vessel; field.organism = game.organism; field.audioDirector = sound;
            var ra = GlowObject("Field Ring A", torusThick, glowMat, field.transform); var rb = GlowObject("Field Ring B", torusThick, glowMat, field.transform);
            field.ringA = ra.transform; field.ringB = rb.transform; field.ringRenderers = new[] { ra.GetComponent<Renderer>(), rb.GetComponent<Renderer>() };

            // diorama + question
            var dio = Child<Bvr2Diorama>(gameGo, "Diorama"); game.diorama = dio;
            dio.rig = rig; dio.input = input; dio.audioDirector = sound; dio.fx = fx; dio.holoMaterial = holo; dio.cellRed = cellRed; dio.cellWhite = cellWhite; dio.cellPlatelet = cellPlatelet;
            dio.tissueMaterial = tissue; dio.glowMaterial = glowMat; dio.redMesh = redMesh; dio.whiteMesh = whiteMesh; dio.plateletMesh = plateletMesh; dio.sphereMesh = sphere; dio.ringMesh = ringMesh; dio.torusMesh = torusThick; dio.fibrinMaterial = fibrin; dio.lipidMaterial = lipid;
            dio.virusMesh = virusMesh; dio.virusHalfA = virusHalfA; dio.virusHalfB = virusHalfB; dio.antibodyMesh = antibodyMesh; dio.virusMaterial = virusMat; dio.antibodyMaterial = antibodyMat; dio.macrophageMaterial = macroMat;
            var question = Child<Bvr2Question>(gameGo, "Question"); game.question = question; question.rig = rig; question.input = input; question.audioDirector = sound; question.fx = fx; question.hud = hud;

            // professor
            game.professor = BuildProfessor(rig, hud);

            // chapter 1 props
            var chapter = Child<Bvr2Chapter1>(gameGo, "Chapter 1 · Поток"); chapter.game = game;
            var props = new GameObject("Chapter 1 Props").transform;
            for (int i = 0; i < 6; i++) chapter.tutorialGates.Add(Gate("Tutorial Gate " + (i + 1), torus, disc, gateMat, gateInner, props));
            for (int i = 0; i < 5; i++) chapter.raceGates.Add(Gate("Spasm Gate " + (i + 1), torus, disc, gateMat, gateInner, props));
            chapter.anchorPad = AnchorPad(sphere, torus, glowMat, device, props);
            chapter.exhibit = Exhibit(vessel.baseRadius, intimaMat, media, adv, props, out chapter.intima, out chapter.media, out chapter.adventitia);

            // chapter 2 props
            var ch2 = Child<Bvr2Chapter2>(gameGo, "Chapter 2 · Бляшка"); ch2.game = game;
            var props2 = new GameObject("Chapter 2 Props").transform;
            BuildChapter2(ch2, vessel, game.organism, props2, sphere, torusThick, whiteMesh, redMesh, plateletMesh, lipid, capMat, lipidRing, foamMat, cellRed, cellPlatelet, fibrin, glowMat, device);
            ch2.embolus.rig = rig; ch2.embolus.input = input; ch2.embolus.vessel = vessel; ch2.embolus.organism = game.organism; ch2.embolus.fx = fx; ch2.embolus.audioDirector = sound; ch2.embolus.tip = bio.tip;

            // chapter 3: immune battlefield, antibodies (left hand), Signal (right hand), synchronisation
            var props3 = new GameObject("Chapter 3 Props").transform;
            var immune = Child<Bvr2Immune>(gameGo, "Immune"); game.immune = immune;
            immune.vessel = vessel; immune.organism = game.organism; immune.fx = fx; immune.audioDirector = sound; immune.rig = rig;
            immune.virusTemplate = VirusTemplate(virusMesh, virusMat, antibodyMesh, antibodyMat, props3);
            immune.spotTemplate = SpotTemplate(sphere, torus, spotMat, glowMat, props3);
            for (int i = 0; i < 4; i++)
            {
                var mg = new GameObject("Macrophage " + (i + 1)); mg.transform.SetParent(props3, false);
                var body = Part("Body", whiteMesh, macroMat, mg.transform, Vector3.zero, Vector3.one * 0.78f);
                var ph = mg.AddComponent<Bvr2Phagocyte>(); ph.body = body.GetComponent<Renderer>(); immune.phagocytes.Add(ph);
                mg.SetActive(false);
            }
            var ab = scanner.gameObject.AddComponent<Bvr2Antibodies>(); game.antibodies = ab;
            ab.rig = rig; ab.input = input; ab.scanner = scanner; ab.immune = immune; ab.fx = fx; ab.audioDirector = sound; ab.tip = scanner.tip;
            var sig = bio.gameObject.AddComponent<Bvr2Signal>(); game.signal = sig;
            sig.rig = rig; sig.input = input; sig.vessel = vessel; sig.immune = immune; sig.fx = fx; sig.hud = hud; sig.tip = bio.tip; abilities.signal = sig;
            var sync = playerGo.AddComponent<Bvr2Sync>(); game.sync = sync;
            sync.rig = rig; sync.immune = immune; sync.fx = fx; sync.audioDirector = sound; sync.swimmer = swimmer;
            sync.hideWhileSynced = new[] { scanner.gameObject, bio.gameObject }; sync.blobMesh = whiteMesh; sync.blobMaterial = macroMat;
            var ch3 = Child<Bvr2Chapter3>(gameGo, "Chapter 3 · Первый контакт"); ch3.game = game;
            VirusExhibit(ch3, virusHalfA, virusHalfB, virusMat, sphere, torus, rnaMat, gateMat, props3);

            // chapter 4: the tear, the Factor ability, debris for the macrophages
            var props4 = new GameObject("Chapter 4 Props").transform;
            immune.debrisTemplate = DebrisTemplate(debrisMesh, debrisMat, props4);
            var woundGo = new GameObject("Wound"); woundGo.transform.SetParent(props4, false);
            var wound = woundGo.AddComponent<Bvr2Wound>();
            wound.vessel = vessel; wound.env = game.environment; wound.organism = game.organism; wound.blood = blood; wound.fx = fx; wound.audioDirector = sound; wound.swimmer = swimmer;
            wound.s = 1800f; wound.angleDeg = 215f; wound.halfLength = 2.6f; wound.halfAngleDeg = 15f;
            wound.patchMesh = sphere; wound.plateletMesh = plateletMesh; wound.ringMesh = torusThick; wound.redMesh = redMesh;
            wound.patchMaterial = patchMat; wound.plateletMaterial = cellPlatelet; wound.ringMaterial = woundRing; wound.fibrinMaterial = fibrin; wound.collagenMaterial = collagenMat; wound.redMaterial = cellRed;
            var factor = bio.gameObject.AddComponent<Bvr2FactorTool>(); game.factor = factor; abilities.factor = factor;
            factor.rig = rig; factor.input = input; factor.vessel = vessel; factor.wound = wound; factor.fx = fx; factor.audioDirector = sound; factor.tip = bio.tip;
            var ch4 = Child<Bvr2Chapter4>(gameGo, "Chapter 4 · Прорыв"); ch4.game = game;
            ch4.wound = wound; ch4.nodeMesh = sphere; ch4.ringMesh = torus; ch4.nodeMaterial = nodeMat;
            var scanGo = new GameObject("Wound Scan Proxy"); scanGo.transform.SetParent(props4, false);
            scanGo.AddComponent<SphereCollider>().radius = 1.6f;
            var wsc = scanGo.AddComponent<Bvr2Scannable>(); wsc.codexId = "wound"; wsc.displayName = "Разрыв стенки"; wsc.scanSeconds = 1.0f; ch4.woundScan = wsc;
            ch4.anchorPad = AnchorPad(sphere, torus, glowMat, device, props4);

            // chapter 5: the infection focus — armoured virions, wall cells with flags, T-killers, the key puzzle
            var props5 = new GameObject("Chapter 5 Props").transform;
            immune.hedgehogTemplate = HedgehogTemplate(hedgehogMesh, hedgehogMat, antibodyMesh, antibodyMat, props5);
            var ch5 = Child<Bvr2Chapter5>(gameGo, "Chapter 5 · Очаг"); ch5.game = game;
            for (int i = 0; i < 12; i++) ch5.cells.Add(WallCell("Wall Cell " + (i + 1), sphere, wallCellMat, flagMat, props5));
            for (int i = 0; i < 2; i++)
            {
                var kg = new GameObject("T-killer " + (i + 1)); kg.transform.SetParent(props5, false);
                var kb = Part("Body", whiteMesh, tkMat, kg.transform, Vector3.zero, Vector3.one * 0.62f);
                var tk = kg.AddComponent<Bvr2TKiller>(); tk.body = kb.GetComponent<Renderer>(); ch5.killers.Add(tk); kg.SetActive(false);
            }
            var keyPuzzle = ch5.gameObject.AddComponent<Bvr2KeyPuzzle>(); ch5.key = keyPuzzle;
            keyPuzzle.input = input; keyPuzzle.fx = fx; keyPuzzle.audioDirector = sound;
            keyPuzzle.capsidMesh = sphere; keyPuzzle.sphereMesh = sphere; keyPuzzle.coneMesh = cone; keyPuzzle.ringMesh = torusThick; keyPuzzle.antibodyMesh = antibodyMesh; keyPuzzle.stalkMesh = sphere;
            keyPuzzle.holoMaterial = holo; keyPuzzle.glowMaterial = glowMat; keyPuzzle.antibodyMaterial = antibodyMat;

            // chapter 6: six sensor rings and the exit ring
            var props6 = new GameObject("Chapter 6 Props").transform;
            var ch6 = Child<Bvr2Chapter6>(gameGo, "Chapter 6 · Гомеостаз"); ch6.game = game;
            for (int i = 0; i < 6; i++) { var g = Gate("Sensor Ring " + (i + 1), torus, disc, gateMat, gateInner, props6); g.radius = 1.5f; g.gameObject.SetActive(false); ch6.sensors.Add(g); }
            ch6.exitGate = Gate("Exit Ring", torus, disc, gateMat, gateInner, props6); ch6.exitGate.radius = 3.2f; ch6.exitGate.gameObject.SetActive(false);

            // prologue: the lab, far above the artery
            var ch0 = Child<Bvr2Prelude>(gameGo, "Prelude · Лаборатория"); ch0.game = game;
            BuildLab(ch0, sphere, torus, torusThick, device, deviceDark, glowMat, holo, labFloor, labWall, game.font);
            ch0.plateMesh = sphere; ch0.skinMaterial = diveSkin; ch0.fatMaterial = diveFat; ch0.wallMaterial = diveWall;

            // campaign: one artery, chapters downstream of each other; chapter choice at start
            var campaign = Child<Bvr2Campaign>(gameGo, "Campaign"); game.campaign = campaign; campaign.game = game;
            campaign.chapters = new List<Bvr2ChapterBase> { ch0, chapter, ch2, ch3, ch4, ch5, ch6 };
            ch6.campaign = campaign;

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[BVR2] Built {ScenePath}: artery {vessel.Length:F0} m, {chunk} wall chunks.");
        }

        // =====================================================================================
        static T Child<T>(GameObject parent, string name) where T : Component
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false); return go.AddComponent<T>();
        }

        static GameObject GlowObject(string name, Mesh mesh, Material mat, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return go;
        }

        static GameObject Part(string name, Mesh mesh, Material mat, Transform parent, Vector3 pos, Vector3 scale, Vector3 euler = default)
        {
            var go = GlowObject(name, mesh, mat, parent); go.transform.localPosition = pos; go.transform.localScale = scale; go.transform.localRotation = Quaternion.Euler(euler); return go;
        }

        static Bvr2Gate Gate(string name, Mesh torus, Mesh disc, Material mat, Material inner, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var gate = go.AddComponent<Bvr2Gate>();
            gate.ring = Part("Ring", torus, mat, go.transform, Vector3.zero, Vector3.one).GetComponent<Renderer>();
            gate.inner = Part("Membrane", disc, inner, go.transform, Vector3.zero, Vector3.one * 0.97f).GetComponent<Renderer>();
            Part("Ring Outer", torus, mat, go.transform, Vector3.zero, Vector3.one * 1.12f);
            go.transform.localScale = Vector3.one * gate.radius;
            return gate;
        }

        static void BuildChapter2(Bvr2Chapter2 ch, Bvr2Vessel v, Bvr2Organism organism, Transform parent, Mesh sphere, Mesh torus, Mesh whiteMesh, Mesh redMesh, Mesh plateletMesh,
            Material lipid, Material cap, Material ring, Material foam, Material red, Material platelet, Material fibrin, Material glow, Material device)
        {
            var a = v.plaques[0]; var b = v.plaques[1];
            // scan target for the plaque itself (the bump is drawn by the wall shader)
            var proxy = new GameObject("Plaque Scan Proxy"); proxy.transform.SetParent(parent, false);
            proxy.transform.position = v.PointAt(a.s, v.Radius(a.s) - a.height * 0.2f, a.angleDeg);
            var sc = proxy.AddComponent<SphereCollider>(); sc.radius = a.height * 0.75f;
            var ps = proxy.AddComponent<Bvr2Scannable>(); ps.codexId = "plaque"; ps.displayName = "Бляшка"; ps.scanSeconds = 1.2f; ch.plaqueScan = ps;
            // lipid windows on the plaque surface
            float[] dsList = { -0.55f, -0.1f, 0.4f, -0.35f, 0.15f, 0.6f }; float[] daList = { -0.35f, 0.3f, -0.2f, 0.05f, -0.45f, 0.35f };
            for (int i = 0; i < 6; i++)
            {
                float s = a.s + dsList[i] * a.halfLength, ang = a.angleDeg + daList[i] * a.halfAngleDeg;
                var go = new GameObject("Lipid Window " + (i + 1)); go.transform.SetParent(parent, false);
                Vector3 pos = v.SurfacePoint(s, ang, 0.05f); Vector3 inward = (v.Centre(s) - pos).normalized;
                go.transform.position = pos; go.transform.rotation = Quaternion.LookRotation(inward, v.Tangent(s));
                var core = Part("Lipid", sphere, lipid, go.transform, Vector3.zero, new Vector3(0.5f, 0.5f, 0.32f));
                var capGo = Part("Cap", sphere, cap, go.transform, new Vector3(0, 0, -0.02f), new Vector3(0.62f, 0.62f, 0.4f));
                var ringGo = Part("Window", torus, ring, go.transform, new Vector3(0, 0, 0.16f), Vector3.one * 0.62f);
                var t = go.AddComponent<Bvr2LipidTarget>(); t.organism = organism; t.core = core.transform; t.cap = capGo.transform; t.ring = ringGo.transform;
                t.coreRenderer = core.GetComponent<Renderer>(); t.ringRenderer = ringGo.GetComponent<Renderer>(); t.s = s; t.angleDeg = ang; t.radius = 0.45f;
                ch.lipids.Add(t);
            }
            // foam cell sitting on the plaque
            var foamGo = new GameObject("Foam Cell"); foamGo.transform.SetParent(parent, false);
            float fs = a.s + 0.75f * a.halfLength, fa = a.angleDeg - 0.25f * a.halfAngleDeg;
            foamGo.transform.position = v.SurfacePoint(fs, fa, 0.9f);
            var body = Part("Body", whiteMesh, foam, foamGo.transform, Vector3.zero, Vector3.one * 0.85f);
            for (int i = 0; i < 9; i++) Part("Fat Drop", sphere, lipid, foamGo.transform, Random.onUnitSphere * 0.72f, Vector3.one * Random.Range(0.2f, 0.32f));
            foamGo.AddComponent<SphereCollider>().radius = 0.95f;
            var fsn = foamGo.AddComponent<Bvr2Scannable>(); fsn.codexId = "foam"; fsn.displayName = "Пенистая клетка"; fsn.scanSeconds = 1.0f; fsn.highlight = body.GetComponent<Renderer>(); ch.foamCell = fsn;
            foamGo.AddComponent<Bvr2Spin>().speed = new Vector3(0, 8, 3);
            // thrombus (grows on whichever cap tears)
            var thr = new GameObject("Thrombus"); thr.transform.SetParent(parent, false);
            var clot = thr.AddComponent<Bvr2Clot>(); clot.redMesh = redMesh; clot.plateletMesh = plateletMesh; clot.redMaterial = red; clot.plateletMaterial = platelet; clot.fibrinMaterial = fibrin;
            clot.size = 2.6f; clot.dome = true; clot.redCount = 26; clot.plateletCount = 34; clot.fibrinCount = 34; clot.seed = 11;
            thr.AddComponent<SphereCollider>().radius = 1.2f;
            var ts = thr.AddComponent<Bvr2Scannable>(); ts.codexId = "thrombus"; ts.displayName = "Тромб"; ts.scanSeconds = 0.9f; ch.thrombusScan = ts; ch.thrombus = clot;
            // embolus
            var emb = new GameObject("Embolus"); emb.transform.SetParent(parent, false);
            var eclot = new GameObject("Clot"); eclot.transform.SetParent(emb.transform, false);
            var ec = eclot.AddComponent<Bvr2Clot>(); ec.redMesh = redMesh; ec.plateletMesh = plateletMesh; ec.redMaterial = red; ec.plateletMaterial = platelet; ec.fibrinMaterial = fibrin;
            ec.size = 1.5f; ec.dome = false; ec.redCount = 16; ec.plateletCount = 18; ec.fibrinCount = 20; ec.seed = 23; ec.growth = 1f;
            var markMat = Mat("Embolus_Marker", "BVR2/Glow"); markMat.SetColor("_Color", Bvr2Style.Danger); markMat.SetFloat("_Intensity", 1.8f);
            var halo = Part("Marker", torus, markMat, emb.transform, Vector3.zero, Vector3.one * 1.1f);
            halo.AddComponent<Bvr2Spin>().speed = new Vector3(30, 50, 0);
            var em = emb.AddComponent<Bvr2Embolus>(); em.clot = ec; em.speedFactor = 1.4f; ch.embolus = em;
            // anchor beacon for the firing position
            ch.anchorPad = AnchorPad(sphere, torus, glow, device, parent);
        }

        static Bvr2Virus VirusTemplate(Mesh mesh, Material mat, Mesh antibodyMesh, Material antibodyMat, Transform parent)
        {
            var go = new GameObject("Virus Template"); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<SphereCollider>().radius = 1.05f;
            var sc = go.AddComponent<Bvr2Scannable>(); sc.codexId = "virus"; sc.displayName = "Вирус"; sc.scanSeconds = 0.8f; sc.highlight = r;
            var abs = new GameObject("Antibodies").transform; abs.SetParent(go.transform, false);
            var dirs = Bvr2Meshes.IcoSphere(0, 0, 1, 1, "tmp").vertices;
            for (int i = 0; i < 8 && i < dirs.Length; i++)
            {
                var d = dirs[(i * 5) % dirs.Length].normalized;
                var y = Part("Antibody", antibodyMesh, antibodyMat, abs, d * 1.4f, Vector3.one * 0.42f);
                y.transform.localRotation = Quaternion.FromToRotation(Vector3.up, -d);
                y.SetActive(false);
            }
            var v = go.AddComponent<Bvr2Virus>(); v.body = r; v.antibodies = abs; v.radius = 0.45f;
            go.SetActive(false);
            return v;
        }

        const string SciFi = "Assets/ThirdParty/SciFiHoloScreen-Shader/Prefabs/";

        /// <summary>Instantiate an asset prefab and scale it so its largest side is <paramref name="size"/> metres, standing on localPos.</summary>
        static GameObject Prefab(string path, Transform parent, Vector3 localPos, float yaw, float size)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (!src) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(src); go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.transform.localScale = Vector3.one;
            var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return go;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float k = size / Mathf.Max(0.001f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)));
            go.transform.localScale = Vector3.one * k;
            // stand it on the floor at localPos
            Vector3 bottom = parent.InverseTransformPoint(new Vector3(b.center.x, b.min.y, b.center.z));
            go.transform.localPosition = localPos - bottom * k;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }

        static Mesh Inverted(Mesh m)
        {
            var t = m.triangles; for (int i = 0; i < t.Length; i += 3) { int a = t[i]; t[i] = t[i + 1]; t[i + 1] = a; }
            var n = m.normals; for (int i = 0; i < n.Length; i++) n[i] = -n[i];
            m.triangles = t; m.normals = n; return m;
        }

        /// <summary>The lab of the prologue: a round room with a holo desk, the patient hologram, a vitals monitor and the shrinking capsule.</summary>
        // SecVarLab room (prefab coordinates are centred on the whole floor; the main room is offset by this)
        static readonly Vector3 SecVarMainRoom = new Vector3(-0.6f, 0, -0.5f);

        static void BuildLab(Bvr2Prelude ch, Mesh sphere, Mesh torus, Mesh torusThick, Material device, Material deviceDark, Material glow, Material holo, Material floorMat, Material wallMat, Font font)
        {
            var labPrefab = Bvr2LabImport.Available ? AssetDatabase.LoadAssetAtPath<GameObject>(Bvr2LabImport.PrefabPath) : null;
            if (labPrefab) { BuildSecVarLab(ch, labPrefab, sphere, torus, torusThick, device, glow, font); return; }
            var root = new GameObject("Lab").transform; root.position = new Vector3(0, 600f, 0); ch.labRoot = root;
            Part("Floor", sphere, floorMat, root, Vector3.zero, new Vector3(6.6f, 0.05f, 6.6f));
            var domeMesh = SaveMesh(Inverted(Bvr2Meshes.IcoSphere(3, 0, 1, 1, "BVR2_LabDome")), "LabDome");
            Part("Dome", domeMesh, wallMat, root, new Vector3(0, 0.2f, 0), new Vector3(7.4f, 4.2f, 7.4f));
            var ringGlow = Mat("Lab_Light_Ring", "BVR2/Glow"); ringGlow.SetColor("_Color", new Color(0.75f, 0.92f, 1f)); ringGlow.SetFloat("_Intensity", 1.4f); ringGlow.SetFloat("_Core", 0.8f);
            Part("Ceiling Ring", torusThick, ringGlow, root, new Vector3(0, 3.6f, 0), Vector3.one * 2.6f, new Vector3(90, 0, 0));
            Part("Wall Strip Low", torus, ringGlow, root, new Vector3(0, 0.6f, 0), Vector3.one * 6.9f, new Vector3(90, 0, 0));
            Part("Wall Strip High", torus, ringGlow, root, new Vector3(0, 2.4f, 0), Vector3.one * 6.5f, new Vector3(90, 0, 0));
            // the desk with the two instruments
            var deskPos = new Vector3(-2.5f, 0, 0.4f);
            var deskGo = Prefab(SciFi + "SM_SciFiHoloScreen_DeskUnit.prefab", root, deskPos, 90f, 1.7f);
            if (!deskGo) deskGo = Part("Desk", sphere, deviceDark, root, deskPos + Vector3.up * 0.45f, new Vector3(0.5f, 0.45f, 0.9f));
            ch.desk = deskGo.transform;
            var tools = new GameObject("Tool Replicas").transform; tools.SetParent(root, false); tools.localPosition = deskPos + new Vector3(0.15f, 1.2f, 0);
            var sc = Part("Scanner Replica", sphere, device, tools, new Vector3(0, 0, -0.18f), new Vector3(0.06f, 0.045f, 0.17f));
            Part("Scanner Lens", sphere, glow, sc.transform, new Vector3(0, 0, 1f), Vector3.one * 0.35f);
            var bt = Part("BioTool Replica", sphere, device, tools, new Vector3(0, 0, 0.18f), new Vector3(0.06f, 0.05f, 0.2f));
            Part("BioTool Coil", torus, glow, bt.transform, new Vector3(0, 0, 0.85f), Vector3.one * 0.7f);
            Part("Pad", torusThick, glow, tools, new Vector3(0, -0.22f, 0), Vector3.one * 0.32f, new Vector3(90, 0, 0));
            ch.toolReplicas = tools;
            // the patient's hologram in the middle: a body outline with the heart and the main vessels
            var map = new GameObject("Patient Hologram").transform; map.SetParent(root, false); map.localPosition = new Vector3(0, 1.35f, 1.4f);
            var bodyGlow = Mat("Lab_Patient", "BVR2/Glow"); bodyGlow.SetColor("_Color", new Color(0.35f, 0.75f, 0.95f)); bodyGlow.SetFloat("_Intensity", 0.7f); bodyGlow.SetFloat("_Core", 0.15f); bodyGlow.SetFloat("_Fresnel", 2.5f);
            Part("Body", sphere, bodyGlow, map, new Vector3(0, 0, 0), new Vector3(0.24f, 0.5f, 0.13f));
            Part("Head", sphere, bodyGlow, map, new Vector3(0, 0.62f, 0), Vector3.one * 0.12f);
            Part("Arm L", sphere, bodyGlow, map, new Vector3(-0.32f, 0.05f, 0), new Vector3(0.06f, 0.42f, 0.06f), new Vector3(0, 0, -12));
            Part("Arm R", sphere, bodyGlow, map, new Vector3(0.32f, 0.05f, 0), new Vector3(0.06f, 0.42f, 0.06f), new Vector3(0, 0, 12));
            Part("Leg L", sphere, bodyGlow, map, new Vector3(-0.11f, -0.78f, 0), new Vector3(0.08f, 0.45f, 0.08f));
            Part("Leg R", sphere, bodyGlow, map, new Vector3(0.11f, -0.78f, 0), new Vector3(0.08f, 0.45f, 0.08f));
            Part("Base", torusThick, glow, map, new Vector3(0, -1.2f, 0), Vector3.one * 0.45f, new Vector3(90, 0, 0));
            var heartMat = Mat("Lab_Heart", "BVR2/Glow"); heartMat.SetColor("_Color", new Color(1f, 0.3f, 0.35f)); heartMat.SetFloat("_Intensity", 1.8f); heartMat.SetFloat("_Core", 0.9f);
            Part("Heart", sphere, heartMat, map, new Vector3(-0.04f, 0.2f, 0.03f), Vector3.one * 0.06f);
            Part("Aorta", torus, heartMat, map, new Vector3(0, 0.3f, 0.02f), Vector3.one * 0.07f);
            Part("Artery Arm", sphere, heartMat, map, new Vector3(0.33f, 0.02f, 0.03f), new Vector3(0.012f, 0.38f, 0.012f), new Vector3(0, 0, 12));
            var entry = Part("Entry Marker", torus, Mat("Lab_Entry", "BVR2/Glow"), map, new Vector3(0.38f, -0.2f, 0.05f), Vector3.one * 0.06f);
            entry.GetComponent<Renderer>().sharedMaterial.SetColor("_Color", Bvr2Style.Knowledge); entry.GetComponent<Renderer>().sharedMaterial.SetFloat("_Intensity", 2f);
            entry.AddComponent<Bvr2Spin>().speed = new Vector3(0, 90, 0);
            map.gameObject.AddComponent<SphereCollider>().radius = 0.75f;
            var ps = map.gameObject.AddComponent<Bvr2Scannable>(); ps.codexId = "internal"; ps.displayName = "Пациент"; ps.scanSeconds = 1.0f; ch.patientScan = ps;
            ch.patientMap = map;
            // vitals monitor
            var monPos = new Vector3(2.6f, 0, 0.9f);
            var monGo = Prefab(SciFi + "SM_SciFiHoloScreen_StandUnit.prefab", root, monPos, -90f, 1.9f);
            if (!monGo) monGo = Part("Monitor Stand", sphere, deviceDark, root, monPos + Vector3.up * 0.7f, new Vector3(0.15f, 0.7f, 0.15f));
            ch.monitor = monGo.transform;
            var monCanvas = Bvr2UiEditor.Canvas("Vitals Screen", root, new Vector2(620, 420), 0.0024f);
            monCanvas.localPosition = monPos + new Vector3(-0.5f, 2.0f, 0.1f); monCanvas.localRotation = Quaternion.Euler(0, 70f, 0);
            Bvr2UiEditor.MakeImage(monCanvas, new Color(0.02f, 0.06f, 0.09f, 0.85f), null, null);
            var mt = Bvr2UiEditor.MakeText(monCanvas, "ПАЦИЕНТ · ПОКАЗАТЕЛИ", 44, Bvr2Style.Ally, font, null); mt.alignment = TextAnchor.MiddleLeft; mt.rectTransform.offsetMin = new Vector2(34, 20); mt.rectTransform.offsetMax = new Vector2(-20, -20);
            ch.monitorText = mt;
            // big holo screen behind the patient (ambience), if the asset is there
            Prefab(SciFi + "SciFiHoloScreen_Large_01.prefab", root, new Vector3(0, 1.4f, 6.2f), 180f, 3.2f);
            // the shrinking capsule
            var cap = new GameObject("Shrink Capsule").transform; cap.SetParent(root, false); cap.localPosition = new Vector3(-1.9f, 0, 3.5f); ch.capsule = cap;
            Part("Pad", sphere, glow, cap, new Vector3(0, 0.04f, 0), new Vector3(0.95f, 0.03f, 0.95f));
            var rings = new GameObject("Rings").transform; rings.SetParent(cap, false); ch.capsuleRings = rings;
            Part("Ring Low", torusThick, glow, rings, new Vector3(0, 0.35f, 0), Vector3.one * 0.95f, new Vector3(90, 0, 0));
            Part("Ring High", torusThick, glow, rings, new Vector3(0, 2.3f, 0), Vector3.one * 0.95f, new Vector3(90, 0, 0));
            Part("Ring Tilted", torus, glow, rings, new Vector3(0, 1.3f, 0), Vector3.one * 1.05f, new Vector3(70, 0, 0));
            // a light only for the lab (the asset props use URP Lit); off in the artery
            var lg = new GameObject("Lab Light"); lg.transform.SetParent(root, false); lg.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = lg.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.1f; light.color = new Color(1f, 0.97f, 0.92f); light.shadows = LightShadows.None; light.enabled = false;
            ch.labLight = light;
            root.gameObject.SetActive(false);
        }

        static Bvr2Virus HedgehogTemplate(Mesh mesh, Material mat, Mesh antibodyMesh, Material antibodyMat, Transform parent)
        {
            var go = new GameObject("Hedgehog Template"); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
            go.AddComponent<SphereCollider>().radius = 1.2f;
            var sc = go.AddComponent<Bvr2Scannable>(); sc.codexId = "virus"; sc.displayName = "Вирион-ёжик"; sc.scanSeconds = 0.8f; sc.highlight = r;
            var abs = new GameObject("Antibodies").transform; abs.SetParent(go.transform, false);
            var dirs = Bvr2Meshes.IcoSphere(0, 0, 1, 1, "tmp").vertices;
            for (int i = 0; i < 10 && i < dirs.Length; i++)
            {
                var d = dirs[i].normalized;
                var y = Part("Antibody", antibodyMesh, antibodyMat, abs, d * 1.45f, Vector3.one * 0.4f);
                y.transform.localRotation = Quaternion.FromToRotation(Vector3.up, -d);
                y.SetActive(false);
            }
            var v = go.AddComponent<Bvr2Virus>(); v.body = r; v.antibodies = abs; v.radius = 0.5f; v.armored = true;
            go.SetActive(false);
            return v;
        }

        static Bvr2WallCell WallCell(string name, Mesh sphere, Material mat, Material flagMat, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var body = Part("Body", sphere, mat, go.transform, Vector3.zero, Vector3.one);
            var flags = new GameObject("Flags").transform; flags.SetParent(go.transform, false);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                var f = new GameObject("Flag " + i).transform; f.SetParent(flags, false); f.localPosition = new Vector3(Mathf.Cos(a) * 0.45f, 0.82f, Mathf.Sin(a) * 0.45f);
                Part("Stalk", sphere, flagMat, f, new Vector3(0, 0.18f, 0), new Vector3(0.03f, 0.2f, 0.03f));
                Part("Antigen", sphere, flagMat, f, new Vector3(0, 0.4f, 0), new Vector3(0.11f, 0.08f, 0.11f));
            }
            go.AddComponent<SphereCollider>().radius = 1f;
            var sc = go.AddComponent<Bvr2Scannable>(); sc.codexId = "infected"; sc.displayName = "Клетка стенки"; sc.scanSeconds = 1.0f; sc.highlight = body.GetComponent<Renderer>();
            var c = go.AddComponent<Bvr2WallCell>(); c.body = body.GetComponent<Renderer>(); c.flags = flags;
            go.SetActive(false);
            return c;
        }

        static Bvr2Virus DebrisTemplate(Mesh mesh, Material mat, Transform parent)
        {
            var go = new GameObject("Debris Template"); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off;
            var v = go.AddComponent<Bvr2Virus>(); v.body = r; v.radius = 0.55f; v.debris = true;
            go.SetActive(false);
            return v;
        }

        static Transform SpotTemplate(Mesh sphere, Mesh torus, Material spot, Material glow, Transform parent)
        {
            var go = new GameObject("Infected Spot Template"); go.transform.SetParent(parent, false);
            Part("Blister", sphere, spot, go.transform, Vector3.zero, new Vector3(0.6f, 0.6f, 0.22f));
            var ring = Part("Halo", torus, glow, go.transform, new Vector3(0, 0, 0.08f), Vector3.one * 0.75f);
            go.SetActive(false);
            return go.transform;
        }

        static void VirusExhibit(Bvr2Chapter3 ch, Mesh halfA, Mesh halfB, Material mat, Mesh sphere, Mesh torus, Material rnaMat, Material marker, Transform parent)
        {
            var root = new GameObject("Virus Exhibit"); root.transform.SetParent(parent, false);
            var a = Part("Capsid Top", halfA, mat, root.transform, Vector3.zero, Vector3.one * 0.3f);
            var b = Part("Capsid Bottom", halfB, mat, root.transform, Vector3.zero, Vector3.one * 0.3f);
            var rna = new GameObject("Genetic Program").transform; rna.SetParent(root.transform, false);
            for (int i = 0; i < 18; i++)
            {
                float u = i / 18f * Mathf.PI * 4f;
                Part("RNA", sphere, rnaMat, rna, new Vector3(Mathf.Cos(u) * 0.09f, (i / 17f - 0.5f) * 0.3f, Mathf.Sin(u) * 0.09f), Vector3.one * 0.028f);
            }
            // the spike to scan: on top of the capsid, ringed so it is easy to find
            var spike = new GameObject("Antigen Spike"); spike.transform.SetParent(a.transform, false); spike.transform.localPosition = new Vector3(0, 1.02f, 0);
            spike.AddComponent<SphereCollider>().radius = 0.55f;
            var sc = spike.AddComponent<Bvr2Scannable>(); sc.codexId = "antigen"; sc.displayName = "Шип оболочки"; sc.scanSeconds = 0.9f;
            Part("Marker", torus, marker, spike.transform, Vector3.zero, Vector3.one * 0.3f, new Vector3(90, 0, 0));
            ch.virusExhibit = root.transform; ch.halfA = a.transform; ch.halfB = b.transform; ch.rna = rna; ch.antigenSpike = sc;
        }

        static Transform AnchorPad(Mesh sphere, Mesh torus, Material glow, Material device, Transform parent)
        {
            var go = new GameObject("Anchor Beacon"); go.transform.SetParent(parent, false);
            Part("Core", sphere, glow, go.transform, Vector3.zero, Vector3.one * 0.12f);
            Part("Shell", sphere, device, go.transform, Vector3.zero, Vector3.one * 0.08f);
            Part("Halo A", torus, glow, go.transform, Vector3.zero, Vector3.one * 0.28f, new Vector3(0, 0, 0));
            Part("Halo B", torus, glow, go.transform, Vector3.zero, Vector3.one * 0.38f, new Vector3(90, 0, 0));
            go.AddComponent<Bvr2Spin>().speed = new Vector3(0, 40, 15);
            return go.transform;
        }

        static Transform Exhibit(float R, Material intimaMat, Material mediaMat, Material advMat, Transform parent, out Bvr2Scannable intima, out Bvr2Scannable media, out Bvr2Scannable adventitia)
        {
            var root = new GameObject("Wall Layers Exhibit"); root.transform.SetParent(parent, false);
            // staircase cut-away: intima shortest (nearest), media longer, adventitia the full window.
            // a small exploded slice (≈ 0.7 m), shown in front of the anchored player
            intima = Layer("Внутренняя оболочка", "intima", 0.60f, 0.645f, 0.24f, -0.23f, intimaMat, root.transform);
            media = Layer("Средняя оболочка", "media", 0.66f, 0.83f, 0.47f, -0.115f, mediaMat, root.transform);
            adventitia = Layer("Наружная оболочка", "adventitia", 0.845f, 0.97f, 0.70f, 0f, advMat, root.transform);
            return root.transform;
        }

        static Bvr2Scannable Layer(string title, string id, float r0, float r1, float length, float zCentre, Material mat, Transform parent)
        {
            var mesh = SaveMesh(Bvr2Meshes.Shell(r0, r1, length, -32, 32, 28, "BVR2_Layer_" + id), "Layer_" + id);
            var go = GlowObject(title, mesh, mat, parent); go.transform.localPosition = new Vector3(0, 0, zCentre);
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            var s = go.AddComponent<Bvr2Scannable>(); s.codexId = id; s.displayName = title; s.scanSeconds = 1.0f; s.highlight = go.GetComponent<Renderer>();
            return s;
        }

        /// <summary>
        /// Prologue in the SecVarLab room (from SecVarLab_Full.unitypackage): main lab with the workbench, the
        /// ceiling projector and the monitor wall; the shrinking capsule stands in the side bay behind the left door.
        /// Coordinates are labRoot-local with the main room centred on the origin, +z towards the round window.
        /// </summary>
        static void BuildSecVarLab(Bvr2Prelude ch, GameObject labPrefab, Mesh sphere, Mesh torus, Mesh torusThick, Material device, Material glow, Font font)
        {
            var root = new GameObject("Lab").transform; root.position = new Vector3(0, 600f, 0); ch.labRoot = root;
            var room = (GameObject)PrefabUtility.InstantiatePrefab(labPrefab, root);
            room.transform.localPosition = -SecVarMainRoom; room.transform.localRotation = Quaternion.identity;
            Vector3 P(float x, float y, float z) => new Vector3(x, y, z) - SecVarMainRoom; // prefab (floor-centred) → lab
            ch.startLocal = new Vector3(0.3f, 1.55f, -2.7f);
            ch.walkable = new[]
            {
                Rect.MinMaxRect(-2.6f, -3.45f, 2.0f, 1.8f),   // main lab, up to the workbench
                Rect.MinMaxRect(-3.55f, 1.35f, -2.4f, 2.45f), // door to the capsule bay
                Rect.MinMaxRect(-6.55f, 0.95f, -3.3f, 3.15f), // capsule bay
            };
            // the instruments float above the orange workbench (robot arms)
            var bench = new GameObject("Workbench").transform; bench.SetParent(root, false); bench.localPosition = P(-1.44f, 0.3f, 2.1f);
            ch.desk = bench;
            var tools = new GameObject("Tool Replicas").transform; tools.SetParent(root, false); tools.localPosition = new Vector3(-0.1f, 1.2f, 2.3f); // right end of the bench, not behind the hologram
            var sc = Part("Scanner Replica", sphere, device, tools, new Vector3(0, 0, -0.18f), new Vector3(0.06f, 0.045f, 0.17f));
            Part("Scanner Lens", sphere, glow, sc.transform, new Vector3(0, 0, 1f), Vector3.one * 0.35f);
            var bt = Part("BioTool Replica", sphere, device, tools, new Vector3(0, 0, 0.18f), new Vector3(0.06f, 0.05f, 0.2f));
            Part("BioTool Coil", torus, glow, bt.transform, new Vector3(0, 0, 0.85f), Vector3.one * 0.7f);
            Part("Pad", torusThick, glow, tools, new Vector3(0, -0.22f, 0), Vector3.one * 0.32f, new Vector3(90, 0, 0));
            ch.toolReplicas = tools;
            // the patient hologram under the ceiling projector
            var map = new GameObject("Patient Hologram").transform; map.SetParent(root, false); map.localPosition = P(-1.15f, 1.35f, 0.33f);
            BuildPatient(ch, map, sphere, torus, torusThick, glow);
            // vitals: a holo panel to the left of the patient, facing the start
            var mon = new GameObject("Monitor").transform; mon.SetParent(root, false); mon.localPosition = new Vector3(-1.6f, -0.2f, 0.7f); ch.monitor = mon;
            var monCanvas = Bvr2UiEditor.Canvas("Vitals Screen", root, new Vector2(620, 420), 0.0017f);
            monCanvas.localPosition = new Vector3(-1.6f, 1.45f, 0.7f); monCanvas.localRotation = Quaternion.Euler(0, -28f, 0); // left of the patient, the professor stands on the right
            Bvr2UiEditor.MakeImage(monCanvas, new Color(0.02f, 0.06f, 0.09f, 0.88f), null, null);
            var mt = Bvr2UiEditor.MakeText(monCanvas, "ПАЦИЕНТ · ПОКАЗАТЕЛИ", 44, Bvr2Style.Ally, font, null); mt.alignment = TextAnchor.MiddleLeft; mt.rectTransform.offsetMin = new Vector2(34, 20); mt.rectTransform.offsetMax = new Vector2(-20, -20);
            ch.monitorText = mt;
            // the shrinking capsule: the bay's glass cylinder + our pad and rings inside it
            var spot = room.transform.Find("Markers/CapsuleSpot");
            var cap = new GameObject("Shrink Capsule").transform; cap.SetParent(root, false);
            cap.localPosition = spot ? root.InverseTransformPoint(spot.position) : P(-5.52f, 0, 1.53f); ch.capsule = cap;
            Part("Pad", sphere, glow, cap, new Vector3(0, 0.36f, 0), new Vector3(0.8f, 0.02f, 0.8f));
            var rings = new GameObject("Rings").transform; rings.SetParent(cap, false); ch.capsuleRings = rings;
            Part("Ring Low", torusThick, glow, rings, new Vector3(0, 0.55f, 0), Vector3.one * 0.78f, new Vector3(90, 0, 0));
            Part("Ring High", torusThick, glow, rings, new Vector3(0, 2.15f, 0), Vector3.one * 0.78f, new Vector3(90, 0, 0));
            Part("Ring Tilted", torus, glow, rings, new Vector3(0, 1.35f, 0), Vector3.one * 0.86f, new Vector3(70, 0, 0));
            // where the professor stands (full height) during the prologue
            Transform Spot(string n, Vector3 pos) { var t = new GameObject(n).transform; t.SetParent(root, false); t.localPosition = pos; return t; }
            ch.profIntro = Spot("Prof Spot · patient", new Vector3(0.4f, 0, 0.05f));
            ch.profTools = Spot("Prof Spot · bench", new Vector3(0.95f, 0, 1.55f));
            ch.profCapsule = Spot("Prof Spot · capsule door", new Vector3(-2.15f, 0, 0.85f));
            ch.labLight = null; // the room is lit in its shader (Bvr2LabLighting), no realtime light
            root.gameObject.SetActive(false);
        }

        static void BuildPatient(Bvr2Prelude ch, Transform map, Mesh sphere, Mesh torus, Mesh torusThick, Material glow)
        {
            var bodyGlow = Mat("Lab_Patient", "BVR2/Glow"); bodyGlow.SetColor("_Color", new Color(0.35f, 0.75f, 0.95f)); bodyGlow.SetFloat("_Intensity", 0.7f); bodyGlow.SetFloat("_Core", 0.15f); bodyGlow.SetFloat("_Fresnel", 2.5f);
            Part("Body", sphere, bodyGlow, map, new Vector3(0, 0, 0), new Vector3(0.24f, 0.5f, 0.13f));
            Part("Head", sphere, bodyGlow, map, new Vector3(0, 0.62f, 0), Vector3.one * 0.12f);
            Part("Arm L", sphere, bodyGlow, map, new Vector3(-0.32f, 0.05f, 0), new Vector3(0.06f, 0.42f, 0.06f), new Vector3(0, 0, -12));
            Part("Arm R", sphere, bodyGlow, map, new Vector3(0.32f, 0.05f, 0), new Vector3(0.06f, 0.42f, 0.06f), new Vector3(0, 0, 12));
            Part("Leg L", sphere, bodyGlow, map, new Vector3(-0.11f, -0.78f, 0), new Vector3(0.08f, 0.45f, 0.08f));
            Part("Leg R", sphere, bodyGlow, map, new Vector3(0.11f, -0.78f, 0), new Vector3(0.08f, 0.45f, 0.08f));
            Part("Base", torusThick, glow, map, new Vector3(0, -1.2f, 0), Vector3.one * 0.45f, new Vector3(90, 0, 0));
            Part("Beam", sphere, bodyGlow, map, new Vector3(0, 1.2f, 0), new Vector3(0.025f, 0.6f, 0.025f));
            var heartMat = Mat("Lab_Heart", "BVR2/Glow"); heartMat.SetColor("_Color", new Color(1f, 0.3f, 0.35f)); heartMat.SetFloat("_Intensity", 1.8f); heartMat.SetFloat("_Core", 0.9f);
            Part("Heart", sphere, heartMat, map, new Vector3(-0.04f, 0.2f, 0.03f), Vector3.one * 0.06f);
            Part("Aorta", torus, heartMat, map, new Vector3(0, 0.3f, 0.02f), Vector3.one * 0.07f);
            Part("Artery Arm", sphere, heartMat, map, new Vector3(0.33f, 0.02f, 0.03f), new Vector3(0.012f, 0.38f, 0.012f), new Vector3(0, 0, 12));
            var entry = Part("Entry Marker", torus, Mat("Lab_Entry", "BVR2/Glow"), map, new Vector3(0.38f, -0.2f, 0.05f), Vector3.one * 0.06f);
            entry.GetComponent<Renderer>().sharedMaterial.SetColor("_Color", Bvr2Style.Knowledge); entry.GetComponent<Renderer>().sharedMaterial.SetFloat("_Intensity", 2f);
            entry.AddComponent<Bvr2Spin>().speed = new Vector3(0, 90, 0);
            map.gameObject.AddComponent<SphereCollider>().radius = 0.75f;
            var ps = map.gameObject.AddComponent<Bvr2Scannable>(); ps.codexId = "internal"; ps.displayName = "Пациент"; ps.scanSeconds = 1.0f; ch.patientScan = ps;
            ch.patientMap = map;
        }

        static Bvr2Scanner BuildScanner(Bvr2Rig rig, Material device, Material dark, Material glow, Material coneMat, Mesh cone, Mesh torus, Mesh sphere, Sprite ring, Material overlay, Font font)
        {
            var root = new GameObject("Scanner"); root.transform.SetParent(rig.leftHand, false);
            Part("Grip", toolBody, dark, root.transform, new Vector3(0, -0.026f, -0.03f), new Vector3(0.015f, 0.03f, 0.022f), new Vector3(-20, 0, 0));
            Part("Body", toolBody, device, root.transform, new Vector3(0, 0, 0.02f), new Vector3(0.021f, 0.016f, 0.06f));
            Part("Stripe", toolBody, glow, root.transform, new Vector3(0, 0.009f, 0.02f), new Vector3(0.006f, 0.009f, 0.05f));
            Part("Lens Ring", torus, glow, root.transform, new Vector3(0, 0, 0.078f), Vector3.one * 0.017f);
            Part("Lens", sphere, glow, root.transform, new Vector3(0, 0, 0.076f), Vector3.one * 0.008f);
            var tip = new GameObject("Tip").transform; tip.SetParent(root.transform, false); tip.localPosition = new Vector3(0, 0, 0.082f);
            var scanner = root.AddComponent<Bvr2Scanner>(); scanner.rig = rig; scanner.tip = tip;
            var coneGo = new GameObject("Scan Cone"); coneGo.transform.SetParent(rig.transform, false);
            coneGo.AddComponent<MeshFilter>().sharedMesh = cone; var cr = coneGo.AddComponent<MeshRenderer>(); cr.sharedMaterial = coneMat; cr.shadowCastingMode = ShadowCastingMode.Off; cr.enabled = false;
            scanner.cone = cr;
            // little screen on top of the scanner
            var canvas = Bvr2UiEditor.Canvas("Scanner Screen", root.transform, new Vector2(360, 200), 0.00018f);
            canvas.localPosition = new Vector3(0, 0.019f, 0.0f); canvas.localRotation = Quaternion.Euler(70, 0, 0);
            var back = Bvr2UiEditor.MakeImage(canvas, new Color(0.02f, 0.05f, 0.08f, 0.9f), null, overlay);
            var ringImg = Bvr2UiEditor.MakeImage(canvas, Bvr2Style.Knowledge, ring, overlay); var rr = ringImg.rectTransform; rr.anchorMin = rr.anchorMax = new Vector2(0, 0.5f); rr.sizeDelta = new Vector2(150, 150); rr.anchoredPosition = new Vector2(95, 0);
            ringImg.type = Image.Type.Filled; ringImg.fillMethod = Image.FillMethod.Radial360; ringImg.fillAmount = 0; ringImg.fillOrigin = (int)Image.Origin360.Top;
            var text = Bvr2UiEditor.MakeText(canvas, "СКАНЕР", 34, Bvr2Style.Text, font, overlay); text.rectTransform.offsetMin = new Vector2(180, 0); text.alignment = TextAnchor.MiddleLeft;
            scanner.ring = ringImg; scanner.screenText = text;
            return scanner;
        }

        static Bvr2BioTool BuildBioTool(Bvr2Rig rig, Material device, Material dark, Material glow, Mesh torus, Mesh sphere, Material overlay, Font font, out Text modeText, out Renderer[] coils)
        {
            var root = new GameObject("BioTool"); root.transform.SetParent(rig.rightHand, false);
            Part("Grip", toolBody, dark, root.transform, new Vector3(0, -0.026f, -0.03f), new Vector3(0.015f, 0.03f, 0.022f), new Vector3(-20, 0, 0));
            Part("Barrel", toolBody, device, root.transform, new Vector3(0, 0, 0.02f), new Vector3(0.018f, 0.018f, 0.065f));
            var ca = Part("Coil A", torus, glow, root.transform, new Vector3(0, 0, 0.04f), Vector3.one * 0.02f);
            var cb = Part("Coil B", torus, glow, root.transform, new Vector3(0, 0, 0.065f), Vector3.one * 0.016f);
            coils = new[] { ca.GetComponent<Renderer>(), cb.GetComponent<Renderer>() };
            var emitter = Part("Emitter", sphere, glow, root.transform, new Vector3(0, 0, 0.09f), Vector3.one * 0.01f);
            var tip = new GameObject("Tip").transform; tip.SetParent(root.transform, false); tip.localPosition = new Vector3(0, 0, 0.1f);
            var bio = root.AddComponent<Bvr2BioTool>(); bio.rig = rig; bio.tip = tip; bio.emitterGlow = emitter.GetComponent<Renderer>();
            // little screen with the active ability
            var canvas = Bvr2UiEditor.Canvas("BioTool Screen", root.transform, new Vector2(360, 110), 0.00018f);
            canvas.localPosition = new Vector3(0, 0.02f, 0.005f); canvas.localRotation = Quaternion.Euler(70, 0, 0);
            Bvr2UiEditor.MakeImage(canvas, new Color(0.02f, 0.05f, 0.08f, 0.9f), null, overlay);
            modeText = Bvr2UiEditor.MakeText(canvas, "ПРИТЯЖЕНИЕ", 40, Bvr2Style.Ally, font, overlay); modeText.alignment = TextAnchor.MiddleCenter;
            return bio;
        }

        // ---------- professor ----------
        static Bvr2Professor BuildProfessor(Bvr2Rig rig, Bvr2Hud hud)
        {
            string fbx = Root + "/Art/Professor/Professor.fbx";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var root = new GameObject("Professor (hologram)");
            var prof = root.AddComponent<Bvr2Professor>(); prof.rig = rig; prof.hud = hud;
            if (!asset) { Debug.LogWarning("[BVR2] Professor.fbx not found"); return prof; }
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset); PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Model"; model.transform.SetParent(root.transform, false);
            // normalise orientation (tallest axis up), height 1.8, face +Z
            var b = Bounds(model);
            if (b.size.z > b.size.y * 1.3f) model.transform.localRotation = Quaternion.Euler(-90, 0, 0) * model.transform.localRotation;
            b = Bounds(model);
            if (b.size.y > 0.01f) model.transform.localScale *= 1.8f / b.size.y;
            b = Bounds(model);
            var head = Find(model.transform, "Head"); var glasses = Find(model.transform, "Glasses");
            if (head && glasses)
            {
                var hb = head.GetComponent<Renderer>().bounds.center; var gb = glasses.GetComponent<Renderer>().bounds.center;
                var f = Vector3.ProjectOnPlane(gb - hb, Vector3.up);
                if (f.sqrMagnitude > 1e-6f) model.transform.rotation = Quaternion.FromToRotation(f.normalized, Vector3.forward) * model.transform.rotation;
            }
            b = Bounds(model);
            model.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
            // meshes with blend shapes (face, beard, moustache) need a SkinnedMeshRenderer to morph
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh; if (!mesh || mesh.blendShapeCount == 0) continue;
                var go = mf.gameObject; var mr = go.GetComponent<MeshRenderer>(); var mats = mr ? mr.sharedMaterials : new Material[0];
                if (mr) Object.DestroyImmediate(mr); Object.DestroyImmediate(mf);
                var smr = go.AddComponent<SkinnedMeshRenderer>(); smr.sharedMesh = mesh; smr.sharedMaterials = mats; smr.rootBone = go.transform;
                var lb = mesh.bounds; lb.Expand(0.06f); smr.localBounds = lb; smr.updateWhenOffscreen = false;
            }
            // head pivot at the neck, carrying the face parts
            var pivot = new GameObject("HeadPivot").transform; pivot.SetParent(model.transform, false);
            pivot.position = head ? head.position : model.transform.position + Vector3.up * 1.5f;
            foreach (var n in new[] { "Head", "Hair", "HairCap", "Glasses", "Mustache", "Beard" }) { var t = Find(model.transform, n); if (t) t.SetParent(pivot, true); }
            // materials: hologram with the original colours
            string tex = Root + "/Art/Professor/Textures/";
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.gameObject.name; string key = n == "ShoeR" ? "ShoeL" : n == "HandR" ? "HandL" : n;
                var mat = Mat("Prof_" + key, "BVR2/Hologram");
                var t2 = AssetDatabase.LoadAssetAtPath<Texture2D>(tex + "Prof_" + key + "_BaseColor.png");
                mat.SetTexture("_MainTex", t2); mat.SetFloat("_Holo", 0.32f); mat.SetFloat("_Alpha", 0.93f);
                mat.SetColor("_Tint", key.StartsWith("Hand") ? new Color(0.93f, 0.6f, 0.43f) : Color.white); // hands have no texture: the face's skin tone
                int subs = 1;
                if (r is SkinnedMeshRenderer smr0) { if (smr0.sharedMesh) subs = smr0.sharedMesh.subMeshCount; }
                else { var mf0 = r.GetComponent<MeshFilter>(); if (mf0 && mf0.sharedMesh) subs = mf0.sharedMesh.subMeshCount; }
                if (n == "Head" && subs >= 3)
                {
                    // head + mouth cavity + teeth (one mesh, so the jaw shape moves the lower teeth too)
                    var mouth = Mat("Prof_Mouth", "BVR2/Hologram"); mouth.SetTexture("_MainTex", Texture2D.whiteTexture); mouth.SetColor("_Tint", new Color(0.2f, 0.05f, 0.06f)); mouth.SetFloat("_Holo", 0.15f); mouth.SetFloat("_Alpha", 0.97f); mouth.SetColor("_RimColor", new Color(0.05f, 0.02f, 0.02f));
                    var teeth = Mat("Prof_Teeth", "BVR2/Hologram"); teeth.SetTexture("_MainTex", Texture2D.whiteTexture); teeth.SetColor("_Tint", new Color(0.86f, 0.84f, 0.78f)); teeth.SetFloat("_Holo", 0.2f); teeth.SetFloat("_Alpha", 0.97f);
                    r.sharedMaterials = new[] { mat, mouth, teeth };
                }
                else r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            }
            prof.model = model.transform; prof.headPivot = pivot;
            prof.handL = Find(model.transform, "HandL"); prof.handR = Find(model.transform, "HandR"); prof.medallion = Find(model.transform, "Medallion");
            prof.face = head ? head.GetComponent<SkinnedMeshRenderer>() : null;
            prof.speechParts = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(m => m != prof.face && m.sharedMesh && m.sharedMesh.GetBlendShapeIndex("MouthOpen") >= 0).ToArray();
            // voice: the recorded lines + an audio source at the head
            if (File.Exists(Bvr2VoiceImport.VoiceDir + "/voice_manifest.json")) Debug.Log("[BVR2] " + Bvr2VoiceImport.Import());
            prof.voice = Bvr2VoiceImport.Load();
            var vo = new GameObject("Voice"); vo.transform.SetParent(pivot, false); vo.transform.localPosition = Vector3.up * 0.1f;
            var src = vo.AddComponent<AudioSource>(); src.playOnAwake = false; src.loop = false; src.spatialBlend = 0f; src.volume = 1f; src.priority = 32;
            src.rolloffMode = AudioRolloffMode.Linear; src.minDistance = 1.5f; src.maxDistance = 18f; src.dopplerLevel = 0;
            prof.voiceSource = src;
            root.transform.localScale = Vector3.one * prof.scale;
            return prof;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
        }

        static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t; return null;
        }

        // ---------- assets ----------
        static void ConfigureImports()
        {
            if (AssetImporter.GetAtPath(Root + "/Art/Professor/Professor.fbx") is ModelImporter mi)
            {
                bool dirty = mi.materialImportMode != ModelImporterMaterialImportMode.None || !mi.importBlendShapes || mi.animationType != ModelImporterAnimationType.None;
                mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.importBlendShapes = true; mi.animationType = ModelImporterAnimationType.None; mi.importAnimation = false;
                mi.isReadable = false; mi.meshCompression = ModelImporterMeshCompression.Off; mi.importNormals = ModelImporterNormals.Import;
                if (dirty) mi.SaveAndReimport();
            }
            foreach (var g in AssetDatabase.FindAssets("t:AudioClip", new[] { Root + "/Audio" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.Contains("/Voice/")) continue; // the professor's voice: Bvr2VoiceImport (compressed, streamed from memory)
                if (AssetImporter.GetAtPath(p) is AudioImporter ai)
                {
                    var s = ai.defaultSampleSettings; bool loop = p.Contains("Loop");
                    s.loadType = loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                    s.compressionFormat = loop ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM; s.quality = 0.6f;
                    ai.defaultSampleSettings = s; ai.forceToMono = true; ai.SaveAndReimport();
                }
            }
        }

        static AudioClip Clip(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);

        // Sci-Fi Hologram Screen Shader (Assets/ThirdParty) as a parallax grid behind boards; optional.
        const string HoloShader = "_MaxKill Studios/SciFiHoloScreen/URP/SciFiHoloScreen Additive URP";
        const string HoloGrid = "Assets/ThirdParty/SciFiHoloScreen-Shader/Textures/T_SciFiHoloScreen-GridTex_04.png";
        static Material HoloBackdropMaterial()
        {
            var sh = Shader.Find(HoloShader); var grid = AssetDatabase.LoadAssetAtPath<Texture2D>(HoloGrid);
            if (!sh || !grid) { Debug.Log("[BVR2] Sci-Fi Hologram Screen shader not found: boards without a backdrop"); return null; }
            var m = Mat("Holo_Backdrop", HoloShader);
            m.SetTexture("_BackdropTexture", grid);
            m.SetColor("_BackdropColor", new Color(0.18f, 0.7f, 0.85f, 1f));
            m.SetFloat("_BackdropColorIntensity", 0.55f);
            m.SetFloat("_BackdropScaleOption", 1f); m.EnableKeyword("_BACKDROPSCALEOPTION_INDEPENDENT"); m.DisableKeyword("_BACKDROPSCALEOPTION_MATCHOBJECT");
            m.SetFloat("_BackdropTiling", 2.2f); m.SetFloat("_BackdropDepth", 1.6f);
            m.SetColor("_BackdropFogColor", new Color(0.02f, 0.12f, 0.16f, 1f)); m.SetFloat("_BackdropFogDensity", 0.55f);
            m.SetColor("_FrameTintColor", Color.black); m.SetColor("_HologramTintColor", Color.black);
            EditorUtility.SetDirty(m); return m;
        }

        static Material Mat(string name, string shader)
        {
            string path = $"{Gen}/Materials/{name}.mat";
            var sh = Shader.Find(shader);
            if (!sh) { Debug.LogError("[BVR2] Shader not found: " + shader); sh = Shader.Find("Universal Render Pipeline/Unlit"); }
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); }
            else if (m.shader != sh) m.shader = sh;
            EditorUtility.SetDirty(m); return m;
        }

        static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = $"{Gen}/Meshes/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { existing.Clear(); EditorUtility.CopySerialized(mesh, existing); existing.name = mesh.name; EditorUtility.SetDirty(existing); return existing; }
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        static Texture2D SaveTexture(Texture2D tex, string name, bool isNormal)
        {
            string path = $"{Gen}/Textures/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default; ti.sRGBTexture = false; ti.wrapMode = TextureWrapMode.Repeat;
            ti.mipmapEnabled = true; ti.anisoLevel = 4; ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Sprite SaveSprite(Texture2D tex, string name, Vector4 border)
        {
            string path = $"{Gen}/Textures/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.spriteBorder = border; ti.alphaIsTransparency = true; ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------- procedural textures ----------
        /// <summary>Endothelium: cells elongated along the flow (u), soft grooves, nuclei, folds, caustics.</summary>
        static Texture2D EndotheliumPattern(int size, out Texture2D normal)
        {
            var rnd = new System.Random(2610);
            int gu = 4, gv = 4; var pts = new Vector2[gu, gv];
            for (int i = 0; i < gu; i++) for (int j = 0; j < gv; j++) pts[i, j] = new Vector2((i + 0.2f + 0.6f * (float)rnd.NextDouble()) / gu, (j + 0.2f + 0.6f * (float)rnd.NextDouble()) / gv);
            var height = new float[size, size]; var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            var cols = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    float d1 = 9, d2 = 9; Vector2 nearest = Vector2.zero;
                    for (int i = -1; i <= gu; i++) for (int j = -1; j <= gv; j++)
                        {
                            var p = pts[(i + gu) % gu, (j + gv) % gv] + new Vector2(i < 0 ? -1 : i >= gu ? 1 : 0, j < 0 ? -1 : j >= gv ? 1 : 0);
                            var dd = new Vector2(u - p.x, v - p.y); float d = dd.magnitude;
                            if (d < d1) { d2 = d1; d1 = d; nearest = dd; } else if (d < d2) d2 = d;
                        }
                    float cell = 1f / gu;
                    float border = Mathf.SmoothStep(0, 1, Mathf.Clamp01((d2 - d1) / (0.12f * cell)));
                    float dome = Mathf.Sqrt(Mathf.Clamp01(1f - d1 / (0.75f * cell)));
                    float h = 0.5f + 0.1f * dome - 0.2f * (1f - border);
                    // nucleus: an oval bump elongated along u
                    float nu = (nearest.x / 0.22f) * (nearest.x / 0.22f) + (nearest.y / 0.1f) * (nearest.y / 0.1f);
                    float nuc = Mathf.Clamp01(1f - nu * gu * gu * 0.9f);
                    h += nuc * 0.1f;
                    float folds = 0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * (v * 3 + 0.12f * Mathf.Sin(2 * Mathf.PI * u)));
                    float caustic = Caustic(u, v);
                    height[x, y] = h + folds * 0.08f;
                    cols[y * size + x] = new Color(Mathf.Clamp01(h), Mathf.Clamp01(nuc), folds, caustic);
                }
            tex.SetPixels(cols); tex.Apply(true);
            normal = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            var ncol = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float hl = height[(x - 1 + size) % size, y], hr = height[(x + 1) % size, y], hd = height[x, (y - 1 + size) % size], hu = height[x, (y + 1) % size];
                    var n = new Vector3((hl - hr) * 2.2f, (hd - hu) * 2.2f, 1f).normalized;
                    ncol[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1);
                }
            normal.SetPixels(ncol); normal.Apply(true);
            return tex;
        }

        static float Caustic(float u, float v)
        {
            float a = 0;
            for (int o = 1; o <= 2; o++)
            {
                float f = 3 * o;
                float w = Mathf.Sin(2 * Mathf.PI * (u * f + 0.3f * Mathf.Sin(2 * Mathf.PI * v * o))) * Mathf.Sin(2 * Mathf.PI * (v * f + 0.3f * Mathf.Sin(2 * Mathf.PI * u * o)));
                a += Mathf.Pow(1f - Mathf.Abs(w), 4f) / o;
            }
            return Mathf.Clamp01(a * 0.8f);
        }

        static Texture2D RoundedRect(int size, float radius)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Max(radius - x - 0.5f, 0, x + 0.5f - (size - radius)); float py = Mathf.Max(radius - y - 0.5f, 0, y + 0.5f - (size - radius));
                    float d = Mathf.Sqrt(px * px + py * py); float a = Mathf.Clamp01(radius - d + 0.5f);
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply(); return t;
        }

        static Texture2D RingTex(int size, float inner)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float r = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).magnitude / (size / 2f);
                    float aa = 2f / size;
                    float a = Mathf.Clamp01((1f - r) / aa) * Mathf.Clamp01((r - inner) / aa);
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply(); return t;
        }

        static Texture2D Radial(int size, float hard, float soft)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float r = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).magnitude / (size / 2f);
                    float a = hard > 0 ? Mathf.Clamp01((1f - r) / (1f - hard)) : Mathf.Pow(Mathf.Clamp01(1f - r), 2f);
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply(); return t;
        }
    }

    /// <summary>Editor-side UI helpers (the runtime factory needs its static font set).</summary>
    static class Bvr2UiEditor
    {
        public static RectTransform Canvas(string name, Transform parent, Vector2 size, float scale)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var c = go.AddComponent<UnityEngine.Canvas>(); c.renderMode = RenderMode.WorldSpace; c.sortingOrder = 75;
            go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 3.2f;
            var rt = (RectTransform)go.transform; rt.sizeDelta = size; rt.localScale = Vector3.one * scale; return rt;
        }

        public static UnityEngine.UI.Image MakeImage(Transform parent, Color color, Sprite sprite, Material mat)
        {
            var go = new GameObject("Image", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var i = go.AddComponent<UnityEngine.UI.Image>(); i.color = color; i.sprite = sprite; i.material = mat; i.raycastTarget = false; return i;
        }

        public static UnityEngine.UI.Text MakeText(Transform parent, string text, int size, Color color, Font font, Material mat)
        {
            var go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var t = go.AddComponent<UnityEngine.UI.Text>(); t.text = text; t.fontSize = size; t.color = color; t.font = font; t.material = mat; t.raycastTarget = false; t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; return t;
        }
    }
}
