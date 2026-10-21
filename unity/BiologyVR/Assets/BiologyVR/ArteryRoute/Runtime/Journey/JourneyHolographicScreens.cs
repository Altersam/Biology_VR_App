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
        Vector3 theoryHome,vitalsHome;
        Quaternion theoryRotation,vitalsRotation;
        string learnedId,learnedTitle,learnedText,learnedSummary,learnedDiagram;
        int learnedScene=-1;
        public string LearnedTopic=>learnedId;
        public void Learn(string id)
        {
            if(!hud||!hud.mission||string.IsNullOrEmpty(id))return;
            var m=hud.mission;var topic=m.world.research?m.world.research.Find(id):null;
            learnedScene=m.SceneNumber;learnedId=id;
            if(topic&&topic.theory)
            {
                var data=topic.theory;learnedTitle=data.displayName;var text=new StringBuilder();AppendTheory(text,data);
                string[] related=id=="rbc"?new[]{"hemoglobin","heme","oxygen","co2"}:id=="hemoglobin"?new[]{"heme","oxygen","co2"}:id=="platelet"?new[]{"plasma"}:System.Array.Empty<string>();
                foreach(var key in related){var item=m.world.research.Find(key);if(item&&item.theory){text.Append("\n<b>Связано с темой: ").Append(item.theory.displayName).Append("</b>\n");AppendTheory(text,item.theory);}}
                if(m.SceneNumber>=4&&m.SceneNumber<=16)text.Append("\n<b>Применение на этом участке</b>\n").Append(LessonDetails(m.SceneNumber));
                text.Append("\nУчебные модели увеличены. Цвет и размер частиц условны.");learnedText=text.ToString();learnedSummary=data.pages.Length>0?data.pages[0].text:"";learnedDiagram=data.pages.Length>0?data.pages[0].visualConnection:"";
            }
            else
            {
                learnedTitle=m.SceneNumber==10?"Иммунный надзор":m.LearningTerm;learnedSummary=ShortExplanation(m.SceneNumber);learnedDiagram=MiniDiagram(m.SceneNumber);
                learnedText="<b>"+learnedTitle+"</b>\n\n"+m.LearningNote+"\n\n"+LessonDetails(m.SceneNumber)+"\n\n<b>Связь с наблюдением</b>\n"+m.Formula;
            }
            ShowPractice=true;Refresh();Canvas.ForceUpdateCanvases();theoryScroll.verticalNormalizedPosition=1;
        }
        static void AppendTheory(StringBuilder text,JourneyResearchTheory data)
        {foreach(var page in data.pages)text.Append("<b>").Append(page.heading).Append("</b>\n").Append(page.text).Append("\n").Append(page.visualConnection).Append("\n\n");}
        public void PresentLearning()
        {
            if(!hud||!hud.mission||hud.mission.Goals==null)return;var m=hud.mission;
            if(learnedScene!=m.SceneNumber){learnedId=null;learnedTitle=null;learnedText=null;learnedScene=m.SceneNumber;ShowPractice=false;}
            bool learned=!string.IsNullOrEmpty(learnedId);
            theoryTitle.text=learned?learnedTitle:m.LearningTerm;
            if(learned){shortTheory.text=learnedSummary;illustrationText.text=learnedDiagram;theoryText.text=learnedText;}
            else{shortTheory.text=ShortExplanation(m.SceneNumber);illustrationText.text=m.SceneNumber==10&&m.world.immunePatrol?"ОБНАРУЖИТЬ → НАПРАВИТЬ → УСТРАНИТЬ":MiniDiagram(m.SceneNumber);theoryText.text="<b>"+m.LearningTerm+"</b>\n\n"+m.LearningNote+"\n\n"+LessonDetails(m.SceneNumber);}
            if(overview)overview.SetActive(!ShowPractice);if(referenceView)referenceView.SetActive(ShowPractice);if(taskRoot)taskRoot.gameObject.SetActive(true);theoryScroll.enabled=ShowPractice;
            if(pageLabel)pageLabel.text=(ShowPractice?"ТЕОРИЯ · прокрути ниже":"УЧЕБНАЯ КАРТОЧКА")+" · v"+Application.version;
            if(referenceButton)referenceButton.GetComponentInChildren<Text>().text=ShowPractice?"Кратко":"Вся теория";
            if(practiceButton){practiceButton.GetComponentInChildren<Text>().text="К действию";practiceButton.SetActive(!m.Complete);}
        }
        void Awake(){stick=new InputAction("Read holographic theory",InputActionType.Value,"<XRController>{RightHand}/thumbstick");if(theoryRoot&&vitalsRoot){theoryHome=theoryRoot.localPosition;vitalsHome=vitalsRoot.localPosition;theoryRotation=theoryRoot.localRotation;vitalsRotation=vitalsRoot.localRotation;}}
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
            if(theoryRoot&&vitalsRoot)
            {
                theoryRoot.localPosition=theoryHome;vitalsRoot.localPosition=vitalsHome;
                theoryRoot.localRotation=theoryRotation;vitalsRoot.localRotation=vitalsRotation;
            }
            if(m.FreeBloodResearch){RefreshResearch(m.world.research);RefreshVitals();PresentLearning();return;}
            shortTheory.resizeTextForBestFit=false;
            if(referenceButton)referenceButton.GetComponentInChildren<Text>().text="Подробнее";
            if(practiceButton)practiceButton.GetComponentInChildren<Text>().text="К практике";
            theoryTitle.text=m.LearningTerm;
            if(shortTheory)shortTheory.text=m.SceneNumber==10&&m.world.immunePatrol?"Изменённая стенка → Scanner.\nГлубокий просмотр: повторный Scanner.\nРЕВЕРС направляет лейкоциты к очагам.":m.world.freeTools&&(m.SceneNumber==8||m.SceneNumber==9)?(m.world.plateletLaunch?"1. Притяжение: захвати фагоцит.\n2. Scanner: изучи и активируй.\n3. Реверс: наведи и отпусти.":"1. Притяжение: захвати фагоцит.\n2. Scanner: изучи клетку.\n3. Активация: удержи курок.\n4. Реверс: наведи и отпусти."):ShortExplanation(m.SceneNumber);
            if(illustrationText)illustrationText.text=m.SceneNumber==10&&m.world.immunePatrol?"ОБНАРУЖИТЬ → НАПРАВИТЬ → УСТРАНИТЬ":MiniDiagram(m.SceneNumber);
            var text=new StringBuilder();text.Append("<b>").Append(m.LearningTerm).Append("</b>\n\n").Append(m.LearningNote);
            if(!string.IsNullOrEmpty(m.Formula))text.Append("\n\n<b>Формула / связь</b>\n").Append(m.Formula);
            text.Append("\n\n<b>Профессор</b>\n«").Append(m.ProfessorLine).Append("»");
            text.Append("\n\n<b>Практика</b>\n").Append(m.CurrentGoalLabel);
            if(ShowPractice){text.Append("\n\n<b>План исследования</b>\n");foreach(var goal in m.Goals)text.Append("• ").Append(goal.label).Append('\n');text.Append("\n").Append(m.PerformanceSummary);}
            if(theoryText.text!=text.ToString())theoryText.text=text.ToString();
            taskText.text=m.CurrentGoalLabel;
            taskHint.text=ShortControls(m);
            if(taskProgress)taskProgress.text=m.Complete?"ГОТОВО":m.Current!=null&&m.Current.required>1?$"{m.Progress} / {m.Current.required}":$"{m.StepIndex+1} / {m.Goals.Count}";
            if(m.SceneNumber==6&&m.world.plaqueChallenge&&m.Current?.action==StudyAction.Pulse)
            {var plaque=m.world.plaqueChallenge;taskProgress.text=$"Осталось {plaque.RemainingCount} • {plaque.RemainingSeconds:0}с";taskHint.text=plaque.Failed?"ОККЛЮЗИЯ • повтор через 3 секунды":$"Trigger / E — очередь • прибыло +{plaque.ReinforcementCount}";}
            if(m.SceneNumber==7&&m.world.embolusWave)taskProgress.text=$"Ловушка {m.world.embolusWave.StabilizedCount}/6";
            if(m.world.immunity&&m.Current?.target=="immune-engulf")taskProgress.text=m.SceneNumber==8?"НАБЛЮДАЙ КОНТАКТ":$"Нагрузка {m.ViralLoad:0}%";
            if(m.SceneNumber==12&&!m.world.plateletLaunch&&m.world.hemostasisPuzzle&&m.world.hemostasisPuzzle.flowLayering)
            {var puzzle=m.world.hemostasisPuzzle;taskProgress.text=puzzle.Activated?$"Фибрин {m.world.FibrinGrowth:P0}":puzzle.Attached<11?$"Первичная {puzzle.Attached}/11":$"Цепочка {puzzle.activationPuzzle.Progress}/3";if(m.Current?.action==StudyAction.Activate)taskHint.text="Grip / ЛКМ — три этапа стабилизации по порядку";else if(m.Current?.target=="platelet")taskHint.text="Удерживай Capture • подведи: адгезия автоматическая";}
            if(overview)overview.SetActive(!ShowPractice);
            if(referenceView)referenceView.SetActive(ShowPractice);
            theoryScroll.enabled=ShowPractice;
            if(pageLabel)pageLabel.text=ShowPractice?"СПРАВКА • 02 / 02":"КРАТКО • 01 / 02";
            if(practiceButton)practiceButton.SetActive(!m.Complete);
            if(hud.continueButton)hud.continueButton.gameObject.SetActive(m.Complete);
            if(chapter!=m.SceneNumber){chapter=m.SceneNumber;Canvas.ForceUpdateCanvases();theoryScroll.verticalNormalizedPosition=1;}
            RefreshVitals();
            PresentLearning();
        }
        void RefreshResearch(JourneyResearchManager research)
        {
            var m=hud.mission;var focused=research.Focused;var data=focused?focused.theory:null;
            theoryTitle.text=data?data.displayName:"Состав крови";
            shortTheory.resizeTextForBestFit=!m.world.freeTools;shortTheory.resizeTextMinSize=24;shortTheory.resizeTextMaxSize=30;
            shortTheory.text=data?(m.world.freeTools?FlowSynopsis(data.id):data.pages[focused.Page].text):m.world.freeTools?"O₂: голубой груз на RBC.\nCO₂: тёмный груз на RBC.\nHb + железо: жёлтая метка.\nЗахват — BioTool ПРИТЯЖЕНИЕ.":"Осмотрись: клетки движутся рядом.\nВыбери элемент потока — порядок свободный.\nМолекулярные детали откроются в анализе.";
            illustrationText.text=data?data.pages[focused.Page].visualConnection:"КЛЕТКИ · ПЛАЗМА · ГАЗООБМЕН";
            var text=new StringBuilder();
            if(data){text.Append("<b>").Append(data.displayName).Append("</b>\n\n");foreach(var page in data.pages)text.Append("<b>").Append(page.heading).Append("</b>\n").Append(page.text).Append("\n\n");text.Append("Учебный масштаб условный; молекулярные модели связаны с их переносчиком.");}
            else text.Append(m.world.freeTools?"Все образцы находятся в общем потоке. O₂ и часть CO₂ показаны связанным грузом эритроцита; основной перенос CO₂ в организме — бикарбонат. Hb и его гем/железо показаны связанным учебным образцом. BioTool ПРИТЯЖЕНИЕ захватывает носитель; после отпускания он возвращается в кровь.":"Выбери конкретный элемент общего потока. Эритроцит, лейкоцит, фагоцит, тромбоцит и плазма доступны в любом порядке. Гемоглобин, гем/Fe²⁺, O₂ и CO₂ открываются в режиме анализа эритроцита и транспорта газов.");
            theoryText.text=text.ToString();
            taskText.text=!string.IsNullOrEmpty(m.ProfessorLine)?m.ProfessorLine:m.CurrentGoalLabel;
            taskHint.text=data&&focused.State==JourneyResearchState.Investigated?"Изучено • можно открыть снова или выбрать другой объект":m.world.freeTools?"Захват: BioTool / ПРИТЯЖЕНИЕ / правый курок":"Scanner: F / левый Trigger • Grip / ЛКМ — рассмотреть";
            taskProgress.text=$"Состав крови {research.RequiredInvestigated}/{research.RequiredCount}";
            pageLabel.text=data?$"{(focused.State==JourneyResearchState.Investigated?"ИЗУЧЕНО":"ИССЛЕДУЕМ")} • {focused.Page+1:00} / {data.pages.Length:00}":"СВОБОДНАЯ ЛАБОРАТОРИЯ / ВЫБЕРИ КЛЕТКУ";
            if(referenceButton)referenceButton.GetComponentInChildren<Text>().text=data?"Ещё →":"Подробнее";
            if(practiceButton){practiceButton.GetComponentInChildren<Text>().text="Объекты";practiceButton.SetActive(!m.Complete);}
            if(hud.continueButton)hud.continueButton.gameObject.SetActive(m.Complete);
            if(overview)overview.SetActive(!ShowPractice);if(referenceView)referenceView.SetActive(ShowPractice);theoryScroll.enabled=ShowPractice;
            if(chapter!=3){chapter=3;Canvas.ForceUpdateCanvases();theoryScroll.verticalNormalizedPosition=1;}
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
        public void NextTheoryPage()=>TogglePractice();
        public void ShowResearchOverview(){if(hud.mission.FreeBloodResearch){ShowPractice=false;hud.mission.world.research.ShowOverview();}else ShowOverview();}
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
            if(m.world.freeTools&&m.SceneNumber==5)return "Поле: курок + рука / колесо";
            var goal=m.Current;if(goal==null)return "";
            if(goal.target=="hemostasis-stable")return "Наблюдай фибрин и постепенное уменьшение утечки";
            if(goal.action==StudyAction.Scan)return "Scanner: левый Trigger / F · навести на цель";
            if(goal.action==StudyAction.Grab)return "Grip / ЛКМ — достать из потока";
            if(goal.action==StudyAction.Enlarge)return "Две руки / Z — учебное увеличение";
            if(goal.target=="oxygen")return "Взять O₂ · перенести · отпустить у гема";
            if(m.SceneNumber==12&&goal.action==StudyAction.Direct&&goal.target=="platelet")return m.world.plateletLaunch?"ПРИТЯЖЕНИЕ → Scanner → РЕВЕРС в разрыв":"BioTool Capture: удерживать Trigger / E • подвести к ране";
            if(goal.action==StudyAction.Bind||((m.SceneNumber==12||m.SceneNumber==13)&&(goal.action==StudyAction.Direct||goal.action==StudyAction.Balance)))return "Grip / ЛКМ — взять · перенести · отпустить";
            if(goal.action==StudyAction.Capture||goal.action==StudyAction.Deposit)return "Удерживать Trigger / E — поле захвата";
            if(goal.target=="phagocyte"&&goal.action==StudyAction.Activate)return m.world.freeTools?"АКТИВАЦИЯ: правый курок / E":"Scanner: удерживать левый Trigger / F • реакция в модели";
            if(goal.target=="immune-engulf")return m.world.freeTools?"РЕВЕРС: удерживать → навести → отпустить":m.SceneNumber==8?"CAPTURE: удерживать → отвести руку → направить → отпустить":"Вирусная нагрузка • используй ранее изученный цикл";
            if(goal.target=="tone")return "Удерживать Trigger · движение руки / колесо";
            return "BioTool: Trigger / E · A/B — режимы · C — ПК";
        }
        static string FlowSynopsis(string id)=>id switch
        {
            "rbc"=>"• Зрелый RBC не имеет ядра.\n• Гибкий двояковогнутый диск.\n• Переносит O₂ благодаря Hb.\n• Участвует в переносе части CO₂.",
            "leukocyte"=>"• Иммунная клетка с ядром.\n• Существует несколько типов.\n• Часть активно фагоцитирует.",
            "phagocyte"=>"• Функциональный тип лейкоцита.\n• Захвати и изучи Scanner.\n• Активируй и направь к вирусу.",
            "platelet"=>"• Фрагмент мегакариоцита.\n• Участвует в гемостазе.\n• Активация меняет его форму.",
            "plasma"=>"• Жидкая среда крови.\n• Вода, белки, электролиты.\n• Транспортирует вещества.",
            "hemoglobin"=>"• Белок с четырьмя субъединицами.\n• Гем содержит железо Fe²⁺.\n• O₂ связывается обратимо.",
            "heme"=>"• Гем связан с гемоглобином.\n• Его железо — Fe²⁺.\n• O₂ связывается с этим центром.",
            "oxygen"=>"• O₂ переносится главным образом Hb.\n• Связь с гемом обратима.\n• В тканях O₂ освобождается.",
            "co2"=>"• CO₂ образуется в тканях.\n• Основной перенос — HCO₃⁻.\n• Часть связана с белками крови.",
            _=>"Изучи выбранный элемент потока."
        };
        static string ShortExplanation(int scene)=>scene switch
        {
            3=>"Эритроциты переносят кислород.\nЛейкоциты защищают организм.\nТромбоциты закрывают повреждения.",
            4=>"Эндотелий контактирует с кровью.\nМедиа управляет тонусом сосуда.\nНаружный слой поддерживает стенку.",
            5=>"Небольшое изменение радиуса\nсильно влияет на кровоток.\nМеняй один параметр и сравнивай.",
            6=>"Растущая масса сужает просвет.\nПоток и доставка кислорода падают.\nРаздроби все элементы до окклюзии.",
            7=>"Пузырёк переносится кровотоком.\nВ узком участке он мешает потоку.\nПоле позволяет удержать пузырёк.",
            8=>"Вирус — не клетка. Он использует\nклетку хозяина для размножения.\nИсследуй его, затем найди фагоцит.",
            9=>"Фагоцит — один из типов\nлейкоцитов. Используй изученный\nцикл взаимодействия в потоке.",
            10=>"Иммунитет контролирует свои ткани.\nИзменённые клетки могут быть распознаны.\nКонтакт NK/T-клеток запускает апоптоз.",
            11=>"Целостность стенки нарушена.\nУтечка ↑ • обнажён субэндотелий.\nЧасть потока идёт наружу, основная — дальше.",
            12=>"Адгезия образует первичную пробку.\nАктивация и агрегация её усиливают.\nФибрин стабилизирует; утечка снижается.",
            13=>"Недостаток пробки оставляет утечку.\nИзбыток перекрывает просвет.\nРабочий результат — равновесие.",
            14=>"Фагоцит очищает клеточный детрит.\nПлазмин удаляет фибрин.\nЭндотелий восстанавливает стенку.",
            15=>"Антитело узнаёт подходящий эпитоп.\nФагоцит удаляет связанный комплекс.\nT-клетка устраняет источник инфекции.",
            16=>"Сравни исходные и итоговые данные.\nПроверь стенку, поток и защиту.\nГомеостаз — согласованная работа системы.",
            _=>"Исследование завершено.\nСравни результаты и повтори\nнаблюдения в свободном режиме."
        };
        static string MiniDiagram(int scene)=>scene switch
        {3=>"Hb + O₂ ⇄ HbO₂",4=>"ЭНДОТЕЛИЙ · МЕДИА",5=>"Q ∝ ΔP · r⁴",6=>"ПОКРЫШКА / ЛИПИДЫ",7=>"ПОТОК → ЛОВУШКА",8=>"РАСПОЗНАТЬ → НАПРАВИТЬ → ПОГЛОТИТЬ",9=>"ОКРУЖЕНИЕ → ПОГЛОЩЕНИЕ",10=>"СИГНАЛ → КОНТАКТ → АПОПТОЗ",11=>"СТЕНКА → ПОТОК",12=>"ТРОМБОЦИТЫ + ФИБРИН",13=>"УТЕЧКА ← БАЛАНС → ПРОСВЕТ",14=>"ОЧИСТКА → REPAIR",15=>"Ag + Ab ⇄ AgAb",_=>"ПОТОК · ЗАЩИТА · БАЛАНС"};
        static string LessonDetails(int scene)=>scene switch
        {
            3=>"<b>Как исследовать</b>\nПросканируй выбранный образец: здесь появятся его строение, функции и связанные темы. Полная теория сохраняется после отпускания клетки. Можно прокручивать текст и вернуться к краткой карточке, не теряя прогресс.",
            4=>"<b>Интима</b>\nЭндотелий образует внутренний барьер и регулирует обмен между кровью и тканями.\n\n<b>Медиа</b>\nГладкие мышечные клетки и эластические структуры изменяют тонус и поддерживают стенку.\n\n<b>Адвентиция</b>\nСоединительная ткань укрепляет сосуд; у крупных сосудов содержит собственные сосуды и нервы.",
            5=>"<b>Давление и расход</b>\nДвижение крови обусловлено градиентом давления. Сопротивление зависит от геометрии сосуда. В идеализированной ламинарной модели R ∝ 1/r⁴.\n\n<b>Условия модели</b>\nПри фиксированном ΔP расширение повышает расход; при фиксированном расходе скорость меняется обратно площади сечения. Эти условия нельзя смешивать. Живые сосуды регулируются сложнее, чем учебный участок.",
            6=>"<b>Атеросклероз</b>\nЛипиды и воспалительные клетки накапливаются в стенке. Бляшка уменьшает просвет и может ограничивать доставку крови.\n\n<b>Фиброзная покрышка</b>\nОтделяет липидное ядро от крови. Её повреждение может запускать тромбообразование. Импульсное удаление элементов здесь — условная игровая модель, не клинический метод лечения.",
            7=>"<b>Газовый эмбол</b>\nГазовый пузырёк переносится потоком и способен перекрывать более узкие сосуды. Опасность зависит от места и объёма газа.\n\n<b>Игровая задача</b>\nПоле удерживает пузырёк, а кольцо обозначает учебную ловушку. Нельзя оставлять движущийся эмбол свободным внутри кровотока.",
            8 or 9=>"<b>Вирус и клетка</b>\nВирусная частица содержит генетический материал и белковую оболочку, но не является клеткой. Для размножения нужна клетка хозяина.\n\n<b>Фагоцитоз</b>\nНекоторые лейкоциты распознают, окружают и поглощают чужеродный материал. После поглощения он разрушается внутри клетки. Настоящий противовирусный ответ также включает интерфероны, антитела и цитотоксические клетки.",
            10=>"<b>Иммунный надзор</b>\nКлетки организма могут получать повреждения и изменения ДНК. Большинство нарушений устраняется естественными механизмами. NK-клетки и цитотоксические лимфоциты способны распознавать некоторые изменённые клетки.\n\n<b>Путь в ткань</b>\nСигналы направляют иммунные клетки к участку. Лейкоциты взаимодействуют с эндотелием и проходят через стенку — диапедез.\n\n<b>Апоптоз</b>\nПри цитотоксическом контакте перфорин/гранзимы могут запускать контролируемую гибель клетки. Reverse визуализирует направляющий сигнал, а не реальное дистанционное управление иммунитетом.",
            11=>"<b>Разрыв стенки</b>\nПовреждение эндотелия обнажает структуры под ним. Часть крови выходит из сосуда, а остальная продолжает движение.\n\n<b>Диагностика</b>\nНужно сопоставить дефект стенки, наружную утечку и снижение давления. Это разные наблюдения одного процесса, а не три разных повреждения.",
            12=>"<b>Адгезия</b>\nТромбоциты прикрепляются к повреждённой поверхности при участии коллагена и фактора фон Виллебранда.\n\n<b>Активация и агрегация</b>\nКлеточные фрагменты меняют форму и выделяют сигналы; фибриноген связывает активированные тромбоциты.\n\n<b>Фибрин</b>\nТромбин превращает фибриноген в фибрин, укрепляющий первичную пробку. Число 11 и активация Scanner — учебная условность, а не реальное число тромбоцитов или механизм прибора.",
            13=>"<b>Баланс гемостаза</b>\nНедостаточная пробка не останавливает потерю крови. Чрезмерное тромбообразование может сужать просвет и уменьшать снабжение тканей.\n\n<b>Цель</b>\nЗакрыть дефект, сохранив движение крови. В организме этот баланс обеспечивают сосудистые, тромбоцитарные, коагуляционные и противосвёртывающие механизмы.",
            14=>"<b>Очистка</b>\nФагоциты удаляют клеточный детрит.\n\n<b>Фибринолиз</b>\nПлазмин расщепляет фибрин, когда временный каркас больше не нужен.\n\n<b>Репарация</b>\nКлетки эндотелия восстанавливают барьер. Остановка кровотечения и восстановление стенки — разные этапы.",
            15=>"<b>Специфичность</b>\nАнтитело узнаёт определённый участок антигена — эпитоп. Подходящая форма связывания помогает нейтрализовать материал и облегчает его удаление фагоцитами.\n\n<b>Источник инфекции</b>\nЦитотоксические T-лимфоциты распознают инфицированные клетки и могут запускать апоптоз. Удаление частиц и устранение клетки-источника — разные задачи.",
            16=>"<b>Гомеостаз</b>\nСравни давление, температуру, кровоток, целостность стенки, вирусную нагрузку и гемостаз. Восстановление системы подтверждается совокупностью показателей, а не одним числом.",
            _=>"Свяжи изученные клеточные процессы с состоянием организма. Исследованные темы доступны в учебных карточках."
        };
    }
}
