using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    [DefaultExecutionOrder(1900)]
    public sealed class JourneyArcadeHud:MonoBehaviour
    {
        public JourneyWorld world;
        public JourneyHolographicScreens screens;
        float refresh;
        void LateUpdate()
        {
            var m=world.mission;if(m.Goals==null||Time.time<refresh)return;refresh=Time.time+.1f;
            var goal=m.Current;bool complete=m.Complete;
            screens.pageLabel.text=complete?"ЭТАП ГОТОВ":$"ЭТАП {m.SceneNumber-2:00} · ДЕЙСТВУЙ";
            if(m.SceneNumber==3)
            {
                var held=world.freeTools.Held;screens.theoryTitle.text=held?held.targetId=="rbc"?"ОБРАЗЕЦ RBC":held.label.ToUpperInvariant():"ПОЙМАЙ ОБРАЗЕЦ";
                screens.shortTheory.text=held?"Образец удерживается полем.\nЛевый курок — проверить.\nОтпусти правый — вернуть в кровь.":"ПРИТЯЖЕНИЕ + правый курок.\nO₂ — две сферы, CO₂ — три.\nПроверь пять типов крови.";
                screens.taskText.text=complete?"Все типы проверены. ДАЛЕЕ →":"Найди ещё не изученный тип.";screens.taskProgress.text=$"{world.research.RequiredInvestigated} / 5";
                screens.illustrationText.text=held?"ЗАХВАТ → SCANNER → ОТПУСТИТЬ":"КЛЕТКИ · ПЛАЗМА · СВЯЗАННЫЙ ГРУЗ";
            }
            else if(m.SceneNumber==10&&world.immunePatrol)
            {
                var patrol=world.immunePatrol;screens.theoryTitle.text=complete?"ИММУННЫЙ НАДЗОР ЗАВЕРШЁН":"ИММУННЫЙ ПАТРУЛЬ";
                screens.shortTheory.text=complete?"Иммунитет контролирует ткани.\nАномальные клетки устранены.\nСтенка возвращается в норму.":m.StepIndex==0?"На стенке изменённый участок.\nНайди его и проверь Scanner.":m.StepIndex==1?"NK / T-клетки распознают аномалии.\nКонтакт запускает апоптоз.\nОтпусти Scanner; нажми снова.":patrol.CurrentFocus?"Лейкоциты выходят из крови.\nКонтакт → апоптоз → очистка.\nПЕРФОРИН / ГРАНЗИМЫ.":"РЕВЕРС: наведи на очаг.\nУдержи направляющий сигнал.\nКлетки выполнят ответ сами.";
                screens.taskText.text=complete?"Надзор завершён. ДАЛЕЕ →":m.StepIndex==0?"Найди изменённую структуру стенки.":m.StepIndex==1?"Повторный Scanner: глубокий просмотр.":patrol.CurrentFocus?"Наблюдай иммунный ответ.":"Выбери любой из оставшихся очагов.";
                screens.taskProgress.text=$"ОЧАГИ {patrol.Treated} / 6";screens.illustrationText.text="ОБНАРУЖИТЬ → НАПРАВИТЬ → АКТИВИРОВАТЬ → УСТРАНИТЬ";
            }
            else if(m.SceneNumber==5||m.SceneNumber==10)
            {
                screens.theoryTitle.text="УПРАВЛЯЙ СОСУДОМ";screens.shortTheory.text=world.hemodynamics.Critical?"ПАРАМЕТРЫ В КРАСНОЙ ЗОНЕ.\nСкорректируй диаметр полем.":goal?.action==StudyAction.Scan?"Левый курок — измерить стенку.\nПосле проверки используй поле.":"Правый курок — держать поле.\nРука вверх — шире. Вниз — уже.";
                screens.taskText.text=complete?"Участок проверен. ДАЛЕЕ →":goal?.target=="hdx-wide"?"Расширь участок.":goal?.target=="hdx-critical"?"Проверь предел расширения.":goal?.target=="hdx-normal"?"Верни зелёный диапазон.":"Проверь участок Scanner.";
            }
            else if(m.SceneNumber==6)
            {screens.theoryTitle.text="ОСВОБОДИ ПОТОК";screens.shortTheory.text=goal?.action==StudyAction.Scan?"Проверь бляшку левым курком.":"ИМПУЛЬС — разбивай липиды.\nВеди прицел по тёплым мишеням.";screens.taskText.text=complete?"Просвет свободен. ДАЛЕЕ →":"Не дай массе закрыть просвет.";}
            else if(m.SceneNumber==7)
            {screens.theoryTitle.text="ЛОВИ ЭМБОЛЫ";screens.shortTheory.text="ПРИТЯЖЕНИЕ — правый курок.\nДержи пузырёк в прицеле.\nПеренеси в кольцо.";screens.taskText.text=complete?"Все пузырьки в ловушке. ДАЛЕЕ →":"Не теряй наведение.";screens.taskProgress.text=$"{world.embolusWave.StabilizedCount} / 6";}
            else if(m.SceneNumber==8||m.SceneNumber==9||m.SceneNumber==15&&goal?.target=="immune-engulf")
            {
                screens.theoryTitle.text=m.SceneNumber==9?"НОВАЯ ВОЛНА":complete?"ВИРУС РАЗРУШЕН":"ЗАПУСТИ ФАГОЦИТ";
                screens.shortTheory.text=complete?"Оболочка разрушена.\nДАЛЕЕ — следующая волна.":world.plateletLaunch?"ПРИТЯЖЕНИЕ → SCANNER\nScanner активирует клетку.\nРЕВЕРС: наведи и отпусти.":"ЗАХВАТ → SCANNER → АКТИВАЦИЯ\nРЕВЕРС: удержи и наведи.\nОтпусти курок в цель.";
                screens.taskText.text=complete?"Этап завершён. ДАЛЕЕ →":goal?.action==StudyAction.Capture?"Притяни свежую клетку.":goal?.action==StudyAction.Scan?"Проверь удерживаемый заряд.":goal?.action==StudyAction.Activate?"Активируй клетку правым курком.":"Попади в подвижную цель.";
                screens.illustrationText.text=m.SceneNumber==8?$"УЧЕБНЫЙ ВИРУС · {world.immunity.TutorialVirus.Integrity} ПОПАДАНИЕ":"ОТРОСТКИ 3 → 2 → 1 → РАЗРУШЕН";
            }
            else if(m.SceneNumber==11&&world.plateletLaunch)
            {screens.theoryTitle.text=complete?"ПОВРЕЖДЕНИЕ ПОДТВЕРЖДЕНО":m.StepIndex==0?"1 · НАЙДИ РАЗРЫВ":m.StepIndex==1?"2 · ИЗМЕРЬ УТЕЧКУ":"3 · ПРОВЕРЬ ДАВЛЕНИЕ";screens.shortTheory.text=complete?"Причина найдена: разрыв стенки.\nСледующий шаг — тромбоциты.":"Наведи левый Scanner в контур.\nУдержи левый курок.\nПосле измерения отпусти.";screens.taskText.text=complete?"Левая Y → закрыть повреждение.":m.StepIndex==0?"Посмотри на светлый край раны.":m.StepIndex==1?"Повтори сканирование той же раны.":"Ещё одно измерение у разрыва.";screens.taskProgress.text=$"ДИАГНОСТИКА {Mathf.Min(3,m.StepIndex)} / 3";screens.illustrationText.text="РАЗРЫВ → УТЕЧКА → СНИЖЕНИЕ АД";}
            else if(m.SceneNumber==12&&world.plateletLaunch)
            {
                var held=world.freeTools.Held;var platelet=held?held.GetComponent<JourneyPlateletEntity>():null;var launch=world.plateletLaunch;var mode=m.GetComponent<BioToolModeController>().CurrentMode;
                bool stabilizing=goal?.target=="hemostasis-stable";bool aiming=platelet&&platelet.ScannerActivated&&mode==BioToolMode.Reverse;
                screens.theoryTitle.text=complete?"РАНА ЗАКРЫТА":stabilizing?"ФИБРИН УКРЕПЛЯЕТ ПРОБКУ":!platelet?"1 · ПОЙМАЙ ТРОМБОЦИТ":!platelet.ScannerActivated?"2 · АКТИВИРУЙ КЛЕТКУ":"3 · НАПРАВЬ В РАНУ";
                screens.shortTheory.text=complete?"Утечка остановлена.\nДальше — сохранить просвет.":stabilizing?"Тромбоциты уже прикрепились.\nДождись укрепления фибрином.":launch.Flying?"Клетка летит к повреждению.\nНаблюдай результат.":held&&!platelet?"Это другой образец.\nВ ПРИТЯЖЕНИИ отпусти курок.\nВыбери маленький тромбоцит.":!platelet?"A / B → ПРИТЯЖЕНИЕ.\nНаведи на жёлтый тромбоцит.\nДержи правый курок.":!platelet.ScannerActivated?"Продолжай держать образец.\nЛевый Scanner — на клетку.\nУдержи левый курок.":!aiming?"Переключи A / B на РЕВЕРС.\nОбразец останется в поле.":launch.PreviewOnWound?"Конец траектории в ране.\nДержи правый курок для заряда.\nЗатем отпусти его.":"Наведи BioTool внутрь контура.\nТраектория покажет попадание.";
                screens.taskText.text=complete?"Левая Y → проверить баланс.":launch.ResultUntil>Time.time&&!held?launch.ResultMessage:aiming&&launch.PreviewOnWound&&world.freeTools.ReverseCharge>.8f?"ГОТОВО — ОТПУСТИ ПРАВЫЙ КУРОК":!held?"Тромбоциты идут в общем потоке.":!platelet?"Нужен тромбоцит, а не эритроцит.":!platelet.ScannerActivated?"Scanner меняет форму тромбоцита.":"Попадание добавит одну клетку в пробку.";
                screens.taskProgress.text=$"ПРОБКА {world.hemostasisPuzzle.Attached} / {world.hemostasisPuzzle.RequiredPlatelets}";screens.illustrationText.text=stabilizing?$"ФИБРИН {world.FibrinGrowth:P0}":$"УТЕЧКА {world.woundState.LeakStrength:P0}";
            }
            else if(m.SceneNumber==13&&world.plateletLaunch)
            {screens.theoryTitle.text="СОХРАНИ ПРОСВЕТ";screens.shortTheory.text=goal?.action==StudyAction.Scan?"Scanner: проверь собранную пробку.":"ПРИТЯЖЕНИЕ: удержи тромбоцит.\nПеремести лучом и отпусти.\nУ раны он закрепится снова.";screens.taskText.text=complete?"Баланс восстановлен. ДАЛЕЕ →":goal?.label;screens.taskProgress.text=$"ТРОМБОЦИТЫ {world.hemostasisPuzzle.Attached}";}
            screens.taskHint.text=m.Complete?"Левая Y / → — следующий этап":world.mission.GetComponent<BioToolModeController>().Caption;
            screens.pageLabel.text+=$" · v{Application.version}";
            screens.vitalLabels[2].text=world.hemodynamics.Active?"ДИАМЕТР Ø":"SpO₂";screens.vitalLabels[4].text=world.hemodynamics.Active||m.SceneNumber==6?"КРОВОТОК":m.SceneNumber>=11&&m.SceneNumber<=14?"УТЕЧКА":"ВИРУС";
            if(!complete&&m.SceneNumber>=10&&m.SceneNumber<=13)screens.taskText.text=screens.shortTheory.text;
            // This presenter owns the task block. The learning screen owns theory.
            screens.PresentLearning();
        }
    }
}
