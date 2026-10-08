// RECOVERY: Reconstructed from this session's captured source and patches; Z: source read failed.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Presentation and explicit focused UI commands; mission rules remain in JourneyMission.</summary>
    public sealed class JourneyHud : MonoBehaviour
    {
        public JourneyMission mission;
        public JourneyInput input;
        public Text title, goal, feedback, vitals, primaryLabel, progress, info;
        public Button continueButton;
        public Slider radius, pressure, balance;
        [Header("Holographic UI (assigned by JourneyHudOverhaul.Apply)")]
        public RectTransform panelRoot;
        public GameObject briefingRoot, experimentRoot, radiusRow, pressureRow, balanceRow;
        public Text toolHint, targetLabel, radiusValue, pressureValue, balanceValue, experimentSummary;
        public Text briefingButtonLabel;
        public Button primaryButton, scanButton;
        public RectTransform progressFill;
        public CanvasGroup panelGroup;
        public JourneyTargetMarker targetMarker;
        public JourneyUiInputGuard inputGuard;
        public float compactHeight = 724f, experimentHeight = 996f;
        public const float OneRowExperimentHeight = 200f, TwoRowExperimentHeight = 260f;
        [Tooltip("Automatically open the optional briefing after a successful panel scan.")]
        public bool showBriefingAfterSuccessfulScan = false;
        [Header("Legacy desktop debug (optional)")]
        public bool showDesktopPanel = true;
        public bool showDebugButtons = true;
        public bool compactNormalMode=true;
        public bool allowDebugGameplayButtons;

        bool panelVisible = true;
        int previousScene=-1;
        JourneyMission subscribedMission;

        void OnEnable() { EnsureGraphicRenderers(); Subscribe(); Refresh(); }
        void Start() { EnsureGraphicRenderers(); Subscribe(); Refresh(); }
        void EnsureGraphicRenderers()
        {
            foreach(var graphic in UnityEngine.Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None))
                if(graphic&&!graphic.GetComponent<CanvasRenderer>())graphic.gameObject.AddComponent<CanvasRenderer>();
        }
        void OnDisable() { Unsubscribe(); }
        void Subscribe()
        {
            if (subscribedMission == mission) return;
            Unsubscribe();
            if (mission) { subscribedMission = mission; subscribedMission.Changed += Refresh; }
        }
        void Unsubscribe()
        {
            if (subscribedMission) subscribedMission.Changed -= Refresh;
            subscribedMission = null;
        }

        public void Refresh()
        {
            if (!mission || mission.Goals == null)
            {
                Set(goal, "Подготовка исследовательского режима…");
                Set(progress, "Подготовка");
                UpdateButtons();
                return;
            }
            var current = mission.Current;
            int scene = mission.SceneNumber;
            string chapter = scene <= 16
                ? ArteryRouteController.Titles[Mathf.Clamp(scene - 3, 0, ArteryRouteController.Titles.Length - 1)]
                : scene == 17 ? "Возвращение в лабораторию" : "Финальный отчёт";
            Set(title, mission.Victory ? "ИССЛЕДОВАНИЕ • ГОМЕОСТАЗ ВОССТАНОВЛЕН" : $"{scene:00} / {chapter}");
            Set(goal, mission.CurrentGoalLabel);
            if(goal){goal.fontSize=scene==16&&mission.Complete?19:28;if(scene==16&&mission.Complete)goal.text=mission.CompactResult;}
            Set(feedback, mission.Feedback);
            Set(progress, mission.Complete ? "Этап завершён" :
                $"Шаг {Mathf.Min(mission.StepIndex + 1, mission.Goals.Count)} / {mission.Goals.Count}" +
                (current != null && current.required > 1 ? $"     •     {mission.Progress} / {current.required}" : ""));
            Set(primaryLabel, current == null ? "Действия завершены" : FocusedActionLabel(current));
            Set(targetLabel, current == null ? "ЦЕЛЬ • этап завершён" : "ЦЕЛЬ • " + TargetName(current.target));
            Set(toolHint, mission.FreeResearchUnlocked&&scene==18?"Свободное исследование: сканер F / левый Trigger\nНаведи сканер на модель и открой справку":ToolHint(current));
            if(!mission.mover.UsingTrackedInput&&toolHint)toolHint.text=DesktopHint(current);
            Set(info, scene==16&&mission.Complete?mission.ResultGrades:$"<b>{mission.LearningTerm}</b>\n\n{mission.LearningNote}" +
                (string.IsNullOrEmpty(mission.Formula) ? "" : "\n\n" + mission.Formula) +
                $"\n\nПрофессор: «{mission.ProfessorLine}»");
            Set(vitals, $"АД {mission.PressureSystolic:0}/{mission.PressureDiastolic:0}    •    T {mission.Temperature:0.0} °C    •    Вирус {mission.ViralLoad:0} %");
            if (progressFill)
            {
                float fraction = mission.Goals.Count == 0 ? 1f :
                    (mission.StepIndex + (current != null ? (float)mission.Progress / Mathf.Max(1, current.required) : 0f)) / mission.Goals.Count;
                var anchorMax = progressFill.anchorMax;
                anchorMax.x = Mathf.Clamp01(fraction);
                progressFill.anchorMax = anchorMax;
            }

            bool radiusVisible = scene == 5 || scene == 10;
            bool pressureVisible = scene == 5;
            bool balanceVisible = scene == 13;
            bool experiment = radiusVisible || pressureVisible || balanceVisible;
            if (experimentRoot) experimentRoot.SetActive(experiment);
            int visibleRows = 0;
            SetRow(radiusRow, radius, radiusVisible, ref visibleRows);
            SetRow(pressureRow, pressure, pressureVisible, ref visibleRows);
            SetRow(balanceRow, balance, balanceVisible, ref visibleRows);
            // Updating presentation must never fire experiment callbacks or advance the mission.
            if (radius) radius.SetValueWithoutNotify(mission.ModelRadius);
            if (pressure) pressure.SetValueWithoutNotify(mission.ModelPressure);
            if (balance) balance.SetValueWithoutNotify(mission.ClotBalance);
            Set(radiusValue, $"Радиус r / r₀     {mission.ModelRadius:0.00}");
            Set(pressureValue, $"Модельное ΔP     {mission.ModelPressure:0} мм рт. ст.");
            Set(balanceValue, $"Баланс гемостаза     {mission.ClotBalance * 100:0} %");
            Set(experimentSummary, balanceVisible
                ? "Недостаток ←     баланс     → Избыток\nЗакрыть дефект и сохранить просвет"
                : scene == 10 ? $"Модель тонуса • r / r₀ = {mission.ModelRadius:0.00}\nНаблюдаемое АД: {mission.PressureSystolic:0}/{mission.PressureDiastolic:0}"
                : $"Q / Q₀ = {mission.ModelFlow:0.00}     •     Q ∝ ΔP · r⁴\nИзменяй один параметр и сравнивай поток");
            float cardHeight = visibleRows > 1 ? TwoRowExperimentHeight : OneRowExperimentHeight;
            if (experimentRoot && experimentRoot.transform is RectTransform experimentRect)
                experimentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardHeight);
            if(previousScene!=scene){if(scene==18)OpenBriefing();else CloseBriefing();previousScene=scene;}
            FitVisiblePanel();
            UpdateButtons();
        }
        void FitVisiblePanel()
        {
            if(!panelRoot)return;
            bool detail=briefingRoot&&briefingRoot.activeSelf;
            bool debugActions=allowDebugGameplayButtons&&detail;
            if(primaryButton)primaryButton.transform.parent.gameObject.SetActive(debugActions);
            float bottom=debugActions||!compactNormalMode?448f:224f;
            if(toolHint)
            {
                var rect=toolHint.rectTransform;if(rect.parent!=panelRoot)rect.SetParent(panelRoot,false);
                rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);
                rect.offsetMin=new Vector2(26,-bottom-68);rect.offsetMax=new Vector2(-26,-bottom-8);
                toolHint.gameObject.SetActive(true);toolHint.raycastTarget=false;bottom+=80;
            }
            if(vitals)
            {
                // Older authored HUDs parent this text to the optional experiment
                // card, which disappears outside Scenes05/10/13. Keep it on the HUD.
                var rect=vitals.rectTransform;if(rect.parent!=panelRoot)rect.SetParent(panelRoot,false);
                rect.anchorMin=new Vector2(0,1);rect.anchorMax=new Vector2(1,1);
                rect.offsetMin=new Vector2(26,-bottom-38);rect.offsetMax=new Vector2(-26,-bottom-8);
                vitals.gameObject.SetActive(true);vitals.raycastTarget=false;bottom+=44;
            }
            if(briefingRoot&&briefingRoot.activeSelf&&briefingRoot.transform is RectTransform briefing)
            {
                float height=Mathf.Max(200,info?info.preferredHeight+32:200);
                briefing.anchorMin=new Vector2(0,1);briefing.anchorMax=new Vector2(1,1);
                briefing.offsetMin=new Vector2(18,-bottom-12-height);briefing.offsetMax=new Vector2(-18,-bottom-12);bottom+=12+height;
            }
            if(experimentRoot)experimentRoot.SetActive(detail&&(mission.SceneNumber==5||mission.SceneNumber==10||mission.SceneNumber==13));
            if(experimentRoot&&experimentRoot.activeSelf&&experimentRoot.transform is RectTransform rt)
            {
                float height=rt.rect.height;
                rt.anchorMin=new Vector2(0,1);rt.anchorMax=new Vector2(1,1);
                rt.offsetMin=new Vector2(18,-bottom-12-height);rt.offsetMax=new Vector2(-18,-bottom-12);
                bottom+=12+height;
            }
            panelRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,bottom+64);
        }

        static void Set(Text text, string value) { if (text) text.text = value; }
        static void SetRow(GameObject row, Slider slider, bool visible, ref int visibleRows)
        {
            if (row) row.SetActive(visible);
            else if (slider) slider.gameObject.SetActive(visible);
            if (!visible) return;
            if (row && row.transform is RectTransform rowRect) PositionExperimentRow(rowRect, visibleRows);
            visibleRows++;
        }
        public static void PositionExperimentRow(RectTransform row, int index)
        {
            float top = 108f + index * 60f;
            row.anchorMin = new Vector2(0, 1);
            row.anchorMax = new Vector2(1, 1);
            row.offsetMin = new Vector2(16, -top - 52f);
            row.offsetMax = new Vector2(-16, -top);
        }
        void UpdateButtons()
        {
            bool initialized = mission && mission.Goals != null;
            bool ready = initialized && mission.Ready;
            var current = initialized ? mission.Current : null;
            if(primaryButton)primaryButton.gameObject.SetActive(allowDebugGameplayButtons);
            if(scanButton)scanButton.gameObject.SetActive(allowDebugGameplayButtons);
            if (primaryButton) primaryButton.interactable = ready && current != null && !mission.Victory;
            if (scanButton) scanButton.interactable = ready && current != null && current.action == StudyAction.Scan&&!XRSettings.isDeviceActive;
            if (continueButton) continueButton.interactable = ready && mission.Complete && mission.SceneNumber < 18 &&
                (mission.SceneNumber != 15 || mission.Victory) && (mission.world == null || !mission.world.Returning);
            if (radius) radius.interactable = ready&&(mission.SceneNumber!=10||allowDebugGameplayButtons);
            if (pressure) pressure.interactable = ready;
            if (balance) balance.interactable = ready&&(!mission.world||!mission.world.hemostasisPuzzle);
        }

        void Update()
        {
            Subscribe();
            UpdateButtons();
            if(panelGroup&&mission&&mission.mover)
            {
                bool transit=mission.mover.Moving;
                panelGroup.alpha=Mathf.MoveTowards(panelGroup.alpha,!panelVisible?0f:transit?.12f:1f,Time.deltaTime/.42f);
                panelGroup.interactable=panelGroup.blocksRaycasts=panelVisible&&!transit;
            }
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f1Key.wasPressedThisFrame) TogglePanel();
            if (keyboard.tabKey.wasPressedThisFrame && panelRoot) ToggleBriefing();
            if (showDebugButtons && mission)
            {
                if (keyboard.f2Key.wasPressedThisFrame) mission.DebugPerformCurrent();
                if (keyboard.f3Key.wasPressedThisFrame) mission.DebugRestartStage();
            }
        }

        // Persistent button listeners bind to these methods, rather than unsaved lambdas.
        public void ExecuteFocused()
        {
            if (!mission || mission.Goals == null || !mission.Ready || mission.Current == null) return;
            if (mission.Current.action == StudyAction.Scan) { ScanFocused(); return; }
            if (input) input.FocusedAction();
        }
        public void ScanFocused()
        {
            if (!mission || mission.Goals == null || !mission.Ready || mission.Current == null) return;
            var target = mission.world ? mission.world.Find(mission.Current.target) : null;
            if (target && target.gameObject.activeInHierarchy)
            {
                bool accepted = mission.Accept(StudyAction.Scan, target.targetId);
                if (accepted && showBriefingAfterSuccessfulScan) OpenBriefing();
            }
            else mission.FeedbackMessage("Цель сканирования пока недоступна. Дождись её появления.");
        }
        public void ContinueJourney() { if (continueButton && continueButton.interactable && mission) mission.Continue(); }
        public void ToggleBriefing()
        {
            if (!briefingRoot) return;
            briefingRoot.SetActive(!briefingRoot.activeSelf);
            Set(briefingButtonLabel, briefingRoot.activeSelf ? "Закрыть справку" : "Справка  [Tab]");
            FitVisiblePanel();
        }
        public void OpenBriefing()
        {
            if (!briefingRoot) return;
            briefingRoot.SetActive(true);
            Set(briefingButtonLabel, "Закрыть справку");
            FitVisiblePanel();
        }
        public void CloseBriefing()
        {
            if (briefingRoot) briefingRoot.SetActive(false);
            Set(briefingButtonLabel, "Справка  [Tab]");
            FitVisiblePanel();
        }
        public void TogglePanel()
        {
            panelVisible = !panelVisible;
            if (panelGroup)
            {
                panelGroup.alpha = panelVisible ? 1f : 0f;
                panelGroup.interactable = panelGroup.blocksRaycasts = panelVisible;
            }
            if (!panelVisible) CloseBriefing();
        }

        static string ToolHint(StudyGoal current)
        {
            if (current == null) return "Продолжить: → / правая B    •    Справка: Tab";
            if(current.action==StudyAction.Grab)return "Захват: поднеси руку к объекту и удерживай Grip\nУвеличение: возьми двумя руками и разведи их";
            if (current.action == StudyAction.Scan) return "Сканер: наведи и удерживай левый Trigger / F\nРаннее отпускание прерывает сканирование";
            if (current.action == StudyAction.Radius || current.action == StudyAction.Pressure || current.action == StudyAction.Balance)
            {
                if(current.target=="tone")return "Наведи BioTool на тонус и удерживай Trigger\nОпусти руку, затем удержи умеренную настройку";
                return "Изменяй параметр на шкале эксперимента\nBioTool: E / правый Trigger • действие по цели";
            }
            return "BioTool: наведи и используй правый Trigger / E\nДля импульса удерживай до завершения заряда";
        }
        static string DesktopHint(StudyGoal current)
        {
            if(current==null)return "→ — продолжить маршрут • Tab — справка";
            if(current.target=="embolus"&&(current.action==StudyAction.Capture||current.action==StudyAction.Deposit))return "Удерживай E • мышь — переноси поле\nКолесо — глубина • удержи эмбол внутри зоны";
            if(current.target=="tone")return "Наведи мышь на регулятор и удерживай E\nКолесо — тонус; удержи умеренное сужение";
            if(current.action==StudyAction.Grab)return "Мышь — указатель • ЛКМ удерживать — взять\nКолесо — глубина • ПКМ + мышь — осмотр";
            if(current.action==StudyAction.Scan)return "Наведи мышь и удерживай F — Scanner\nЛКМ удерживать — перенести • G — взять/отпустить";
            if(current.action==StudyAction.Enlarge)return "Z — учебное увеличение\nЗатем эритроцит плавно вернётся в поток";
            if(current.target=="oxygen")return "G — взять/отпустить O₂ • колесо — ближе/дальше\nНаведи O₂ на гем и отпусти рядом с точкой связывания";
            return "E — BioTool по указателю; для заряда удерживай\nЛКМ — перенос • колесо — глубина • ПКМ — осмотр";
        }
        public static string FocusedActionLabel(StudyGoal current)
        {
            if (current.target == "small-radius") return "Уменьшить радиус модели";
            if (current.target == "large-radius") return "Увеличить радиус модели";
            if (current.target == "tone") return "Настроить сосудистый тонус";
            if (current.action == StudyAction.Balance) return "Проверить: " + TargetName(current.target).ToLowerInvariant();
            return ActionLabel(current.action) + ": " + TargetName(current.target).ToLowerInvariant();
        }
        public static string ActionLabel(StudyAction action)
        {
            switch (action)
            {
                case StudyAction.Grab: return "Захватить";
                case StudyAction.Scan: return "Сканировать";
                case StudyAction.Enlarge: return "Увеличить";
                case StudyAction.Detach: return "Отсоединить";
                case StudyAction.Bind: return "Связать";
                case StudyAction.Activate: return "Активировать";
                case StudyAction.Measure: return "Измерить";
                case StudyAction.Radius: return "Изменить радиус";
                case StudyAction.Pressure: return "Изменить давление";
                case StudyAction.Pulse: return "Импульс";
                case StudyAction.Capture: return "Захватить полем";
                case StudyAction.Deposit: return "Поместить в ловушку";
                case StudyAction.Mark: return "Пометить";
                case StudyAction.Direct: return "Направить";
                case StudyAction.Balance: return "Настроить баланс";
                case StudyAction.Compare: return "Сравнить";
                case StudyAction.Return: return "Вернуться";
                default: return "Исследовать";
            }
        }
        public static string TargetName(string id)
        {
            switch (id)
            {
                case "rbc": return "Эритроцит";
                case "hemoglobin": return "Гемоглобин";
                case "heme": return "Гем и Fe²⁺";
                case "oxygen": return "Кислород O₂";
                case "co2": return "Углекислый газ CO₂";
                case "leukocyte": return "Лейкоцит";
                case "platelet": return "Тромбоцит";
                case "plasma": return "Белки и ионы плазмы";
                case "endothelium": return "Эндотелий";
                case "layers": return "Слои сосудистой стенки";
                case "intima": return "Интима";
                case "media": return "Медиа";
                case "adventitia": return "Адвентиция";
                case "flow": case "plaque-flow": case "open-flow": return "Кровоток";
                case "flow-model": return "Модель кровотока";
                case "small-radius": case "large-radius": return "Радиус модели";
                case "pressure": return "Модельное давление";
                case "normal-flow": return "Нормальный кровоток";
                case "plaque": return "Атеросклеротическая бляшка";
                case "pulse-mode": return "Режим «Импульс»";
                case "lipid": return "Липидная мишень";
                case "cap": return "Фиброзная покрышка";
                case "embolus": return "Газовый эмбол";
                case "attract-mode": return "Режим «Притяжение»";
                case "virus-study": return "Вирусная частица";
                case "genome": return "Генетический материал";
                case "capsid": return "Капсид";
                case "epitope": return "Эпитоп";
                case "immune-mode": return "Иммунное управление";
                case "incoming-virus": return "Входящий вирион";
                case "phagocyte": return "Фагоцит";
                case "pressure-low": return "Пониженное давление";
                case "pressure-stable": return "Стабилизация давления";
                case "pressure-return": return "Повторное падение давления";
                case "tone-mode": return "Сосудистое поле";
                case "tone": return "Сосудистый тонус";
                case "wound": return "Повреждение эндотелия";
                case "leak": return "Утечка крови";
                case "thrombin": return "Тромбин";
                case "fibrin": return "Фибриновая сеть";
                case "clot": return "Стабильный сгусток";
                case "insufficient": return "Недостаточный ответ";
                case "excessive": return "Избыточный ответ";
                case "optimal": return "Оптимальный баланс";
                case "debris": return "Клеточный детрит";
                case "repair": return "Восстановление эндотелия";
                case "healed-wall": return "Восстановленная стенка";
                case "antibody-A": return "Комплементарное антитело";
                case "antibody-B": case "antibody-C": return "Антитело";
                case "neutralized": return "Нейтрализованные комплексы";
                case "infected-cell": return "Инфицированная клетка";
                case "t-cell": return "T-лимфоцит";
                case "remaining": return "Оставшиеся комплексы";
                case "vitals": return "Показатели организма";
                case "homeostasis": return "Гомеостаз";
                case "return-field": return "Поле возврата";
                default: return "Объект исследования";
            }
        }

        void OnGUI()
        {
            // Keep an opt-in legacy diagnostic surface for scenes that have not applied the helper.
            if (panelRoot || !showDesktopPanel || !panelVisible || XRSettings.isDeviceActive || !mission) return;
            GUILayout.BeginArea(new Rect(12, 96, Mathf.Min(500, Screen.width - 24), 280), GUI.skin.box);
            GUILayout.Label("BIOLOGY VR • ИССЛЕДОВАНИЕ");
            GUILayout.Label(mission.CurrentGoalLabel);
            GUILayout.Label(mission.Feedback);
            GUILayout.Label("F — сканер • E — BioTool • → — продолжить • F1 — скрыть");
            if (GUILayout.Button("Действие по выбранной цели")) ExecuteFocused();
            if (GUILayout.Button("Сканировать выбранную цель")) ScanFocused();
            if (showDebugButtons && GUILayout.Button("Повторить этап")) mission.DebugRestartStage();
            GUILayout.EndArea();
        }
    }
}
