using System;
using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    public enum StudyAction{Grab,Scan,Enlarge,Detach,Bind,Activate,Measure,Radius,Pressure,Pulse,Capture,Deposit,Mark,Direct,Balance,Compare,Return}
    [Serializable]public sealed class StudyGoal
    {
        public string label,target;public StudyAction action;public int required=1;
        public StudyGoal(string label,StudyAction action,string target,int required=1){this.label=label;this.action=action;this.target=target;this.required=required;}
    }
    public sealed class JourneyMission : MonoBehaviour
    {
        public JourneyMover mover;
        public JourneyWorld world;
        public int SceneNumber=3;
        public event Action Changed;
        public int StepIndex{get;private set;}
        public int Progress{get;private set;}
        public float Temperature{get;private set;}=36.6f;
        public float PressureSystolic{get;private set;}=120;
        public float PressureDiastolic=>Mathf.Round(Mathf.LerpUnclamped(60,80,(PressureSystolic-92)/28f));
        public float ViralLoad{get;private set;}=0;
        public float ModelRadius{get;private set;}=1;
        public float ModelPressure{get;private set;}=120;
        public float ClotBalance{get;private set;}=.3f;
        public int WrongCapHits{get;private set;}
        public string Feedback{get;private set;}="Сначала наблюдаем, потом делаем вывод.";
        public bool Ready=>mover!=null&&!mover.Moving;
        public bool Complete=>Goals!=null&&StepIndex>=Goals.Count;
        public void RecordHomeostasisMeasurements(){PressureSystolic=120;Temperature=36.8f;}
        public StudyGoal Current=>Complete?null:Goals[StepIndex];
        public string CurrentGoalLabel=>FreeResearchUnlocked&&SceneNumber==18?"Свободное исследование — сканируй модели":SceneNumber==17&&world&&world.Returning?"Переход масштаба — подожди":Victory?"ПОБЕДА — вирусный очаг нейтрализован":Goals==null?"Подготовка исследовательского режима":Complete&&SceneNumber==15?"Дождись завершения фагоцитоза":Complete?"Этап завершён — можно продолжить":Current.label;
        public string LearningTerm{get;private set;}="Гомеостаз";
        public string LearningNote{get;private set;}="Сначала наблюдаем. Потом делаем вывод.";
        public string Formula{get;private set;}="";
        public string ProfessorLine{get;private set;}="Сначала наблюдаем. Потом делаем вывод.";
        public bool FreeResearchUnlocked{get;private set;}
        public bool Victory{get;private set;}
        public int AcceptedActions{get;private set;}
        public int WrongTargetActions{get;private set;}
        public int ScannerActions{get;private set;}
        public int ToolActions{get;private set;}
        public int Retries{get;private set;}
        public int MissedViruses{get;private set;}
        public float ActiveSeconds{get;private set;}
        public float ActionAccuracy=>AcceptedActions+WrongTargetActions==0?1f:AcceptedActions/(float)(AcceptedActions+WrongTargetActions);
        public string PerformanceSummary=>$"Точность действий: {ActionAccuracy:P0}\nОшибочные цели: {WrongTargetActions}\nПовторные попытки: {Retries}\nПропущенные вирионы: {MissedViruses}";
        public string ResultGrades=>$"Исследование: {(world.HomeostasisChecks==6?"A":"B")}\nТочность BioTool: {(ActionAccuracy>=.95f?"A":ActionAccuracy>=.85f?"B+":"B")}\nГемостаз: {(ClotBalance>.5f&&ClotBalance<.72f?"A":"B")}\nСтабильность: {(PressureSystolic>=110&&Temperature<37.2f&&ViralLoad==0?"A":"B")}\n"+PerformanceSummary;
        public string CompactResult=>$"MISSION COMPLETE\nИсследование {(world.HomeostasisChecks==6?"A":"B")} • BioTool {(ActionAccuracy>=.95f?"A":ActionAccuracy>=.85f?"B+":"B")}\nГемостаз {(ClotBalance>.5f&&ClotBalance<.72f?"A":"B")} • Стабильность {(PressureSystolic>=110&&Temperature<37.2f&&ViralLoad==0?"A":"B")}";
         public string ReportText=>"ИЗУЧЕНО\nЭритроцит • гемоглобин • O₂ • CO₂ • лейкоцит • тромбоцит\nЭндотелий • кровоток • бляшка • газовый эмбол\nВирус • антиген • антитело • фагоцитоз • T-лимфоцит\n\nРЕЗУЛЬТАТ\nПовреждение закрыто, кровоток сохранён, вирусная нагрузка 0 %.\nГомеостаз: 72 уд/мин • 120/80 • SpO₂ 98 % • 36,8 °C\n\nКАЧЕСТВО ПРОХОЖДЕНИЯ\n"+PerformanceSummary+"\n\nОТКРЫТ РЕЖИМ СВОБОДНОГО ИССЛЕДОВАНИЯ";
        public float FlowMultiplier=>SceneNumber==5?Mathf.Clamp(ModelFlow,.25f,1.8f):SceneNumber==4?.48f:SceneNumber==8?.42f:SceneNumber==13?ClotBalance>.86f?.38f:ClotBalance<.28f?.65f:1f:SceneNumber==10||SceneNumber==11?.65f:1f;
        public float ModelFlow=>Mathf.Pow(ModelRadius,4)*(ModelPressure/120);
        public List<StudyGoal> Goals{get;private set;}
        bool started;
        float lastToneReport=1;
        void Start(){started=true;ResetRunStatistics();world.ResetJourneyMemory();Enter(3,false);}
        public void ResetRunStatistics(){AcceptedActions=WrongTargetActions=ScannerActions=ToolActions=Retries=MissedViruses=0;ActiveSeconds=0;}
        void Update()
        {
            if(started&&Ready&&!Complete)ActiveSeconds+=Time.deltaTime;
            if(started&&Ready&&SceneNumber==10&&Current?.target=="pressure-return"&&PressureSystolic>92)
            {
                float before=PressureSystolic;PressureSystolic=Mathf.MoveTowards(PressureSystolic,92,Time.deltaTime*3f);
                if(Mathf.FloorToInt(before)!=Mathf.FloorToInt(PressureSystolic))Changed?.Invoke();
            }
        }
        void Goal(string l,StudyAction a,string t,int n=1)=>Goals.Add(new StudyGoal(l,a,t,n));
        public void Enter(int scene,bool travel=true,bool retry=false)
        {
            if(retry)world.ForgetEpisode(scene);else world.RememberEpisode();
            SceneNumber=Mathf.Clamp(scene,3,18);StepIndex=0;Progress=0;Goals=new List<StudyGoal>();Victory=false;
            Feedback="Выполни действия текущего этапа. Следующая локация пока скрыта.";
            SetBriefing(SceneNumber);
            switch(SceneNumber)
            {
                case 3:
                    Goal("Поймай эритроцит в зоне замедления",StudyAction.Grab,"rbc");
                    Goal("Просканируй эритроцит",StudyAction.Scan,"rbc");Goal("Увеличь учебный эритроцит",StudyAction.Enlarge,"rbc");
                    Goal("Изучи гемоглобин: четыре субъединицы",StudyAction.Scan,"hemoglobin");Goal("Просканируй гем и Fe²⁺",StudyAction.Scan,"heme");
                    Goal("Сними молекулу O₂ с гема",StudyAction.Detach,"oxygen");Goal("Верни O₂ в точку связывания",StudyAction.Bind,"oxygen");
                    Goal("Сравни транспорт CO₂",StudyAction.Scan,"co2");Goal("Найди и просканируй лейкоцит",StudyAction.Scan,"leukocyte");
                    Goal("Просканируй тромбоцит в покое",StudyAction.Scan,"platelet");Goal("Сравни с активированной формой",StudyAction.Compare,"platelet");
                    Goal("Изучи белки и ионы плазмы",StudyAction.Scan,"plasma");break;
                case 4:
                    Goal("Сканируй эндотелий",StudyAction.Scan,"endothelium");Goal("Включи послойный режим",StudyAction.Activate,"layers");
                    Goal("Просканируй интиму",StudyAction.Scan,"intima");Goal("Просканируй мышечно-эластическую медиа",StudyAction.Scan,"media");Goal("Просканируй адвентицию",StudyAction.Scan,"adventitia");break;
                case 5:
                    Goal("Сними давление, скорость и площадь",StudyAction.Measure,"flow");Goal("Активируй голографическую модель",StudyAction.Activate,"flow-model");
                    Goal("Уменьши радиус модели",StudyAction.Radius,"small-radius");Goal("Увеличь радиус модели",StudyAction.Radius,"large-radius");
                    Goal("Измени модельное давление",StudyAction.Pressure,"pressure");Goal("Верни нормальные параметры",StudyAction.Activate,"normal-flow");break;
                case 6:
                    WrongCapHits=0;Goal("Обнаружь локальное нарушение потока",StudyAction.Measure,"plaque-flow");Goal("Просканируй бляшку и её состав",StudyAction.Scan,"plaque");
                    Goal("Открой режим BioTool «Импульс»",StudyAction.Activate,"pulse-mode");Goal("Обработай три липидные мишени, сохрани покрышку",StudyAction.Pulse,"lipid",3);break;
                case 7:
                    Goal("Просканируй движущийся пузырёк",StudyAction.Scan,"embolus");Goal("Открой режим «Притяжение»",StudyAction.Activate,"attract-mode");
                    Goal("Захвати пузырёк полем BioTool",StudyAction.Capture,"embolus");Goal("Удерживай Trigger: перенеси пузырёк и стабилизируй в ловушке",StudyAction.Deposit,"embolus");break;
                case 8:
                    Goal("Просканируй неизвестный вирион",StudyAction.Scan,"virus-study");Goal("Увеличь вирус в учебном режиме",StudyAction.Enlarge,"virus-study");
                    Goal("Изучи генетический материал",StudyAction.Scan,"genome");Goal("Изучи капсид",StudyAction.Scan,"capsid");Goal("Найди антиген и эпитоп",StudyAction.Scan,"epitope");break;
                case 9:
                    Temperature=36.6f;ViralLoad=25;Goal("Открой иммунное управление",StudyAction.Activate,"immune-mode");Goal("Распознай и пометь восемь входящих частиц",StudyAction.Mark,"incoming-virus",8);Goal("Передай помеченные частицы фагоциту",StudyAction.Direct,"phagocyte");break;
                case 10:
                    PressureSystolic=92;ModelRadius=lastToneReport=1;world.UpdateExperiment(ModelRadius,ModelPressure);
                    Goal("Измерь падение артериального давления",StudyAction.Measure,"pressure-low");Goal("Включи сосудистое поле",StudyAction.Activate,"tone-mode");
                    Goal("Удерживай Trigger: опусти BioTool и удержи умеренный тонус",StudyAction.Radius,"tone");Goal("Замерь временную стабилизацию",StudyAction.Measure,"pressure-stable");Goal("Подтверди повторное падение и найди причину",StudyAction.Measure,"pressure-return");break;
                case 11:
                    Goal("Сканером определи повреждение эндотелия",StudyAction.Scan,"wound");Goal("BioTool измерь утечку крови",StudyAction.Measure,"leak");Goal("HUD подтверди снижение давления",StudyAction.Measure,"pressure-low");break;
                case 12:
                    Goal("Возьми тромбоцит и прикрепи к exposed wound",StudyAction.Direct,"platelet");Goal("Активируй первый прикреплённый тромбоцит",StudyAction.Activate,"platelet");
                    Goal("Добавь пять тромбоцитов руками: агрегация",StudyAction.Direct,"platelet",5);Goal("Сканируй доступные факторы: найди тромбин",StudyAction.Scan,"thrombin");
                    Goal("Активируй тромбин: фибриноген → фибрин",StudyAction.Activate,"thrombin");Goal("Протяни три нити между парными anchors вокруг пробки",StudyAction.Direct,"fibrin",3);break;
                case 13:
                    Goal("Просканируй собранную пробку",StudyAction.Scan,"clot");Goal("Убери часть тромбоцитов/нитей: проверь UNDER",StudyAction.Balance,"insufficient");Goal("Добавь резервные тромбоциты: проверь OVER",StudyAction.Balance,"excessive");
                    Goal("Скорректируй пробку руками: закрой дефект и сохрани просвет",StudyAction.Balance,"optimal");Goal("Подтверди сохранённый кровоток",StudyAction.Measure,"open-flow");break;
                case 14:
                    Goal("Распознай три разных фрагмента debris сканером",StudyAction.Scan,"debris",3);Goal("Включи помощь фагоцита",StudyAction.Activate,"cleanup-mode");Goal("Наведи BioTool на каждый распознанный debris",StudyAction.Direct,"debris",3);
                    Goal("Найди плазмин: фактор удаления фибрина",StudyAction.Scan,"plasmin");Goal("Активируй фибринолиз",StudyAction.Activate,"plasmin");Goal("После удаления фибрина запусти repair",StudyAction.Activate,"repair");Goal("Просканируй восстановленный эндотелий",StudyAction.Scan,"healed-wall");break;
                case 15:
                    Temperature=38.3f;ViralLoad=100;Goal("Самостоятельно найди и просканируй клетку-источник",StudyAction.Scan,"infected-cell");Goal("Окна подсветки: pulse для защищённых целей, затем mark четырёх вирионов",StudyAction.Mark,"incoming-virus",4);
                    Goal("Найди эпитоп сканером",StudyAction.Scan,"epitope");Goal("Выбери комплементарное антитело",StudyAction.Bind,"antibody-A");
                    Goal("Направь фагоцит к связанному antigen-antibody комплексу",StudyAction.Direct,"neutralized");
                    Goal("Направь T-лимфоцит к инфицированной клетке",StudyAction.Direct,"t-cell");Goal("Очисти три оставшихся комплекса",StudyAction.Direct,"remaining",3);break;
                case 16:
                    Goal("Измерь восстановленное артериальное давление",StudyAction.Measure,"check-pressure");Goal("Измерь температуру",StudyAction.Measure,"check-temperature");Goal("Измерь сохранённый кровоток",StudyAction.Measure,"check-flow");
                    Goal("Сканером проверь целостность восстановленной стенки",StudyAction.Scan,"check-wall");Goal("Сканером проверь отсутствие вирусной нагрузки",StudyAction.Scan,"check-virus");Goal("Проверь стабильный гемостаз и открытый просвет",StudyAction.Measure,"check-hemostasis");break;
            }
            if(travel)mover.TravelTo(SceneNumber);else mover.TravelTo(SceneNumber,true);
            world.ShowScene(SceneNumber);
            Changed?.Invoke();
        }
        void SetBriefing(int scene)
        {
            LearningTerm="Гомеостаз";LearningNote="Сначала наблюдаем. Потом делаем вывод.";Formula="";ProfessorLine="Сначала наблюдаем. Потом делаем вывод.";
            switch(scene)
            {
                case 3:LearningTerm="Форменные элементы крови";LearningNote="Эритроциты переносят O₂ и часть CO₂; лейкоциты участвуют в защите; тромбоциты помогают остановить кровотечение.";Formula="Hb + O₂ ⇄ HbO₂";ProfessorLine="Сравни объект, его функцию и место в общей системе.";break;
                case 4:LearningTerm="Эндотелий и стенка артерии";LearningNote="Эндотелий контактирует с кровью; медиа меняет тонус; наружные слои поддерживают сосуд.";ProfessorLine="Сосуд — это живая ткань, а не пустая трубка.";break;
                case 5:LearningTerm="Гемодинамика";LearningNote="Радиус особенно сильно влияет на поток: небольшое сужение заметно меняет пропускную способность.";Formula="Q = vS   •   Q ∝ ΔP·r⁴/(ηL)";ProfessorLine="Проверь закономерность сначала на модели, затем сравни с наблюдаемым потоком.";break;
                case 6:LearningTerm="Атеросклеротическая бляшка";LearningNote="LDL и воспаление формируют липидное ядро; фиброзная покрышка удерживает его внутри стенки.";Formula="Повреждение покрышки → тромбоциты → фибрин → окклюзия";ProfessorLine="Точность важнее силы: работай только по липидным мишеням.";break;
                case 7:LearningTerm="Газовый эмбол";LearningNote="Пузырёк может перемещаться с кровотоком и застрять там, где просвет становится уже.";ProfessorLine="Опасность определяется не размером объекта, а тем, где он окажется.";break;
                case 8:LearningTerm="Строение вирусной частицы";LearningNote="Сначала различи генетический материал, капсид и поверхностный антиген — атака без распознавания неточна.";Formula="Антиген ⊃ эпитоп";ProfessorLine="Не атакуй неизвестное. Получи данные.";break;
                case 9:LearningTerm="Иммунный ответ";LearningNote="Распознавание и маркировка позволяют иммунной клетке забрать частицу; пропуски повышают температуру.";Formula="39,5 °C — критический игровой предел";ProfessorLine="Сохрани контроль над волной: правильное действие снижает нагрузку.";break;
                case 10:LearningTerm="Сосудистый тонус";LearningNote="Умеренное сужение временно поддерживает давление, но не устраняет причину его падения.";Formula="92/60 → 100/66 → поиск утечки";ProfessorLine="Симптом можно временно стабилизировать, но причину нужно найти.";break;
                case 11:LearningTerm="Повреждение эндотелия";LearningNote="Разрыв стенки объясняет одновременно кровопотерю и падение давления.";Formula="Дефект стенки + утечка → снижение объёма кровотока";ProfessorLine="Три независимых наблюдения должны привести к одному выводу.";break;
                case 12:LearningTerm="Гемостаз";LearningNote="Адгезия и активация тромбоцитов создают первичную пробку; тромбин запускает сеть фибрина.";Formula="Фибриноген —(тромбин)→ фибрин";ProfessorLine="Пробка должна расти поэтапно, а не возникать мгновенно.";break;
                case 13:LearningTerm="Баланс гемостаза";LearningNote="Недостаточная пробка пропускает кровь, избыточная перекрывает просвет; нужен рабочий оптимум.";Formula="Кровотечение остановлено + поток сохранён";ProfessorLine="Хороший результат — это баланс, а не максимальное значение шкалы.";break;
                case 14:LearningTerm="Фагоцитоз и восстановление";LearningNote="Фагоцит убирает детрит, после чего эндотелий может закрыть дефект.";Formula="Очистка → миграция клеток → восстановленная стенка";ProfessorLine="Сначала убираем последствия повреждения, затем восстанавливаем ткань.";break;
                case 15:LearningTerm="Иммунная специфичность";LearningNote="Антитело связывает подходящий эпитоп; фагоцит удаляет комплекс; T-лимфоцит устраняет источник новых вирусов.";Formula="Ag + Ab ⇄ AgAb";ProfessorLine="Финальный очаг — это инфицированная клетка и её потомство, а не один гигантский вирус.";break;
                case 16:LearningTerm="Восстановленный гомеостаз";LearningNote="Сравни исходные и финальные значения: система вернулась к рабочему диапазону.";Formula="72 • 120/80 • 98 % • 36,8 °C";ProfessorLine="Сравни с исходным состоянием и объясни, что именно его восстановило.";break;
                case 17:LearningTerm="Смена масштаба";LearningNote="Микромир не исчезает — меняется масштаб наблюдения, чтобы связать клеточные процессы с организмом.";ProfessorLine="Исследование завершено. Теперь соберём отчёт.";break;
                case 18:LearningTerm="Финальный отчёт";LearningNote=ReportText;Formula="";ProfessorLine="Ты прошёл путь от наблюдения к объяснению и восстановил гомеостаз.";break;
            }
        }
        public void Continue()
        {
            if(!Complete){Feedback="Сначала заверши действия: "+CurrentGoalLabel;Changed?.Invoke();return;}
            if(mover.Moving)return;
            if(SceneNumber==17&&world!=null&&world.Returning){Feedback="Подожди завершения перехода масштаба в лабораторию.";Changed?.Invoke();return;}
             if(SceneNumber<15)Enter(SceneNumber+1);
             else if(SceneNumber==15&&Victory)Enter(16,false);
             else if(SceneNumber==16)Enter(17,false);
             else if(SceneNumber==17){FreeResearchUnlocked=true;Enter(18,false);}
             else {Feedback="Исследование завершено. Можно повторно изучать объекты.";Changed?.Invoke();}
        }
        public void DebugSetScene(int scene)
        {
            if(!mover)return;
            Enter(scene,false);
        }
        public void DebugRestartStage()
        {
            if(mover&&mover.Moving)return;
            world.ForgetEpisode(SceneNumber);world.ResetCheckpoint(SceneNumber);Retries++;
            Enter(SceneNumber,false,true);
            Feedback="Этап перезапущен. Выполни действие, указанное в панели.";
            Changed?.Invoke();
        }
        public void DebugPerformCurrent()
        {
            if(!Ready){Feedback="Подожди завершения перехода к станции.";Changed?.Invoke();return;}
            if(Complete){Feedback="Все действия этапа выполнены. Нажми «Продолжить».";Changed?.Invoke();return;}
            if(world)world.Primary();
        }
        public bool Accept(StudyAction action,string target)
        {
            if(!started||!Ready||Complete||Victory)return false;
            if(SceneNumber==6&&action==StudyAction.Pulse&&target=="cap")
            {
                WrongCapHits++;WrongTargetActions++;Feedback=$"Покрышка повреждена ({WrongCapHits}/3). Возможен тромбоз.";world.ShowCapDamage(WrongCapHits);
                if(WrongCapHits>=3)Checkpoint("Окклюзия: возврат к контрольной точке перед бляшкой.");Changed?.Invoke();return false;
            }
            if(action==StudyAction.Bind&&target.StartsWith("antibody")&&target!="antibody-A"){WrongTargetActions++;Temperature+=.2f;Feedback="Это антитело не комплементарно эпитопу. Выбери другую форму.";Changed?.Invoke();return false;}
            if(Current.action!=action||Current.target!=target){WrongTargetActions++;Feedback="Сейчас требуется: "+Current.label;Changed?.Invoke();return false;}
            // Geometry-dependent actions finish only after their motion/trigger result.
            if(!world.CanApply(action,target)){WrongTargetActions++;Feedback=SceneNumber==10&&target=="pressure-return"?"АД ещё снижается: наблюдай динамику и повтори измерение.":"Сначала захвати объект или доведи его в требуемую зону.";Changed?.Invoke();return false;}
            world.Apply(action,target,Progress);
            var instrumentFeedback=GetComponent<JourneyToolFeedback>();if(instrumentFeedback)instrumentFeedback.Confirm(action,target);
            AcceptedActions++;if(action==StudyAction.Scan)ScannerActions++;else if(action!=StudyAction.Grab)ToolActions++;
            Progress++;
            if(SceneNumber==9&&action==StudyAction.Mark){Temperature=Mathf.Min(38.1f,Temperature+.14f);ViralLoad=Mathf.Max(0,ViralLoad-3);}
            if(SceneNumber==9&&target=="phagocyte")ViralLoad=0;
            if(SceneNumber==10&&target=="pressure-stable")PressureSystolic=100;
            if(SceneNumber==10&&target=="pressure-return")PressureSystolic=92;
            if(SceneNumber==15)
            {
                if(action==StudyAction.Mark)ViralLoad=Mathf.Max(72,ViralLoad-7);
                if(target=="neutralized")ViralLoad=48;
                if(target=="t-cell")ViralLoad=15;
                if(target=="remaining")ViralLoad=Mathf.Max(0,ViralLoad-5);
            }
            Feedback="Выполнено: "+Current.label;
            if(Progress>=Current.required){StepIndex++;Progress=0;}
            if(Complete)
            {
                if(SceneNumber==14){PressureSystolic=120;Temperature=36.8f;}
                if(SceneNumber==15)ConfirmVictory();
                if(SceneNumber==16)Feedback="MISSION COMPLETE\n"+ResultGrades;
                if(SceneNumber!=16)Feedback=SceneNumber==15?(Victory?"ПОБЕДА. Источник устранён, вирусная нагрузка 0 %, поток стабилен.":"Фагоциты завершают поглощение оставшихся комплексов."):"Все действия этапа выполнены. Продолжить →";
            }
            world.RememberEpisode();world.PrepareGoal(Current);Changed?.Invoke();return true;
        }
        public void SetRadius(float value)
        {
            ModelRadius=Mathf.Clamp(value,.5f,1.5f);world.UpdateExperiment(ModelRadius,ModelPressure);
            if(SceneNumber==5&&Current?.action==StudyAction.Radius)
            {if(Current.target=="small-radius"&&ModelRadius<.82f)Accept(StudyAction.Radius,"small-radius");else if(Current.target=="large-radius"&&ModelRadius>1.18f)Accept(StudyAction.Radius,"large-radius");}
            else if(SceneNumber==10&&Current?.target=="tone"&&ModelRadius>.70f&&ModelRadius<.92f){PressureSystolic=100;Accept(StudyAction.Radius,"tone");}
            Changed?.Invoke();
        }
        public void UpdateToneFromHand(float radius)
        {
            if(SceneNumber!=10||Current?.target!="tone"||!Ready)return;
            ModelRadius=Mathf.Clamp(radius,.55f,1.15f);world.UpdateExperiment(ModelRadius,ModelPressure);
            PressureSystolic=ModelRadius>=.72f?Mathf.Lerp(92,100,Mathf.InverseLerp(1,.88f,ModelRadius)):100-(.72f-ModelRadius)*90;
            if(Mathf.Abs(lastToneReport-ModelRadius)>=.01f){lastToneReport=ModelRadius;Changed?.Invoke();}
        }
        public void WarnExcessiveTone()
        {WrongTargetActions++;Feedback="Чрезмерное сужение ограничивает поток. Подними BioTool и верни умеренный тонус.";Changed?.Invoke();}
        public void RecordPhysicalError(string text){WrongTargetActions++;Feedback=text;Changed?.Invoke();}
        public void RecordManualPulse(){AcceptedActions++;ToolActions++;Feedback="Защита вириона снята импульсом. Теперь распознай и пометь цель в окно.";Changed?.Invoke();}
        public void SetPressure(float value){ModelPressure=Mathf.Clamp(value,60,170);world.UpdateExperiment(ModelRadius,ModelPressure);if(SceneNumber==5&&Current?.action==StudyAction.Pressure&&Mathf.Abs(ModelPressure-120)>15)Accept(StudyAction.Pressure,"pressure");Changed?.Invoke();}
        public void SetBalance(float value)
        {
            if(SceneNumber==13&&world.hemostasisPuzzle){FeedbackMessage("Физический баланс: меняй тромбоциты и нити руками.");return;}
            ClotBalance=Mathf.Clamp01(value);world.UpdateClot(ClotBalance);
            if(SceneNumber==13&&Current?.action==StudyAction.Balance)
            {
                var key=value<.28f?"insufficient":value>.86f?"excessive":value>.5f&&value<.72f?"optimal":"";
                if(key==Current.target)Accept(StudyAction.Balance,key);
            }
            Changed?.Invoke();
        }
        public void UpdatePhysicalClotBalance(float value)
        {
            if(SceneNumber!=13)return;float before=ClotBalance;ClotBalance=Mathf.Clamp01(value);world.UpdateClot(ClotBalance);
            PressureSystolic=ClotBalance<.28f?90:ClotBalance>.86f?96:112;
            if(Mathf.Abs(before-ClotBalance)>.005f)Changed?.Invoke();
        }
        public void MissVirus(){MissedViruses++;WrongTargetActions++;Temperature+=.20f;ViralLoad=Mathf.Min(100,ViralLoad+5);Feedback="Частица пропущена: температура растёт.";if(Temperature>=39.5f)Checkpoint("Лихорадочная реакция выше игрового порога: контрольная точка.");Changed?.Invoke();}
        public void RetryEmbolus(bool escaped)
        {
            if(SceneNumber!=7||Goals==null||Complete)return;
            StepIndex=Mathf.Min(StepIndex,2);Progress=0;
            WrongTargetActions++;if(escaped)Retries++;
            Feedback=escaped?"Эмбол ушёл с потоком. Локальная контрольная точка: захвати снова.":"Поле отпущено — пузырёк снова уносит поток. Удерживай Trigger после захвата.";
            world.RememberEpisode();world.PrepareGoal(Current);Changed?.Invoke();
        }
        public void Checkpoint(string message){Retries++;int scene=SceneNumber;world.ResetCheckpoint(scene);Enter(scene,false,true);Feedback=message;Changed?.Invoke();}
        public void FeedbackMessage(string message){Feedback=message;Changed?.Invoke();}
        public void ConfirmVictory()
        {
            if(SceneNumber!=15||!Complete||Victory||!world.ResidualsCleared)return;
            ViralLoad=0;Temperature=36.8f;Victory=true;world.RememberEpisode();world.ShowVictory();Changed?.Invoke();
        }
        public void FreeInspect(string id)
        {
            if(!FreeResearchUnlocked)return;
            switch(id)
            {
                case "rbc":LearningTerm="Эритроцит";LearningNote="Безъядерная клетка с двояковогнутой формой; переносит кислород благодаря гемоглобину.";Formula="Hb + O₂ ⇄ HbO₂";break;
                case "leukocyte":LearningTerm="Лейкоцит";LearningNote="Клетка иммунной системы, распознающая и устраняющая угрозы.";break;
                case "platelet":LearningTerm="Тромбоцит";LearningNote="Клеточный фрагмент, который прикрепляется к повреждённой стенке и участвует в гемостазе.";break;
                case "plaque":LearningTerm="Атеросклеротическая бляшка";LearningNote="Липидное ядро, воспалительные клетки и фиброзная покрышка сужают просвет сосуда.";break;
                case "embolus":LearningTerm="Газовый эмбол";LearningNote="Пузырёк газа, который способен нарушить кровоток в более узком участке.";break;
                case "virus-study":LearningTerm="Вирусная частица";LearningNote="Генетический материал окружён капсидом; поверхностные структуры участвуют в распознавании.";break;
                case "t-cell":LearningTerm="Цитотоксический T-лимфоцит";LearningNote="Распознаёт инфицированную клетку и запускает контролируемое устранение источника инфекции.";break;
                default:LearningTerm=id;LearningNote="Объект доступен для повторного изучения в свободном режиме.";break;
            }
            Feedback="Свободное исследование: карточка добавлена в журнал.";Changed?.Invoke();
        }
    }
}
