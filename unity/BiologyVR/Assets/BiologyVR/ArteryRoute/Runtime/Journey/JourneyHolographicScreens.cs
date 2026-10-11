using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyHolographicScreens:MonoBehaviour
    {
        public JourneyHud hud;
        public RectTransform theoryRoot,vitalsRoot,taskRoot;
        public ScrollRect theoryScroll;
        public Text theoryTitle,theoryText,vitalsText,taskText,taskHint;
        public GameObject overview,referenceView,practiceButton;
        public Text shortTheory,illustrationText,taskProgress,pageLabel;
        public Text[] vitalValues,vitalStatuses;
        public Text[] vitalLabels;
        public Text monitorStatus;
        public Button referenceButton;
        public CanvasGroup[] groups;
        public LineRenderer pulseTrace;
        public bool ShowPractice{get;private set;}
        public int ScrollInputs{get;private set;}
        InputAction stick;
        JourneyMission subscribed;
        int chapter=-1;
        float refreshAt;
        void Awake(){stick=new InputAction("Read holographic theory",InputActionType.Value,"<XRController>{RightHand}/thumbstick");}
        void OnEnable(){stick?.Enable();Subscribe();}
        void OnDisable(){stick?.Disable();if(subscribed)subscribed.Changed-=Refresh;subscribed=null;}
        void OnDestroy(){stick?.Dispose();}
        void Subscribe(){if(hud&&hud.mission&&subscribed!=hud.mission){if(subscribed)subscribed.Changed-=Refresh;subscribed=hud.mission;subscribed.Changed+=Refresh;Refresh();}}
        public bool PointerOverTheory
        {
            get
            {
                if(!hud||!hud.mission||Mouse.current==null||!theoryRoot||!theoryRoot.gameObject.activeInHierarchy)return false;
                return RectTransformUtility.RectangleContainsScreenPoint(theoryRoot,Mouse.current.position.ReadValue(),hud.mission.mover.viewCamera);
            }
        }
        bool TrackedPointerOverTheory()
        {
            if(!hud||!hud.inputGuard)return false;
            if(hud.inputGuard.nearFarRays!=null)foreach(var ray in hud.inputGuard.nearFarRays)
                if(ray&&ray.isActiveAndEnabled&&ray.TryGetCurrentUIRaycastResult(out var hit)&&hit.gameObject&&hit.gameObject.transform.IsChildOf(theoryRoot))return true;
            foreach(var aim in new[]{hud.input.scanner,hud.input.bioTool})
            {
                if(!aim)continue;var plane=new Plane(theoryRoot.forward,theoryRoot.position);
                if(plane.Raycast(new Ray(aim.position,aim.forward),out float distance)&&distance>0&&distance<4)
                {var local=theoryRoot.InverseTransformPoint(aim.position+aim.forward*distance);if(theoryRoot.rect.Contains(local))return true;}
            }
            return false;
        }
        public void Refresh()
        {
            if(!hud||!hud.mission||hud.mission.Goals==null)return;
            var m=hud.mission;
            theoryTitle.text=m.LearningTerm;
            if(shortTheory)shortTheory.text=ShortExplanation(m.SceneNumber);
            if(illustrationText)illustrationText.text=MiniDiagram(m.SceneNumber);
            var text=new StringBuilder();text.Append("<b>").Append(m.LearningTerm).Append("</b>\n\n").Append(m.LearningNote);
            if(!string.IsNullOrEmpty(m.Formula))text.Append("\n\n<b>Формула / связь</b>\n").Append(m.Formula);
            text.Append("\n\n<b>Профессор</b>\n«").Append(m.ProfessorLine).Append("»");
            text.Append("\n\n<b>Практика</b>\n").Append(m.CurrentGoalLabel);
            if(ShowPractice){text.Append("\n\n<b>План исследования</b>\n");foreach(var goal in m.Goals)text.Append("• ").Append(goal.label).Append('\n');text.Append("\n").Append(m.PerformanceSummary);}
            if(m.SceneNumber==3)text.Append("\n\nHb, гем и O₂ показаны в увеличенном учебном масштабе и перемещаются с эритроцитом. CO₂ и компоненты плазмы представлены учебными моделями. Достань объект из потока перед сканированием.");
            if(theoryText.text!=text.ToString())theoryText.text=text.ToString();
            taskText.text=m.CurrentGoalLabel;
            taskHint.text=ShortControls(m);
            if(taskProgress)taskProgress.text=m.Complete?"ГОТОВО":m.Current!=null&&m.Current.required>1?$"{m.Progress} / {m.Current.required}":$"{m.StepIndex+1} / {m.Goals.Count}";
            if(m.SceneNumber==6&&m.world.plaqueChallenge&&m.Current?.action==StudyAction.Pulse)
            {var plaque=m.world.plaqueChallenge;taskProgress.text=$"Осталось {plaque.RemainingCount} • {plaque.RemainingSeconds:0}с";taskHint.text=plaque.Failed?"ОККЛЮЗИЯ • повтор через 3 секунды":$"Trigger / E — очередь • прибыло +{plaque.ReinforcementCount}";}
            if(m.SceneNumber==7&&m.world.embolusWave)taskProgress.text=$"Ловушка {m.world.embolusWave.StabilizedCount}/6";
            if(m.SceneNumber==12&&m.world.hemostasisPuzzle&&m.world.hemostasisPuzzle.flowLayering)
            {var puzzle=m.world.hemostasisPuzzle;taskProgress.text=puzzle.Activated?$"Фибрин {m.world.FibrinGrowth:P0}":puzzle.Attached<11?$"Первичная {puzzle.Attached}/11":$"Цепочка {puzzle.activationPuzzle.Progress}/3";if(m.Current?.action==StudyAction.Activate)taskHint.text="Grip / ЛКМ — три этапа стабилизации по порядку";else if(m.Current?.target=="platelet")taskHint.text="Удерживай Capture • подведи: адгезия автоматическая";}
            if(overview)overview.SetActive(!ShowPractice);
            if(referenceView)referenceView.SetActive(ShowPractice);
            theoryScroll.enabled=ShowPractice;
            if(pageLabel)pageLabel.text=ShowPractice?"СПРАВКА • 02 / 02":"КРАТКО • 01 / 02";
            if(practiceButton)practiceButton.SetActive(!m.Complete);
            if(hud.continueButton)hud.continueButton.gameObject.SetActive(m.Complete);
            if(chapter!=m.SceneNumber){chapter=m.SceneNumber;Canvas.ForceUpdateCanvases();theoryScroll.verticalNormalizedPosition=1;}
            RefreshVitals();
        }
        void RefreshVitals()
        {
            if(!hud||!hud.mission)return;var m=hud.mission;
            if(vitalsText)vitalsText.text=$"АД {m.PressureSystolic:0}/{m.PressureDiastolic:0} • Пульс 72 • SpO₂ 98 % • T {m.Temperature:0.0} °C • Вирус {m.ViralLoad:0} %";
            if(vitalValues==null||vitalValues.Length!=5)return;
            var values=new[]{$"{m.PressureSystolic:0}/{m.PressureDiastolic:0}","72","98 %",$"{m.Temperature:0.0}°",$"{m.ViralLoad:0} %"};
            var plaque=m.SceneNumber==6?m.world.plaqueChallenge:null;
            var wound=m.SceneNumber>=11&&m.SceneNumber<=14?m.world.woundState:null;
            if(plaque){values[1]=$"{plaque.PulseBpm:0}";values[2]=$"{plaque.OxygenPercent:0} %";values[4]=$"{plaque.FlowFraction*100:0} %";}
            if(wound)values[4]=$"{wound.LeakStrength*100:0} %";
            if(vitalLabels!=null&&vitalLabels.Length==5)vitalLabels[4].text=plaque?"КРОВОТОК":wound?"УТЕЧКА":"ВИРУС";
            if(monitorStatus)monitorStatus.text=plaque?"● ЛОКАЛЬНАЯ МОДЕЛЬ":"● МОДЕЛЬ / LIVE";
            bool infection=m.ViralLoad>=.5f;
            var states=new[]{m.PressureSystolic<110?"Ниже нормы":m.PressureSystolic>130?"Выше нормы":"Норма","Норма","Норма",m.Temperature>37.2f?"Повышена":"Норма",infection?"Инфекция":"Не обнаружен"};
            if(plaque){states[1]=plaque.PulseBpm>90?"Компенсация":"Норма";states[2]=plaque.OxygenPercent<95?"Снижена":"Норма";states[4]=plaque.Failed?"ОККЛЮЗИЯ":plaque.Cleared?"Просвет свободен":$"Сужение {plaque.Obstruction*100:0}%";}
            if(wound)states[4]=wound.LeakStrength>.03f?"Кровь наружу":"Остановлена";
            for(int i=0;i<5;i++){vitalValues[i].text=values[i];vitalStatuses[i].text=states[i];vitalStatuses[i].color=(i==0&&(m.PressureSystolic<110||m.PressureSystolic>130)||i==3&&m.Temperature>37.2f||i==4&&(infection||plaque&&!plaque.Cleared)||plaque&&((i==1&&plaque.PulseBpm>90)||(i==2&&plaque.OxygenPercent<95)))?HolographicStyle.Amber:HolographicStyle.Secondary;}
        }
        public void TogglePractice(){ShowPractice=!ShowPractice;Refresh();}
        public void ShowOverview(){ShowPractice=false;Refresh();}
        public void ScrollDown(){ScrollBy(-.20f);}
        public void ScrollUp(){ScrollBy(.20f);}
        void ScrollBy(float amount){theoryScroll.verticalNormalizedPosition=Mathf.Clamp01(theoryScroll.verticalNormalizedPosition+amount);ScrollInputs++;}
        void Update()
        {
            Subscribe();if(!hud||!hud.mission)return;
            if(Time.time>=refreshAt){refreshAt=Time.time+.15f;RefreshVitals();}
            bool visible=!hud.panelGroup||hud.panelGroup.blocksRaycasts||hud.panelGroup.alpha>.5f;
            foreach(var group in groups)if(group){group.alpha=visible?(hud.mission.mover.Moving?.25f:1):0;group.blocksRaycasts=group.interactable=visible&&!hud.mission.mover.Moving;}
            if(visible&&ShowPractice&&TrackedPointerOverTheory())
            {float y=stick!=null?stick.ReadValue<Vector2>().y:0;if(Mathf.Abs(y)>.25f)ScrollBy(y*Time.deltaTime*.7f);}
            if(visible&&ShowPractice&&Keyboard.current!=null)
            {if(Keyboard.current.pageDownKey.wasPressedThisFrame)ScrollDown();if(Keyboard.current.pageUpKey.wasPressedThisFrame)ScrollUp();}
            if(pulseTrace)
            {
                pulseTrace.enabled=visible;
                pulseTrace.positionCount=64;
                float bpm=hud.mission.SceneNumber==6&&hud.mission.world.plaqueChallenge?hud.mission.world.plaqueChallenge.PulseBpm:72;
                for(int i=0;i<64;i++){float p=Mathf.Repeat(i/63f-Time.time*bpm/60,1);float peak=Mathf.Exp(-Mathf.Pow((p-.5f)*35,2))-.4f*Mathf.Exp(-Mathf.Pow((p-.43f)*30,2));pulseTrace.SetPosition(i,new Vector3(-60+i*120/63f,144+peak*8,0));}
            }
        }
        static string ShortControls(JourneyMission m)
        {
            if(m.Complete)return "Продолжить → / левая Y";
            var goal=m.Current;if(goal==null)return "";
            if(goal.target=="hemostasis-stable")return "Наблюдай фибрин и постепенное уменьшение утечки";
            if(goal.action==StudyAction.Scan)return m.SceneNumber==3?"Grip / ЛКМ — взять · Trigger / F — сканировать":"Scanner: левый Trigger / F · навести на цель";
            if(goal.action==StudyAction.Grab)return "Grip / ЛКМ — достать из потока";
            if(goal.action==StudyAction.Enlarge)return "Две руки / Z — учебное увеличение";
            if(goal.target=="oxygen")return "Взять O₂ · перенести · отпустить у гема";
            if(m.SceneNumber==12&&goal.action==StudyAction.Direct&&goal.target=="platelet")return "BioTool Capture: удерживать Trigger / E • подвести к ране";
            if(goal.action==StudyAction.Bind||((m.SceneNumber==12||m.SceneNumber==13)&&(goal.action==StudyAction.Direct||goal.action==StudyAction.Balance)))return "Grip / ЛКМ — взять · перенести · отпустить";
            if(goal.action==StudyAction.Capture||goal.action==StudyAction.Deposit)return "Удерживать Trigger / E — поле захвата";
            if(m.SceneNumber==9&&goal.action==StudyAction.Mark)return "Trigger / E — импульс · удержание — очередь";
            if(goal.target=="tone")return "Удерживать Trigger · движение руки / колесо";
            return "BioTool: Trigger / E · A/B — режимы · C — ПК";
        }
        static string ShortExplanation(int scene)=>scene switch
        {
            3=>"Эритроциты переносят кислород.\nЛейкоциты защищают организм.\nТромбоциты закрывают повреждения.",
            4=>"Эндотелий контактирует с кровью.\nМедиа управляет тонусом сосуда.\nНаружный слой поддерживает стенку.",
            5=>"Небольшое изменение радиуса\nсильно влияет на кровоток.\nМеняй один параметр и сравнивай.",
            6=>"Растущая масса сужает просвет.\nПоток и доставка кислорода падают.\nРаздроби все элементы до окклюзии.",
            7=>"Пузырёк переносится кровотоком.\nВ узком участке он мешает потоку.\nПоле позволяет удержать пузырёк.",
            8=>"Геном окружён капсидом.\nПоверхностный антиген помогает\nиммунной системе распознать вирус.",
            9=>"Метка помогает иммунной клетке\nраспознать и поглотить вирион.\nПропуски повышают вирусную нагрузку.",
            10=>"Тонус временно поддерживает АД.\nУмеренное сужение сохраняет поток.\nПричину падения давления нужно найти.",
            11=>"Целостность стенки нарушена.\nУтечка ↑ • обнажён субэндотелий.\nЧасть потока идёт наружу, основная — дальше.",
            12=>"Адгезия образует первичную пробку.\nАктивация и агрегация её усиливают.\nФибрин стабилизирует; утечка снижается.",
            13=>"Недостаток пробки оставляет утечку.\nИзбыток перекрывает просвет.\nРабочий результат — равновесие.",
            14=>"Фагоцит очищает клеточный детрит.\nПлазмин удаляет фибрин.\nЭндотелий восстанавливает стенку.",
            15=>"Антитело узнаёт подходящий эпитоп.\nФагоцит удаляет связанный комплекс.\nT-клетка устраняет источник инфекции.",
            16=>"Сравни исходные и итоговые данные.\nПроверь стенку, поток и защиту.\nГомеостаз — согласованная работа системы.",
            _=>"Исследование завершено.\nСравни результаты и повтори\nнаблюдения в свободном режиме."
        };
        static string MiniDiagram(int scene)=>scene switch
        {3=>"Hb + O₂ ⇄ HbO₂",4=>"ЭНДОТЕЛИЙ · МЕДИА",5=>"Q ∝ ΔP · r⁴",6=>"ПОКРЫШКА / ЛИПИДЫ",7=>"ПОТОК → ЛОВУШКА",8=>"ГЕНОМ · КАПСИД · ЭПИТОП",9=>"МЕТКА → ФАГОЦИТ",10=>"РАДИУС ↔ ДАВЛЕНИЕ",11=>"СТЕНКА → ПОТОК",12=>"ТРОМБОЦИТЫ + ФИБРИН",13=>"УТЕЧКА ← БАЛАНС → ПРОСВЕТ",14=>"ОЧИСТКА → REPAIR",15=>"Ag + Ab ⇄ AgAb",_=>"ПОТОК · ЗАЩИТА · БАЛАНС"};
    }
}
