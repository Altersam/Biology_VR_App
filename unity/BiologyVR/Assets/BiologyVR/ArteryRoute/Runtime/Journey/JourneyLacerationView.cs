using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyLacerationView:MonoBehaviour
    {
        public JourneyWorld world;
        public GameObject visualRoot;
        public Transform[] edgeFlaps;
        public LineRenderer[] jets;
        public Renderer scar;
        public Text label;
        public float ClosurePreview{get;private set;}
        Vector3[] restScale;
        Vector3 outward,up,forward;
        MaterialPropertyBlock colors;
        void Awake(){restScale=new Vector3[edgeFlaps.Length];for(int i=0;i<edgeFlaps.Length;i++)restScale[i]=edgeFlaps[i].localScale;float s=world.mover.path.Anchor(11)+5;outward=world.mover.path.Right(s);up=world.mover.path.Up(s);forward=world.mover.path.Forward(s);colors=new MaterialPropertyBlock();}
        void LateUpdate()
        {
            if(!world||world.mission.Goals==null)return;var mission=world.mission;bool near=mission.SceneNumber>=11&&mission.SceneNumber<=14;
            visualRoot.SetActive(mission.SceneNumber<17&&(near||world.IsEpisodeCompleted(11)));if(!visualRoot.activeSelf)return;
            float healing=world.RepairGrowth;
            bool activated=world.hemostasisPuzzle&&world.hemostasisPuzzle.Activated;
            ClosurePreview=Mathf.MoveTowards(ClosurePreview,healing>.001f?healing:activated?.30f:0,Time.deltaTime*.14f);
            for(int i=0;i<edgeFlaps.Length;i++)edgeFlaps[i].localScale=Vector3.Scale(restScale[i],new Vector3(1,Mathf.Lerp(1,.45f,ClosurePreview),1));
            for(int i=0;i<jets.Length;i++)
            {
                jets[i].enabled=!world.woundState.splitMainFlow&&near&&world.woundState.LeakStrength>.07f;
                if(!jets[i].enabled)continue;
                float pulse=.8f+.2f*Mathf.Sin(Time.time*8+i);float x=(i-(jets.Length-1)*.5f)*.30f;
                var point=world.woundSite.position+forward*x+up*Mathf.Sin(i*2.1f)*.028f;
                jets[i].SetPosition(0,point-outward*.16f);jets[i].SetPosition(1,point+outward*(.34f+.22f*pulse)*world.woundState.LeakStrength+forward*.03f);jets[i].widthMultiplier=.012f*pulse*world.woundState.LeakStrength;
            }
            if(scar){colors??=new MaterialPropertyBlock();scar.GetPropertyBlock(colors);colors.SetColor("_BaseColor",Color.Lerp(new Color(.29f,.045f,.045f),new Color(.76f,.38f,.38f),ClosurePreview));scar.SetPropertyBlock(colors);}
            if(label){label.gameObject.SetActive(near);label.text=healing>.99f?"ЭНДОТЕЛИЙ ВОССТАНОВЛЕН":healing>0?"ВОССТАНОВЛЕНИЕ ЭНДОТЕЛИЯ":activated?world.hemostasisPuzzle.StabilizationComplete?"ГЕМОСТАЗ СТАБИЛЕН • РЕПАРАЦИЯ ВПЕРЕДИ":"ОБРАЗОВАНИЕ ФИБРИНА • УТЕЧКА СНИЖАЕТСЯ":"РАЗРЫВ СТЕНКИ • КРОВЬ ВЫХОДИТ НАРУЖУ";label.transform.SetPositionAndRotation(world.woundSite.position+up*.72f-outward*.18f,world.mover.viewCamera.transform.rotation);}
        }
    }
}
