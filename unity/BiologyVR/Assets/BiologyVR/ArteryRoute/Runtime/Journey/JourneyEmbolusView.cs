using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyEmbolusView : MonoBehaviour
    {
        public JourneyWorld world;
        public Transform dock;
        public LineRenderer ring,tether;
        public Text label;
        void LateUpdate()
        {
            if(!world||!world.mission)return;
            bool active=world.mission.SceneNumber==7;
            dock.gameObject.SetActive(active);
            tether.enabled=active&&world.EmbolusCaptured;
            if(!active)return;
            var p=world.mover.path;float s=p.Anchor(7)+1.9f;
            dock.SetPositionAndRotation(p.Offset(s,.88f,-.25f),p.Frame(s));
            if(ring)ring.widthMultiplier=.015f*(1+.10f*Mathf.Sin(Time.time*2));
            if(tether.enabled)
            {
                var input=world.mission.GetComponent<JourneyInput>();var tracked=input&&(input.useExplicitAimSources||world.mover.UsingTrackedInput);
                var start=tracked&&input.bioTool?input.bioTool.position:world.mover.viewCamera.transform.position+world.mover.viewCamera.transform.right*.28f-world.mover.viewCamera.transform.up*.15f;
                tether.widthMultiplier=.0015f;
                tether.enabled=JourneyToolFeedback.ClipBeamStart(world.mover.viewCamera,ref start,world.bubble.position);
                if(tether.enabled){tether.SetPosition(0,start);tether.SetPosition(1,world.bubble.position);}
            }
            if(label){label.text=world.mission.Complete?"Эмбол нейтрализован":world.EmbolusCaptured?$"Удерживай поле • стабилизация {world.EmbolusStability:P0}":"Стабилизационная зона";label.transform.SetPositionAndRotation(dock.position+world.mover.viewCamera.transform.up*.37f,world.mover.viewCamera.transform.rotation);}
        }
    }
}
