using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    [DefaultExecutionOrder(1600)]
    public sealed class JourneyHemodynamicsInterface:MonoBehaviour
    {
        public JourneyHemodynamicsExperiment experiment;
        public JourneyHolographicScreens screens;
        public GameObject compactTheory,compactVitals;
        public Text title,explanation,task,progress;
        public Text[] values;
        public GameObject continueButton;
        bool activeBefore;
        void LateUpdate()
        {
            bool active=experiment.Active;compactTheory.SetActive(active);compactVitals.SetActive(active);
            if(active||activeBefore){screens.theoryRoot.gameObject.SetActive(!active);screens.vitalsRoot.gameObject.SetActive(!active);}activeBefore=active;
            if(!active)return;var mission=experiment.world.mission;
            title.text=mission.Complete?(experiment.Late?"ДАННЫЕ СОПОСТАВЛЕНЫ":"ЭКСПЕРИМЕНТ ЗАВЕРШЁН"):experiment.Title;
            explanation.text=mission.Complete?(experiment.Late?"Местный диаметр и стенка нормальны. Причину АД ищем дальше.":"Учебная модель возвращена к исходным условиям."):experiment.Summary;
            task.resizeTextForBestFit=true;task.resizeTextMinSize=22;task.resizeTextMaxSize=26;
            task.text=mission.Complete?(experiment.Late?"Продолжи поиск причины по сосуду.":"Радиус задаёт сопротивление.\nНизкое АД требует проверки причины."):mission.CurrentGoalLabel;
            progress.text=mission.Complete?"ГОТОВО":mission.Current?.action==StudyAction.Radius?$"Стабильность {experiment.Stability:P0}":$"{mission.StepIndex+1} / {mission.Goals.Count}";
            values[0].text=$"{mission.PressureSystolic:0}/{mission.PressureDiastolic:0}";values[1].text="72";values[2].text=$"{experiment.FlowRatio*100:0}%";
            continueButton.SetActive(mission.Complete);
        }
    }
}
