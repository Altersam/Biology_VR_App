using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyHemostasisView : MonoBehaviour
    {
        [SerializeField] JourneyWorld world;
        [SerializeField] Renderer[] plug;
        [SerializeField] Renderer network;
        [SerializeField] Text label;
        public void Configure(JourneyWorld w,Renderer[] platelets,Renderer fibres,Text caption){world=w;plug=platelets;network=fibres;label=caption;}
        void LateUpdate()
        {
            if(!world||!world.mission)return;
            var m=world.mission;bool active=m.SceneNumber>=11&&m.SceneNumber<=14;
            bool dormant=world.IsEpisodeCompleted(12)||world.IsEpisodeCompleted(13);
            bool plugVisible=(active&&((m.SceneNumber==12&&m.StepIndex>=2)||(m.SceneNumber==13)||(m.SceneNumber==14&&world.RepairGrowth<.999f))||dormant)&&world.RepairGrowth<.999f;
            foreach(var r in plug)if(r)r.enabled=plugVisible&&!world.hemostasisPuzzle;
            if(network)network.enabled=(active||dormant)&&world.FibrinGrowth>0&&world.RepairGrowth<.999f&&(!world.hemostasisPuzzle||world.hemostasisPuzzle.Connections>=3);
            if(!label)return;label.gameObject.SetActive(active);
            if(!active)return;
            string text=m.SceneNumber==11?"Дефект эндотелия • диагностика":m.SceneNumber==13?m.ClotBalance<.28f?"Недостаточная пробка • утечка":m.ClotBalance>.86f?"Избыточный сгусток • сужение":"Оптимум • просвет сохранён":m.SceneNumber==14?world.RepairGrowth>.999f?"Эндотелий восстановлен":world.RepairGrowth>0?"Восстановление эндотелия":"Очистка зоны • фагоцитоз":m.StepIndex==0?"Адгезия тромбоцитов":m.StepIndex==1?"Активация • изменение формы":m.StepIndex==2?"Агрегация • первичная пробка":m.StepIndex==3?"Тромбин • фибриноген → фибрин":"Фибриновая сеть • стабилизация";
            if(label.text!=text)label.text=text;
            var camera=world.mover.viewCamera.transform;
            label.transform.SetPositionAndRotation(world.woundSite.position+(camera.position-world.woundSite.position).normalized*.25f+camera.up*.75f,camera.rotation);
        }
    }
}
