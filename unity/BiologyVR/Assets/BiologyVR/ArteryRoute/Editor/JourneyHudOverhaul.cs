using System;
using System.Linq;
using BiologyVR.ArteryRoute;
using BiologyVR.ArteryRoute.Journey;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace BiologyVR.ArteryRoute.Editor
{
    /// <summary>
    /// Idempotent scene-time UI authoring helper. The narrative builder can call Apply(hud)
    /// after creating the mission, and the resulting bindings are serialized into the scene.
    /// It never destroys objects and only hides the four canvases made by the old HUD builder.
    /// </summary>
    public static class JourneyHudOverhaul
    {
        const string RootName = "Journey HUD — Cyan Holographic Panel";
        const string MarkerName = "Journey Target Marker — visual only";
        const float HudWorldScale = .0016f;
        const float MarkerWorldScale = .0024f;
        static readonly Color Cyan = new Color(.10f, .91f, 1f, 1f);
        static readonly Color CyanSoft = new Color(.22f, .70f, .80f, 1f);
        static readonly Color Ink = new Color(.008f, .035f, .065f, .97f);
        static readonly Color InkLight = new Color(.018f, .10f, .14f, .98f);
        static readonly Color Dim = new Color(.46f, .70f, .75f, 1f);

        public static void Apply(JourneyHud hud)
        {
            if (!hud) throw new ArgumentNullException(nameof(hud));
            if (!hud.mission) throw new InvalidOperationException("JourneyHud.mission is required");
            var camera = hud.mission.mover ? hud.mission.mover.viewCamera : null;
            if (!camera) throw new InvalidOperationException("JourneyHud requires JourneyMover.viewCamera");

            HideKnownLegacyCanvases(camera);
            EnsureEventSystem();
            // Recreate only this generated HUD: cached Graphics from the old version may refer
            // to missing CanvasRenderers. Never reuse these malformed serialized components.
            var previousPanel=camera.transform.Find(RootName);if(previousPanel)UnityEngine.Object.DestroyImmediate(previousPanel.gameObject);
            var previousMarker=camera.transform.Find(MarkerName);if(previousMarker)UnityEngine.Object.DestroyImmediate(previousMarker.gameObject);
            var root = FindOrCreate(RootName, camera.transform);
            ConfigureCanvas(root, camera, true);
            var rootRect = root.GetComponent<RectTransform>();
            // Camera-local metres: keep the compact panel to the player's side rather than
            // floating above the hero. The canvas remains world-space for readable XR text.
            rootRect.localPosition = new Vector3(-.8f, -.18f, 1.8f);
            rootRect.localRotation = Quaternion.identity;
            rootRect.localScale = Vector3.one * HudWorldScale;
            hud.compactHeight = 724f; hud.experimentHeight = 996f;
            rootRect.sizeDelta = new Vector2(640, hud.compactHeight);
            var group = GetOrAdd<CanvasGroup>(root.gameObject);
            group.interactable = group.blocksRaycasts = true;

            var flatBackground = Child(root.transform, "Panel opaque background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            flatBackground.color = new Color(.006f, .045f, .070f, .94f);flatBackground.raycastTarget=false;Stretch(flatBackground.rectTransform,rootRect,0,0);
            var bg = Rounded(root.transform, "Panel background", new Color(.006f, .045f, .070f, .20f), 22, 2);
            Stretch(bg.rectTransform, rootRect, 0, 0);
            var topLine = Rounded(root.transform, "Cyan edge", Cyan, 22, 0);
            topLine.color = new Color(Cyan.r, Cyan.g, Cyan.b, .72f);
            topLine.rectTransform.anchorMin = new Vector2(0, 1); topLine.rectTransform.anchorMax = new Vector2(1, 1);
            topLine.rectTransform.pivot = new Vector2(.5f, 1); topLine.rectTransform.sizeDelta = new Vector2(0, 3); topLine.rectTransform.anchoredPosition = Vector2.zero;

            var header = Text(root.transform, "Station header", "BIOLOGY VR  /  ИССЛЕДОВАНИЕ", 18, Cyan, TextAnchor.MiddleLeft);
            Anchor(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -42), new Vector2(-20, -10));
            hud.title = header;
            var status = Text(root.transform, "Research status", "●  НАБЛЮДЕНИЕ АКТИВНО", 13, new Color(.38f, 1f, .74f), TextAnchor.MiddleRight);
            Anchor(status.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -42), new Vector2(-20, -10));

            var taskCard = Card(root.transform, "Current task", new Vector2(18, -60), new Vector2(-18, -224), InkLight);
            var taskTag = Text(taskCard.transform, "Task tag", "ТЕКУЩАЯ ЗАДАЧА", 12, Dim, TextAnchor.UpperLeft);
            Anchor(taskTag.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -34), new Vector2(-16, -12));
            hud.goal = Text(taskCard.transform, "Goal", "Подготовка…", 28, Color.white, TextAnchor.UpperLeft);
            Anchor(hud.goal.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -110), new Vector2(-16, -38));
            hud.progress = Text(taskCard.transform, "Progress", "Шаг 1 / 1", 14, Dim, TextAnchor.MiddleRight);
            Anchor(hud.progress.rectTransform, new Vector2(.65f, 0), new Vector2(1, 0), new Vector2(0, 12), new Vector2(-16, 42));
            var progressTrack = Rounded(taskCard.transform, "Progress track", new Color(.04f, .22f, .27f, 1), 5, 0);
            Anchor(progressTrack.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(16, 5), new Vector2(-16, 12));
            // Migrate the previous sibling fill instead of leaving a duplicate behind on re-apply.
            var oldProgressFill = taskCard.transform.Find("Progress fill");
            if (oldProgressFill) { oldProgressFill.SetParent(progressTrack.transform, false);GetOrAdd<CanvasRenderer>(oldProgressFill.gameObject);GetOrAdd<JourneyRoundedGraphic>(oldProgressFill.gameObject); }
            hud.progressFill = Rounded(progressTrack.transform, "Progress fill", Cyan, 5, 0).rectTransform;
            hud.progressFill.anchorMin = Vector2.zero; hud.progressFill.anchorMax = new Vector2(0, 1);
            hud.progressFill.pivot = Vector2.zero; hud.progressFill.offsetMin = hud.progressFill.offsetMax = Vector2.zero;
            progressTrack.raycastTarget = false; hud.progressFill.GetComponent<JourneyRoundedGraphic>().raycastTarget = false;

            var actionCard = Card(root.transform, "Focused action", new Vector2(18, -236), new Vector2(-18, -448), Ink);
            var hint = Text(actionCard.transform, "Tool hint", "BioTool: наведи и нажми E", 16, Dim, TextAnchor.UpperLeft);
            Anchor(hint.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -54), new Vector2(-16, -14));
            hud.toolHint = hint;
            hud.targetLabel = hud.targetLabel ? Reuse(hud.targetLabel, actionCard.transform, "Target") : Text(actionCard.transform, "Target", "ЦЕЛЬ • Объект исследования", 18, Cyan, TextAnchor.MiddleLeft);
            GetOrAdd<CanvasRenderer>(hud.targetLabel.gameObject);
            hud.targetLabel.fontSize = 18; hud.targetLabel.color = Cyan; hud.targetLabel.alignment = TextAnchor.MiddleLeft;
            hud.targetLabel.raycastTarget = false; hud.targetLabel.horizontalOverflow = HorizontalWrapMode.Wrap; hud.targetLabel.verticalOverflow = VerticalWrapMode.Truncate;
            Anchor(hud.targetLabel.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -92), new Vector2(-16, -60));
            hud.primaryButton = Button(actionCard.transform, "PRIMARY ACTION", new Vector2(.39f, 1), new Vector2(1, 1), new Vector2(6, -156), new Vector2(-16, -100), Cyan, Color.black);
            hud.primaryLabel = Text(hud.primaryButton.transform, "Caption", "Выполнить действие", 18, Color.white, TextAnchor.MiddleCenter);
            SetButtonCaption(hud.primaryButton, hud.primaryLabel);
            ResetPersistentListeners(hud.primaryButton.onClick); UnityEventTools.AddPersistentListener(hud.primaryButton.onClick, hud.ExecuteFocused);
            hud.scanButton = Button(actionCard.transform, "SCAN ACTION", new Vector2(0, 1), new Vector2(.39f, 1), new Vector2(16, -156), new Vector2(-6, -100), new Color(.025f, .22f, .27f, 1), Cyan);
            var scanCaption = Text(hud.scanButton.transform, "Caption", "Сканировать цель", 18, Cyan, TextAnchor.MiddleCenter);
            SetButtonCaption(hud.scanButton, scanCaption);
            ResetPersistentListeners(hud.scanButton.onClick); UnityEventTools.AddPersistentListener(hud.scanButton.onClick, hud.ScanFocused);
            hud.feedback = Text(actionCard.transform, "Feedback", "Система готова", 14, Dim, TextAnchor.MiddleLeft);
            Anchor(hud.feedback.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -200), new Vector2(-16, -164));

            var briefing = Card(root.transform, "Research briefing", new Vector2(18, -460), new Vector2(-18, -660), InkLight);
            hud.briefingRoot = briefing.gameObject;
            hud.info = Text(briefing.transform, "Briefing text", "", 16, new Color(.87f, .95f, .96f), TextAnchor.UpperLeft);
            Anchor(hud.info.rectTransform, Vector2.zero, Vector2.one, new Vector2(16, 16), new Vector2(-16, -16));
            hud.info.horizontalOverflow = HorizontalWrapMode.Wrap; hud.info.verticalOverflow = VerticalWrapMode.Truncate;
            var briefingButton = Button(root.transform, "BRIEFING TOGGLE", new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 11), new Vector2(154, 38), new Color(.025f, .16f, .20f, 1), Cyan);
            hud.briefingButtonLabel = Text(briefingButton.transform, "Caption", "Справка  [Tab]", 13, Cyan, TextAnchor.MiddleCenter);
            SetButtonCaption(briefingButton, hud.briefingButtonLabel);
            ResetPersistentListeners(briefingButton.onClick); UnityEventTools.AddPersistentListener(briefingButton.onClick, hud.ToggleBriefing);
            briefing.SetActive(false);

            var continueButton = Button(root.transform, "CONTINUE", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-190, 11), new Vector2(-18, 38), new Color(.025f, .16f, .20f, 1), Cyan);
            var continueCaption = Text(continueButton.transform, "Caption", "Продолжить  →", 13, Cyan, TextAnchor.MiddleCenter);
            SetButtonCaption(continueButton, continueCaption);
            ResetPersistentListeners(continueButton.onClick); UnityEventTools.AddPersistentListener(continueButton.onClick, hud.ContinueJourney); hud.continueButton = continueButton;

            var experiment = Card(root.transform, "Experiment module", new Vector2(18, -672), new Vector2(-18, -672 - JourneyHud.TwoRowExperimentHeight), InkLight);
            hud.experimentRoot = experiment.gameObject;
            var oldVitals=experiment.transform.Find("Vitals");if(oldVitals)oldVitals.SetParent(root.transform,false);
            hud.vitals = Text(root.transform, "Vitals", "АД 120/80", 15, Color.white, TextAnchor.UpperLeft);
            Anchor(hud.vitals.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -262), new Vector2(-26, -232));
            hud.experimentSummary = Text(experiment.transform, "Experiment summary", "", 13, Dim, TextAnchor.UpperLeft);
            Anchor(hud.experimentSummary.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -86), new Vector2(-16, -42));
            hud.radius = SliderRow(experiment.transform, "Radius row", "Радиус r / r₀", 0, .5f, 1.5f, 1, hud.mission.SetRadius, out hud.radiusValue);
            hud.pressure = SliderRow(experiment.transform, "Pressure row", "Модельное ΔP", 1, 60, 170, 120, hud.mission.SetPressure, out hud.pressureValue);
            hud.balance = SliderRow(experiment.transform, "Balance row", "Баланс гемостаза", 0, 0, 1, .3f, hud.mission.SetBalance, out hud.balanceValue);
            hud.radiusRow = hud.radius.transform.parent.gameObject; hud.pressureRow = hud.pressure.transform.parent.gameObject; hud.balanceRow = hud.balance.transform.parent.gameObject;
            SetSliderPersistent(hud.radius, hud.mission.SetRadius); SetSliderPersistent(hud.pressure, hud.mission.SetPressure); SetSliderPersistent(hud.balance, hud.mission.SetBalance);
            AddOrGet<CanvasGroup>(experiment.gameObject).blocksRaycasts = true;
            hud.panelRoot = rootRect; hud.panelGroup = group;

            var markerCanvas = FindOrCreate(MarkerName, camera.transform);
            ConfigureCanvas(markerCanvas, camera, false);
            var markerRect = markerCanvas.GetComponent<RectTransform>(); markerRect.localPosition = Vector3.zero; markerRect.localRotation = Quaternion.identity; markerRect.localScale = Vector3.one * MarkerWorldScale; markerRect.sizeDelta = new Vector2(260, 100);
            markerCanvas.GetComponent<Canvas>().sortingOrder = 20;
            var markerGroup = GetOrAdd<CanvasGroup>(markerCanvas.gameObject); markerGroup.blocksRaycasts = markerGroup.interactable = false;
            var markerVisual = Rounded(markerCanvas.transform, "Target ring", new Color(Cyan.r, Cyan.g, Cyan.b, .025f), 32, 2);
            markerVisual.raycastTarget = false; markerVisual.rectTransform.sizeDelta = new Vector2(82, 82); markerVisual.rectTransform.anchorMin = markerVisual.rectTransform.anchorMax = new Vector2(.5f, .5f); markerVisual.rectTransform.anchoredPosition = Vector2.zero;
            var marker = GetOrAdd<JourneyTargetMarker>(markerCanvas.gameObject); marker.hud = hud; marker.visual = markerVisual.rectTransform; marker.group = markerGroup;
            marker.caption = Text(markerCanvas.transform, "Target marker label", "", 13, Cyan, TextAnchor.MiddleCenter); marker.caption.raycastTarget = false;
            marker.caption.rectTransform.sizeDelta = new Vector2(250, 24); marker.caption.rectTransform.anchorMin = marker.caption.rectTransform.anchorMax = new Vector2(.5f, .5f); marker.caption.rectTransform.anchoredPosition = new Vector2(0, -58);
            hud.targetMarker = marker;

            var guard = GetOrAdd<JourneyUiInputGuard>(root.gameObject); var rigRoot = hud.mission.mover.origin ? hud.mission.mover.origin.gameObject : camera.gameObject; guard.hud = hud; guard.surfaces = new[] { rootRect }; guard.rays = rigRoot.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(true); guard.nearFarRays = rigRoot.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(true); hud.inputGuard = guard;
            // Keep both desktop and tracked-device UI paths available; marker has neither raycaster.
            EditorUtility.SetDirty(hud); EditorUtility.SetDirty(root); EditorUtility.SetDirty(markerCanvas);
            EditorSceneManager.MarkSceneDirty(root.scene);
            hud.Refresh();
        }

        static void HideKnownLegacyCanvases(Camera camera)
        {
            string[] names = { "Narrative chapter", "Current scenario actions", "Learning term and professor briefing", "Live measurements and experiments" };
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (names.Contains(canvas.gameObject.name)) canvas.gameObject.SetActive(false);
        }
        static GameObject FindOrCreate(string name, Transform parent)
        {
            var existing = parent.Find(name);
            if (existing) return existing.gameObject;
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas)); go.transform.SetParent(parent, false); return go;
        }
        static void ConfigureCanvas(GameObject go, Camera camera, bool interactive)
        {
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera; canvas.overrideSorting = true;canvas.sortingOrder=interactive?50:60;
            if (interactive)
            {
                GetOrAdd<GraphicRaycaster>(go);
                GetOrAdd<ArteryTrackedRaycaster>(go);
            }
        }
        static void EnsureEventSystem()
        {
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (!eventSystem) eventSystem = new GameObject("XR EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            if (!eventSystem.GetComponent<XRUIInputModule>()) eventSystem.gameObject.AddComponent<XRUIInputModule>();
        }
        static JourneyRoundedGraphic Rounded(Transform parent, string name, Color color, float radius, float border)
        {
            var go = Child(parent, name, typeof(RectTransform), typeof(CanvasRenderer), typeof(JourneyRoundedGraphic)); var graphic = go.GetComponent<JourneyRoundedGraphic>(); graphic.color = color; graphic.radius = radius; graphic.borderWidth = border; return graphic;
        }
        static GameObject Card(Transform parent, string name, Vector2 minMaxTop, Vector2 maxMaxBottom, Color color)
        {
            var card = Child(parent, name, typeof(RectTransform)); var rt = card.GetComponent<RectTransform>();
            // Arguments are expressed as top and bottom edges; RectTransform offsetMin is the bottom edge.
            // Set the pivot before the bounds so runtime height changes keep the top edge fixed.
            rt.pivot = new Vector2(.5f, 1);
            Anchor(rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(minMaxTop.x, maxMaxBottom.y), new Vector2(maxMaxBottom.x, minMaxTop.y));
            var graphic = Rounded(card.transform, "Card surface", color, 14, 1); Stretch(graphic.rectTransform, rt, 0, 0); return card;
        }
        static Text Text(Transform parent, string name, string value, int size, Color color, TextAnchor anchor)
        {
            var go = Child(parent, name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)); var text = go.GetComponent<Text>(); text.text = value; text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.color = color; text.alignment = anchor; text.raycastTarget = false; text.supportRichText = true; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }
        static Text Reuse(Text existing, Transform parent, string name) { if (existing) existing.transform.SetParent(parent, false); existing.name = name; return existing; }
        static Button Button(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, Color color, Color textColor)
        {
            var go = Child(parent, name, typeof(RectTransform), typeof(CanvasRenderer), typeof(JourneyRoundedGraphic), typeof(Button)); var rt = go.GetComponent<RectTransform>(); Anchor(rt, min, max, offsetMin, offsetMax); var graphic = go.GetComponent<JourneyRoundedGraphic>(); graphic.color = color; graphic.radius = 9; graphic.borderWidth = 1;var button = go.GetComponent<Button>(); button.targetGraphic = graphic; button.colors = new ColorBlock { normalColor = Color.white, highlightedColor = new Color(1f,1f,1f,1f), pressedColor = CyanSoft, selectedColor = Color.white, disabledColor = new Color(1,1,1,.28f), colorMultiplier = 1, fadeDuration = .08f }; return button;
        }
        static void SetButtonCaption(Button button, Text caption) { caption.transform.SetParent(button.transform, false); Stretch(caption.rectTransform, button.GetComponent<RectTransform>(), 0, 0); caption.color = Color.white; }
        static Slider SliderRow(Transform parent, string name, string label, int index, float low, float high, float value, UnityEngine.Events.UnityAction<float> action, out Text valueText)
        {
            var row = Child(parent, name, typeof(RectTransform)); JourneyHud.PositionExperimentRow(row.GetComponent<RectTransform>(), index);
            valueText = Text(row.transform, "Value", label, 16, Color.white, TextAnchor.MiddleLeft); Anchor(valueText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -24), Vector2.zero);
            var go = Child(row.transform, "Slider", typeof(RectTransform), typeof(Slider)); var slider = go.GetComponent<Slider>(); Anchor(slider.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1, 0), new Vector2(0, 4), new Vector2(0, 20)); slider.minValue = low; slider.maxValue = high; slider.SetValueWithoutNotify(value); slider.wholeNumbers = false; slider.direction = Slider.Direction.LeftToRight;
            var track = Rounded(slider.transform, "Track", new Color(.07f, .22f, .26f, 1), 5, 0); Stretch(track.rectTransform, slider.GetComponent<RectTransform>(), 0, 0); track.raycastTarget = false;
            var handle = Rounded(slider.transform, "Handle", Cyan, 9, 0); handle.rectTransform.sizeDelta = new Vector2(18, 18); handle.raycastTarget = true; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            return slider;
        }
        static void SetSliderPersistent(Slider slider, UnityAction<float> action) { ResetPersistentListeners(slider.onValueChanged); UnityEventTools.AddPersistentListener(slider.onValueChanged, action); }
        static void ResetPersistentListeners(UnityEventBase unityEvent)
        {
            while (unityEvent.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(unityEvent, unityEvent.GetPersistentEventCount() - 1);
            unityEvent.RemoveAllListeners();
        }
        static GameObject Child(Transform parent, string name, params Type[] components)
        {
            var old = parent.Find(name);
            if (old)
            {
                foreach (var component in components)
                    if (!old.GetComponent(component)) old.gameObject.AddComponent(component);
                if(old.GetComponent<Graphic>()&&!old.GetComponent<CanvasRenderer>())old.gameObject.AddComponent<CanvasRenderer>();
                return old.gameObject;
            }
            var go = new GameObject(name, components); go.transform.SetParent(parent, false); return go;
        }
        static T GetOrAdd<T>(GameObject go) where T : Component { var value = go.GetComponent<T>(); return value ? value : go.AddComponent<T>(); }
        static T AddOrGet<T>(GameObject go) where T : Component { return GetOrAdd<T>(go); }
        static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax) { rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offsetMin; rt.offsetMax = offsetMax; }
        static void Stretch(RectTransform rt, RectTransform parent, float insetMin, float insetMax) { rt.SetParent(parent, false); Anchor(rt, Vector2.zero, Vector2.one, new Vector2(insetMin, insetMin), new Vector2(-insetMax, -insetMax)); }
    }
}
