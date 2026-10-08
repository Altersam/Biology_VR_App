using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Spatial platelet/fibrin puzzle at the existing WALL_A. No alternate mission or input system.</summary>
    public sealed class JourneyHemostasisPuzzle:MonoBehaviour
    {
        public JourneyWorld world;
        public JourneyHemostasisPiece[] platelets,strands;
        public Transform[] starts,ends;
        public LineRenderer[] fibres;
        public Mesh resting,activated;
        public Material[] restingMaterials,activatedMaterials;
        public JourneyTarget wrongFactor;
        public Text label;
        public int Attached{get;private set;}
        public int Connections{get;private set;}
        public bool Activated{get;private set;}
        public bool ReactionReady{get;private set;}
        public bool ProcessingPlacement{get;private set;}
        int lastScene=-1;
        bool initialized;
        float reaction;
        float stableBalance;
        Vector3[] connectionPositions;
        Vector3 normal,up,forward;
        public Vector3 Slot(int index)
        {float a=index*Mathf.PI*2/6;float radius=index<6?.34f:.57f;return world.woundSite.position+normal*(.17f+(index%2)*.045f)+up*Mathf.Sin(a)*radius+forward*Mathf.Cos(a)*radius;}
        void Start(){Frame();}
        void Frame(){float s=world.mover.path.Anchor(11)+5;normal=-world.mover.path.Right(s);up=world.mover.path.Up(s);forward=world.mover.path.Forward(s);}
        public void Begin()
        {
            Frame();if(initialized)return;initialized=true;Attached=Connections=0;Activated=ReactionReady=false;reaction=0;
            for(int i=0;i<platelets.Length;i++)
            {
                var p=platelets[i];p.gameObject.SetActive(i<6);p.SetPlaced(false);p.transform.position=world.woundSite.position+normal*(1.05f+i*.13f)+up*((i%3-1)*.42f)+forward*((i/3-.5f)*.55f);
                p.GetComponent<MeshFilter>().sharedMesh=resting;p.GetComponent<Renderer>().sharedMaterials=restingMaterials;
            }
            foreach(var p in strands){p.SetPlaced(false);p.gameObject.SetActive(false);}
            foreach(var line in fibres)line.enabled=false;
            if(world.fibrin)world.fibrin.SetActive(false);
        }
        void LateUpdate()
        {
            if(!world||!world.mission)return;var m=world.mission;
            if(lastScene!=m.SceneNumber)
            {
                lastScene=m.SceneNumber;if(lastScene==12)Begin();
                if(lastScene==13&&initialized)
                {foreach(var item in platelets){item.gameObject.SetActive(true);item.EnableBalanceGrab();}foreach(var item in strands)item.EnableBalanceGrab();stableBalance=0;}
                if(lastScene==14&&initialized)
                {foreach(var item in platelets){item.Grab.enabled=false;foreach(var c in item.GetComponents<Collider>())c.enabled=false;}foreach(var item in strands){item.Grab.enabled=false;foreach(var c in item.GetComponents<Collider>())c.enabled=false;}}
            }
            bool active=m.SceneNumber>=12&&m.SceneNumber<=14;
            if(!initialized)return;
            if(m.SceneNumber==12&&ReactionReady==false&&reaction>0)
            {reaction=Mathf.Min(1,reaction+Time.deltaTime/.75f);ReactionReady=reaction>=1;}
            if(m.SceneNumber==12)
            {
                for(int i=0;i<platelets.Length;i++)if(!platelets[i].Held&&!platelets[i].Placed)
                {platelets[i].transform.position+=forward*Time.deltaTime*(Activated?.025f:.012f);}
                var proxy=world.Find("platelet");if(proxy)
                {
                    proxy.transform.position=Slot(0);foreach(var c in proxy.GetComponentsInChildren<Collider>(true))c.enabled=m.Current?.target=="platelet"&&m.Current.action==StudyAction.Activate;
                    foreach(var r in proxy.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                }
                var factor=world.Find("thrombin");if(factor){factor.transform.position=world.woundSite.position+normal*.85f+up*.90f;factor.gameObject.SetActive(Attached>=6);}
                if(wrongFactor){wrongFactor.transform.position=world.woundSite.position+normal*.85f+up*.90f+forward*.7f;wrongFactor.gameObject.SetActive(Attached>=6);}
                for(int i=0;i<strands.Length;i++)
                {
                    bool ready=reaction>0;strands[i].gameObject.SetActive(ready);starts[i].gameObject.SetActive(ready);ends[i].gameObject.SetActive(ready);
                    fibres[i].enabled=ready||Attached>=6;
                    fibres[i].positionCount=ready?2:3;
                    if(ready){fibres[i].SetPosition(0,starts[i].position);fibres[i].SetPosition(1,strands[i].transform.position);}
                    else if(Attached>=6){fibres[i].SetPosition(0,starts[i].position);fibres[i].SetPosition(1,starts[i].position+up*.09f+normal*.045f);fibres[i].SetPosition(2,starts[i].position+up*.18f);}
                }
            }
            else if(wrongFactor)wrongFactor.gameObject.SetActive(false);
            if(m.SceneNumber==13)
            {
                foreach(var item in platelets)item.EnableBalanceGrab();foreach(var item in strands)item.EnableBalanceGrab();
                for(int i=0;i<fibres.Length;i++){fibres[i].enabled=strands[i].Placed||strands[i].Held;fibres[i].SetPosition(0,starts[i].position);fibres[i].SetPosition(1,strands[i].transform.position);}
                float balance=.06f+.44f*Mathf.Min(Attached,6)/6f+.12f*Connections/3f+Mathf.Max(0,Attached-6)*.10f;
                world.mission.UpdatePhysicalClotBalance(balance);
                bool held=false;foreach(var item in platelets)held|=item.Held;foreach(var item in strands)held|=item.Held;
                var goal=m.Current;bool correct=goal?.action==StudyAction.Balance&&(goal.target=="insufficient"&&balance<.28f||goal.target=="excessive"&&balance>.86f||goal.target=="optimal"&&balance>.50f&&balance<.72f);
                stableBalance=correct&&!held?stableBalance+Time.deltaTime:0;
                if(stableBalance>.65f){stableBalance=0;m.Accept(StudyAction.Balance,goal.target);}
            }
            if(m.SceneNumber>=14)
            {
                for(int i=0;i<fibres.Length;i++){fibres[i].enabled=world.FibrinRemoval<.999f&&world.RepairGrowth<.999f&&strands[i].Placed;fibres[i].widthMultiplier=.013f*(1-world.FibrinRemoval);}
                foreach(var item in strands)item.gameObject.SetActive(world.FibrinRemoval<.999f&&world.RepairGrowth<.999f);
                foreach(var anchor in starts)anchor.gameObject.SetActive(world.FibrinRemoval<.999f&&world.RepairGrowth<.999f);foreach(var anchor in ends)anchor.gameObject.SetActive(world.FibrinRemoval<.999f&&world.RepairGrowth<.999f);
            }
            if(label)
            {
                label.gameObject.SetActive(active);
                if(active){label.text=m.SceneNumber==12?$"Руками: тромбоциты {Attached}/6 • фибрин {Connections}/3":m.SceneNumber==13?"Корректируй пробку, сохраняя просвет":"Восстановление того же дефекта";label.transform.SetPositionAndRotation(world.woundSite.position+up*1.25f,world.mover.viewCamera.transform.rotation);}
            }
        }
        public void PlaceReleased(JourneyHemostasisPiece item,Transform hand)
        {
            var m=world.mission;
            if((m.SceneNumber!=12&&m.SceneNumber!=13)||!m.Ready||item.Placed)return;
            var goal=m.Current;
            bool sequence=goal!=null&&goal.action==StudyAction.Direct&&goal.target==(item.strand?"fibrin":"platelet");
            if(m.SceneNumber==13)sequence=true;
            if(!sequence){Warn("Неверный этап: сначала адгезия, активация и тромбин.");return;}
            int slot=item.index;Vector3 destination=item.strand?ends[slot].position:Slot(slot);
            if(item.strand&&!ReactionReady){Warn("Тромбин ещё превращает фибриноген в нить. Подожди реакцию.");return;}
            if(Vector3.Distance(item.transform.position,destination)>(item.strand?.19f:.26f))
            {if(m.SceneNumber==12)Warn(item.strand?"Конец нити не соединён с парным anchor. Протяни к другому концу.":"Тромбоцит вне exposed adhesion zone. Возьми снова и поднеси к ране.");return;}
            ProcessingPlacement=true;
            bool accepted=m.SceneNumber==13||m.Accept(StudyAction.Direct,item.strand?"fibrin":"platelet");ProcessingPlacement=false;
            if(!accepted)return;
            item.SetPlaced(true);StartCoroutine(Snap(item.transform,destination));
            if(item.strand)Connections++;else{Attached++;SetPlatelet(item,Activated);}
            if(hand){var node=hand.name.Contains("Left")?XRNode.LeftHand:XRNode.RightHand;var device=InputDevices.GetDeviceAtXRNode(node);if(device.isValid)device.SendHapticImpulse(0,.12f,.04f);}
            if(item.strand&&Connections==strands.Length&&m.SceneNumber==12)world.StartPhysicalFibrinGrowth();
            if(m.SceneNumber==13)world.SetPhysicalPlateletCount(Attached);
        }
        public void Removed(JourneyHemostasisPiece item)
        {if(item.strand)Connections=Mathf.Max(0,Connections-1);else Attached=Mathf.Max(0,Attached-1);world.SetPhysicalPlateletCount(Attached);stableBalance=0;}
        IEnumerator Snap(Transform t,Vector3 point)
        {yield return null;var from=t.position;for(float time=0;time<.18f;time+=Time.deltaTime){t.position=Vector3.Lerp(from,point,Mathf.SmoothStep(0,1,time/.18f));yield return null;}t.position=point;}
        void SetPlatelet(JourneyHemostasisPiece p,bool active)
        {p.GetComponent<MeshFilter>().sharedMesh=active?activated:resting;p.GetComponent<Renderer>().sharedMaterials=active?activatedMaterials:restingMaterials;}
        public void ActivatePlatelets(){Activated=true;foreach(var p in platelets)if(p.Placed)SetPlatelet(p,true);}
        public void StartReaction()
        {
            reaction=.001f;
            for(int i=0;i<strands.Length;i++){strands[i].transform.position=starts[i].position+normal*.04f;strands[i].gameObject.SetActive(true);}
        }
        public bool CanApply(StudyAction action,string id)
        {
            if(id=="platelet"&&action==StudyAction.Direct)return ProcessingPlacement;
            if(id=="platelet"&&action==StudyAction.Activate)return Attached>=1;
            if(id=="thrombin")return Attached>=6&&Activated;
            if(id=="fibrin"&&action==StudyAction.Direct)return ProcessingPlacement&&ReactionReady;
            return true;
        }
        void Warn(string text)
        {world.mission.RecordPhysicalError(text);var feedback=world.mission.GetComponent<JourneyToolFeedback>();if(feedback)feedback.Warn(false);}
    }
}
