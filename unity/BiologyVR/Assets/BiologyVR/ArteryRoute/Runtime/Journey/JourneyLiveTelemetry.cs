using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    [DefaultExecutionOrder(1800)]
    public sealed class JourneyLiveTelemetry:MonoBehaviour
    {
        public JourneyWorld world;
        public JourneyHolographicScreens screens;
        public Text assistance;
        public Image alarm;
        public Text modeLabel;
        public float IdleSeconds{get;private set;}
        int scene=-1,step=-1,progress=-1,research=-1;
        float pressure=120,pulse=72,oxygen=98,flow=1,temperature=36.6f;
        JourneyArcadeHud episodePresenter;
        void Awake(){episodePresenter=GetComponent<JourneyArcadeHud>();}
        public bool Critical{get;private set;}
        void LateUpdate()
        {
            var m=world.mission;if(m.Goals==null)return;
            if(scene!=m.SceneNumber||step!=m.StepIndex||progress!=m.Progress||research!=world.research.RequiredInvestigated){IdleSeconds=0;scene=m.SceneNumber;step=m.StepIndex;progress=m.Progress;research=world.research.RequiredInvestigated;}
            else if(m.Ready&&!m.Complete)IdleSeconds+=Time.deltaTime;
            var h=world.hemodynamics;var plaque=m.SceneNumber==6?world.plaqueChallenge:null;
            bool wound=m.SceneNumber>=11&&m.SceneNumber<=14;
            bool field=(h.Active||world.mission.GetComponent<BioToolModeController>().CurrentMode==BioToolMode.Tonus)&&!(world.immunePatrol&&world.immunePatrol.Active)&&!wound;float obstruction=plaque?plaque.Obstruction:0;
            float targetPressure=field?(h.Late?92:120)/Mathf.Pow(h.RadiusRatio,2):plaque?120+obstruction*65:m.PressureSystolic;
            float targetFlow=field?h.FlowRatio:plaque?plaque.FlowFraction:1;
            float stress=field?Mathf.Clamp01(Mathf.Abs(h.RadiusRatio-1)*1.8f):obstruction;
            float smoothing=1-Mathf.Exp(-Time.deltaTime*3);
            pressure=Mathf.Lerp(pressure,targetPressure,smoothing);pulse=Mathf.Lerp(pulse,72+stress*40+m.ViralLoad*.10f,smoothing);oxygen=Mathf.Lerp(oxygen,98-stress*8,smoothing);flow=Mathf.Lerp(flow,targetFlow,smoothing);temperature=Mathf.Lerp(temperature,m.Temperature,smoothing);
            Critical=field?h.Critical:plaque&&obstruction>.8f;var color=Critical?new Color(1,.3f,.18f):HolographicStyle.Cyan;
            screens.vitalValues[0].text=$"{pressure:0}/{pressure*.66f:0}";
            screens.vitalValues[1].text=$"{pulse+.8f*Mathf.Sin(Time.time*1.1f):0}";
            screens.vitalValues[2].text=field?$"{h.DiameterMm:0.00} мм":$"{oxygen+.12f*Mathf.Sin(Time.time*.8f):0.0}%";
            screens.vitalValues[3].text=field?$"{h.VelocityRatio:0.00}×":$"{temperature:0.0}°";
            screens.vitalValues[4].text=wound?$"{world.woundState.LeakStrength*100:0}%":field||plaque?$"{flow*100:0}%":$"{m.ViralLoad:0}%";
            screens.vitalLabels[2].text=field?"ДИАМЕТР / Ø":"КИСЛОРОД / SpO₂";screens.vitalLabels[3].text=field?"СКОРОСТЬ / v":"ТЕМПЕРАТУРА";screens.vitalLabels[4].text=wound?"УТЕЧКА":field||plaque?"КРОВОТОК / Q":"ВИРУСНАЯ НАГРУЗКА";
            screens.monitorStatus.text=Critical?"КРИТИЧЕСКОЕ ИЗМЕНЕНИЕ":"● ЖИВОЙ МОНИТОРИНГ";screens.monitorStatus.color=color;
            for(int i=0;i<screens.vitalStatuses.Length;i++){screens.vitalStatuses[i].text=Critical?"КРИТИЧЕСКИ":stress>.25f?"Изменяется":"Стабильно";screens.vitalStatuses[i].color=color;}
            if(wound){screens.vitalStatuses[0].text=pressure<110?"Ниже нормы":"Восстановлено";screens.vitalStatuses[0].color=pressure<110?HolographicStyle.Amber:HolographicStyle.Cyan;screens.vitalStatuses[4].text=world.woundState.LeakStrength>.03f?"Кровь наружу":"Остановлена";screens.vitalStatuses[4].color=world.woundState.LeakStrength>.03f?HolographicStyle.Amber:HolographicStyle.Cyan;}
            if(alarm){alarm.enabled=Critical;alarm.color=new Color(1,.12f,.07f,.15f+.15f*Mathf.Sin(Time.time*5));}
            if(!episodePresenter&&field){screens.theoryTitle.text="Сосудистое поле";screens.shortTheory.text="Scanner: измерь стенку.\nПоле: удерживай правый курок.\nРука вверх — шире, вниз — уже.\nСледи за изменением показателей.";screens.illustrationText.text=$"R/R₀ = (r₀/r)⁴ = {h.ResistanceRatio:0.00}\nΔP = const: Q ∝ r⁴; v ∝ r²";}
            else if(!episodePresenter&&world.freeTools.Inspection&&Time.time<world.freeTools.InspectionUntil){var topic=world.freeTools.Inspection;screens.theoryTitle.text=topic.displayName;screens.shortTheory.text=topic.pages[0].text;screens.illustrationText.text=topic.pages[0].visualConnection;}
            if(modeLabel)modeLabel.text="BioTool / "+m.GetComponent<BioToolModeController>().Caption;
            assistance.gameObject.SetActive(Critical||IdleSeconds>=60);assistance.text=Critical?"Слишком сильное изменение! Верни диаметр ближе к исходному.":Hint();
        }
        string Hint()
        {
            var m=world.mission;var goal=m.Current;
            if(m.SceneNumber==3)return "Найди клетку → ПРИТЯЖЕНИЕ → удержи правый курок. Scanner — левый курок. Отпусти, чтобы вернуть в поток.";
            if(m.SceneNumber==5)return "Просканируй стенку. В СОСУДИСТОМ ПОЛЕ удержи курок и подними/опусти руку; на ПК используй колесо.";
            if(m.SceneNumber==7)return "ПРИТЯЖЕНИЕ: наведи на пузырёк и удерживай курок. Не теряй наведение; доведи пузырёк в кольцо.";
            if(m.SceneNumber==10&&world.immunePatrol)return m.StepIndex<2?"Проверь изменённую стенку Scanner. Отпусти и нажми снова, чтобы раскрыть глубокий слой.":"РЕВЕРС: удержи сигнал на доступном очаге. Лейкоциты сами мигрируют к ткани.";
            if(m.SceneNumber==12&&world.plateletLaunch)return "Тромбоцит из крови: ПРИТЯЖЕНИЕ → Scanner активирует → РЕВЕРС → попадание в разрыв.";
            if(m.SceneNumber==8||m.SceneNumber==9||m.SceneNumber==15&&goal?.target=="immune-engulf")return world.plateletLaunch?"Фагоцит: ПРИТЯЖЕНИЕ → Scanner активирует → РЕВЕРС. Наведи на вирус и отпусти.":"Фагоцит: ПРИТЯЖЕНИЕ → Scanner → АКТИВАЦИЯ → РЕВЕРС. Удержи курок, наведи на вирус и отпусти.";
            return goal?.action==StudyAction.Scan?"Наведи Scanner на объект текущей задачи и удерживай левый курок.":"Выбери режим BioTool для текущей задачи. Наведи на объект и удерживай правый курок.";
        }
    }
}
