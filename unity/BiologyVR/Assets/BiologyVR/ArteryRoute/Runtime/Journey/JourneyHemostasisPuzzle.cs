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
        public bool flowLayering;
        public int RequiredPlatelets=>flowLayering?11:6;
        public JourneyPlateletActivation activationPuzzle;
        public bool ProcessingActivation{get;private set;}
        public bool StabilizationComplete=>Activated&&world.FibrinGrowth>=.999f&&world.woundState.LeakStrength<.025f;
        public int FlowReentries{get;private set;}
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
        float[] flowArc;
        Vector3[] releasePositions;
        float[] rejoin;
        readonly Plane[] planes=new Plane[6];
        public Vector3 Slot(int index)
        {if(flowLayering){int column=index%4,row=index/4;return world.woundSite.position+normal*(.18f+row*.06f)+up*((column%2==0?-.1f:.1f)+row*.012f)+forward*((column-1.5f)*.62f);}float a=index*Mathf.PI*2/6;float radius=index<6?.34f:.57f;return world.woundSite.position+normal*(.17f+(index%2)*.045f)+up*Mathf.Sin(a)*radius+forward*Mathf.Cos(a)*radius;}
        void Start(){Frame();}
        void Frame(){float s=world.mover.path.Anchor(11)+5;normal=-world.mover.path.Right(s);up=world.mover.path.Up(s);forward=world.mover.path.Forward(s);}
        public void Begin()
        {
            Frame();if(initialized)return;initialized=true;Attached=Connections=0;Activated=ReactionReady=false;reaction=0;
            for(int i=0;i<platelets.Length;i++)
            {
                var p=platelets[i];p.gameObject.SetActive(i<RequiredPlatelets);p.SetPlaced(false);p.SetPlacementSlot(-1);p.transform.position=world.woundSite.position+normal*(1.05f+i*.13f)+up*((i%3-1)*.42f)+forward*((i/3-.5f)*.55f);
                p.GetComponent<MeshFilter>().sharedMesh=resting;p.GetComponent<Renderer>().sharedMaterials=restingMaterials;
            }
            foreach(var p in strands){p.SetPlaced(false);p.gameObject.SetActive(false);}
            foreach(var line in fibres)line.enabled=false;
            if(world.fibrin)world.fibrin.SetActive(false);
            if(flowLayering){flowArc=new float[platelets.Length];releasePositions=new Vector3[platelets.Length];rejoin=new float[platelets.Length];for(int i=0;i<platelets.Length;i++){flowArc[i]=world.mover.path.Anchor(11)-2+i*.48f;rejoin[i]=1;if(i<RequiredPlatelets)platelets[i].transform.position=FlowPoint(i);}}
        }
        Vector3 FlowPoint(int index)=>world.mover.path.Offset(flowArc[index],-.65f+(index%4)*.44f,-.48f+(index%3)*.34f);
        public void RejoinFlow(JourneyHemostasisPiece item)
        {if(!flowLayering||item.strand||flowArc==null)return;releasePositions[item.index]=item.transform.position;rejoin[item.index]=0;}
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
                for(int i=0;i<platelets.Length;i++)if(platelets[i].gameObject.activeInHierarchy&&!platelets[i].Held&&!platelets[i].Placed)
                {
                    if(flowLayering)
                    {
                        flowArc[i]+=Time.deltaTime*(.30f+(i%4)*.025f);var point=FlowPoint(i);
                        GeometryUtility.CalculateFrustumPlanes(world.mover.viewCamera,planes);
                        float entry=world.mover.path.Anchor(11)-4;var incoming=world.mover.path.Offset(entry,-.65f+(i%4)*.44f,-.48f+(i%3)*.34f);
                        if(flowArc[i]>world.mover.path.Anchor(11)+9&&!GeometryUtility.TestPlanesAABB(planes,new Bounds(point,Vector3.one))&&!GeometryUtility.TestPlanesAABB(planes,new Bounds(incoming,Vector3.one)))
                        {flowArc[i]=entry;point=incoming;FlowReentries++;}
                        rejoin[i]=Mathf.Min(1,rejoin[i]+Time.deltaTime/.7f);platelets[i].transform.position=Vector3.Lerp(releasePositions[i],point,Mathf.SmoothStep(0,1,rejoin[i]));platelets[i].transform.Rotate(0,Time.deltaTime*(8+i%4),0);
                    }
                    else platelets[i].transform.position+=forward*Time.deltaTime*(Activated?.025f:.012f);
                }
                var proxy=world.Find("platelet");if(proxy)
                {
                    proxy.transform.position=Slot(0);foreach(var c in proxy.GetComponentsInChildren<Collider>(true))c.enabled=m.Current?.target=="platelet"&&m.Current.action==StudyAction.Activate;
                    foreach(var r in proxy.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                }
                var factor=world.Find("thrombin");if(factor){factor.transform.position=world.woundSite.position+normal*.85f+up*.90f;factor.gameObject.SetActive(Attached>=RequiredPlatelets&&(!flowLayering||m.Current?.action==StudyAction.Scan));}
                if(wrongFactor){wrongFactor.transform.position=world.woundSite.position+normal*.85f+up*.90f+forward*.7f;wrongFactor.gameObject.SetActive(Attached>=RequiredPlatelets&&(!flowLayering||m.Current?.action==StudyAction.Scan));}
                for(int i=0;i<strands.Length;i++)
                {
                    bool ready=reaction>0;strands[i].gameObject.SetActive(ready);starts[i].gameObject.SetActive(ready);ends[i].gameObject.SetActive(ready);
                    fibres[i].enabled=ready||Attached>=RequiredPlatelets;
                    fibres[i].positionCount=ready?2:3;
                    if(flowLayering)fibres[i].widthMultiplier=.013f*world.FibrinGrowth;
                    if(ready){fibres[i].SetPosition(0,starts[i].position);fibres[i].SetPosition(1,strands[i].transform.position);}
                    else if(Attached>=RequiredPlatelets){fibres[i].SetPosition(0,starts[i].position);fibres[i].SetPosition(1,starts[i].position+up*.09f+normal*.045f);fibres[i].SetPosition(2,starts[i].position+up*.18f);}
                }
                if(flowLayering&&StabilizationComplete&&m.Current?.target=="hemostasis-stable")m.Accept(StudyAction.Measure,"hemostasis-stable");
            }
            else if(wrongFactor)wrongFactor.gameObject.SetActive(false);
            if(m.SceneNumber==13)
            {
                foreach(var item in platelets)item.EnableBalanceGrab();foreach(var item in strands)item.EnableBalanceGrab();
                for(int i=0;i<fibres.Length;i++){fibres[i].enabled=strands[i].Placed||strands[i].Held;fibres[i].SetPosition(0,starts[i].position);fibres[i].SetPosition(1,strands[i].transform.position);}
                float balance=.06f+.44f*Mathf.Min(Attached,RequiredPlatelets)/RequiredPlatelets+.12f*Connections/3f+Mathf.Max(0,Attached-RequiredPlatelets)*.10f;
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
                if(active){label.text=m.SceneNumber==12?$"Первичная тромбоцитарная пробка: {Attached}/{RequiredPlatelets}"+(Activated?" • фибриновая стабилизация":Attached==RequiredPlatelets?" • рыхлая, утечка сохраняется":""):m.SceneNumber==13?"Корректируй пробку, сохраняя просвет":"Восстановление того же дефекта";label.transform.SetPositionAndRotation(world.woundSite.position+up*1.25f,world.mover.viewCamera.transform.rotation);}
            }
        }
        public void PlaceReleased(JourneyHemostasisPiece item,Transform hand)
        {
            if(flowLayering&&world.mission.SceneNumber==12&&!item.strand){RejoinFlow(item);world.mission.FeedbackMessage("Подведи тромбоцит удерживаемым полем BioTool: адгезия к обнажённому субэндотелию происходит автоматически.");return;}
            PlaceAtDamage(item,hand,false);
        }
        public bool TryAdhereFromField(JourneyHemostasisPiece item)
        {
            if(!flowLayering||world.mission.SceneNumber!=12||item.Placed)return false;
            float nearest=float.MaxValue;for(int i=0;i<RequiredPlatelets;i++){bool occupied=false;foreach(var p in platelets)if(p!=item&&p.Placed&&p.PlacementSlot==i){occupied=true;break;}if(!occupied)nearest=Mathf.Min(nearest,Vector3.Distance(item.transform.position,Slot(i)));}
            if(nearest>.17f)return false;PlaceAtDamage(item,null,true);return item.Placed;
        }
        void PlaceAtDamage(JourneyHemostasisPiece item,Transform hand,bool adhesion)
        {
            var m=world.mission;
            if((m.SceneNumber!=12&&m.SceneNumber!=13)||!m.Ready||item.Placed)return;
            var goal=m.Current;
            bool sequence=goal!=null&&goal.action==StudyAction.Direct&&goal.target==(item.strand?"fibrin":"platelet");
            if(m.SceneNumber==13)sequence=true;
            if(!sequence){Warn("Неверный этап: сначала наложи тромбоциты, затем выполни активацию.");RejoinFlow(item);return;}
            int slot=item.index;
            if(flowLayering&&!item.strand)
            {
                slot=-1;float nearest=float.MaxValue;int available=m.SceneNumber==13?platelets.Length:RequiredPlatelets;
                for(int candidate=0;candidate<available;candidate++)
                {bool occupied=false;foreach(var platelet in platelets)if(platelet!=item&&platelet.Placed&&platelet.PlacementSlot==candidate){occupied=true;break;}if(occupied)continue;float d=Vector3.Distance(item.transform.position,Slot(candidate));if(d<nearest){nearest=d;slot=candidate;}}
                if(slot<0)return;
            }
            Vector3 destination=item.strand?ends[slot].position:Slot(slot);
            if(item.strand&&!ReactionReady){Warn("Тромбин ещё превращает фибриноген в нить. Подожди реакцию.");return;}
            if(Vector3.Distance(item.transform.position,destination)>(item.strand?.19f:.26f))
            {if(m.SceneNumber==12)Warn(item.strand?"Конец нити не соединён с парным anchor. Протяни к другому концу.":"Тромбоцит вне разрыва. Возьми снова и наложи на свободный участок.");RejoinFlow(item);return;}
            ProcessingPlacement=true;
            bool accepted=m.SceneNumber==13||m.Accept(StudyAction.Direct,item.strand?"fibrin":"platelet");ProcessingPlacement=false;
            if(!accepted)return;
            item.SetPlacementSlot(slot);item.SetPlaced(true);StartCoroutine(Snap(item.transform,destination));
            if(item.strand)Connections++;else{Attached++;SetPlatelet(item,Activated||adhesion);}
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
        public void ActivatePlatelets()
        {
            Activated=true;foreach(var p in platelets)if(p.Placed)SetPlatelet(p,true);
            if(flowLayering){StartReaction();Connections=strands.Length;for(int i=0;i<strands.Length;i++){strands[i].transform.position=ends[i].position;strands[i].SetPlaced(true);}world.StartPhysicalFibrinGrowth();}
        }
        public bool FinishActivationPuzzle()
        {ProcessingActivation=true;bool accepted=world.mission.Accept(StudyAction.Activate,"platelet");ProcessingActivation=false;return accepted;}
        public void StartReaction()
        {
            reaction=.001f;
            for(int i=0;i<strands.Length;i++){strands[i].transform.position=starts[i].position+normal*.04f;strands[i].gameObject.SetActive(true);}
        }
        public bool CanApply(StudyAction action,string id)
        {
            if(id=="platelet"&&action==StudyAction.Direct)return ProcessingPlacement;
            if(id=="platelet"&&action==StudyAction.Activate)return flowLayering?Attached>=RequiredPlatelets&&ProcessingActivation:Attached>=1;
            if(id=="thrombin")return flowLayering?Attached>=RequiredPlatelets:Attached>=6&&Activated;
            if(id=="hemostasis-stable")return StabilizationComplete;
            if(id=="fibrin"&&action==StudyAction.Direct)return ProcessingPlacement&&ReactionReady;
            return true;
        }
        void Warn(string text)
        {world.mission.RecordPhysicalError(text);var feedback=world.mission.GetComponent<JourneyToolFeedback>();if(feedback)feedback.Warn(false);}
    }
}
