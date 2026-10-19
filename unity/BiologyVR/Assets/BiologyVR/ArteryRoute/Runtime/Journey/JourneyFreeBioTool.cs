using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>The real BioTool trigger drives one bounded XRI field hand in every episode.</summary>
    public sealed class JourneyFreeBioTool:MonoBehaviour
    {
        public JourneyWorld world;
        public JourneyBloodPayload payload;
        public JourneyTarget Held{get;private set;}
        public bool Selecting{get;private set;}
        public bool ReturnSuppressed{get;private set;}
        public float ReverseCharge{get;private set;}
        public int Shots{get;private set;}
        public int BrokenCells{get;private set;}
        public JourneyResearchTheory Inspection{get;private set;}
        public float InspectionUntil{get;private set;}
        JourneyBioFieldInteractor hand;
        readonly RaycastHit[] contacts=new RaycastHit[48];
        XRGrabInteractable selected;
        Transform dock;
        Vector3 pullStart,velocity;
        float acquireAge,reverseAge,nextShot,depth=1.1f;
        bool previousTrigger;
        JourneyTarget scanTarget,acquireTarget;
        float scanAge;
        bool scanConsumed;
        int scene=-1;
        void Awake()
        {
            var go=new GameObject("BioTool field XRI hand");go.transform.SetParent(world.transform,false);dock=go.transform;
            hand=go.AddComponent<JourneyBioFieldInteractor>();hand.owner=this;hand.interactionManager=FindFirstObjectByType<XRInteractionManager>();hand.interactionLayers=1<<28;
        }
        public XRGrabInteractable Selected=>selected;
        public bool Handle(float dt,bool scan,bool trigger,JourneyTarget hit,Transform aim,bool desktop,BioToolMode mode)
        {
            if(scene!=world.mission.SceneNumber){Release();scene=world.mission.SceneNumber;previousTrigger=false;}
            if(!aim)return false;
            if(mode!=BioToolMode.Tonus||!trigger)world.hemodynamics.Control(dt,false,aim,hit,desktop);
            if(mode==BioToolMode.Tonus&&trigger&&!scan&&!selected){world.hemodynamics.Control(dt,true,aim,hit,desktop);return true;}
            if(world.EmbolusCaptured&&mode==BioToolMode.Capture)return false;
            var bond=hit?hit.GetComponent<JourneyFlowBond>():null;var carrier=bond&&bond.carrier?bond.carrier:hit;
            bool phag=Held&&Held.GetComponent<JourneyPhagocyteEntity>();var platelet=Held?Held.GetComponent<JourneyPlateletEntity>():null;
            if(mode==BioToolMode.Bio&&trigger&&!scan&&carrier&&world.bloodEntities.Owns(carrier)){var comparedPlatelet=carrier.GetComponent<JourneyPlateletEntity>();if(comparedPlatelet)comparedPlatelet.SetState(JourneyPlateletState.Activated);}
            if(mode==BioToolMode.Impulse&&trigger&&!scan&&Time.time>=nextShot)
            {
                nextShot=Time.time+.24f;Shots++;var end=hit?hit.transform.position:aim.position+aim.forward*12;
                world.mission.GetComponent<JourneyInstrumentRig>().FirePulse(end);
                if(carrier&&carrier!=Held&&world.bloodEntities.Owns(carrier)){world.bloodEntities.stream.BreakCell(world.bloodEntities.Slot(carrier).index);world.bloodEntities.Release(carrier,true);BrokenCells++;}
                else if(hit&&hit.GetComponent<JourneyPlaqueLipid>()&&world.plaqueChallenge.Combat)world.Primary(hit);
            }
            if(mode==BioToolMode.Capture&&trigger&&!scan&&!selected&&carrier&&world.bloodEntities.Owns(carrier))
            {
                if(acquireTarget!=carrier){acquireTarget=carrier;acquireAge=0;}acquireAge+=dt;if(acquireAge>=.12f){Held=carrier;selected=carrier.GetComponent<XRGrabInteractable>();depth=desktop?1.1f:.45f;Selecting=true;dock.SetPositionAndRotation(carrier.transform.position,carrier.transform.rotation);velocity=Vector3.zero;var cell=carrier.GetComponent<JourneyPhagocyteEntity>();if(cell)world.immunity.Captured(cell);}
            }
            else if(!selected)acquireAge=0;
            if(mode==BioToolMode.Capture&&trigger&&!scan&&!selected&&(!carrier||!world.bloodEntities.Owns(carrier))&&world.mission.SceneNumber!=7)
            {
                int count=Physics.RaycastNonAlloc(new Ray(aim.position,aim.forward),contacts,12,~0,QueryTriggerInteraction.Collide);XRGrabInteractable nearest=null;float distance=12;
                for(int i=0;i<count;i++){var grab=contacts[i].collider.GetComponentInParent<XRGrabInteractable>();if(grab&&grab.enabled&&!grab.GetComponent<JourneyHemostasisPiece>()&&!grab.GetComponent<JourneyFlowBond>()&&contacts[i].distance<distance){distance=contacts[i].distance;nearest=grab;}}
                if(nearest){selected=nearest;Held=nearest.GetComponent<JourneyTarget>();Selecting=true;depth=Mathf.Clamp(distance,.7f,6);dock.SetPositionAndRotation(nearest.transform.position,nearest.transform.rotation);}
            }
            if(selected)
            {
                hand.interactionLayers=selected.interactionLayers;
                if(mode==BioToolMode.Reverse&&(phag||platelet&&world.plateletLaunch))
                {
                    if(trigger&&!previousTrigger){pullStart=aim.position;reverseAge=0;}
                    if(trigger){reverseAge+=dt;float pull=Mathf.Clamp(Vector3.Dot(pullStart-aim.position,aim.forward),0,.25f);ReverseCharge=Mathf.Max(Mathf.Clamp01(reverseAge/.65f),pull/.25f);}
                    if(phag)world.immunity.Preview(Held.GetComponent<JourneyPhagocyteEntity>(),aim,ReverseCharge);else world.plateletLaunch.Preview(Held,aim,ReverseCharge);
                    if(!trigger&&previousTrigger&&ReverseCharge>.15f){var target=Held;var cell=Held.GetComponent<JourneyPhagocyteEntity>();Release(false);if(cell)world.immunity.LaunchFromField(cell,aim,ReverseCharge);else world.plateletLaunch.Launch(target,aim,ReverseCharge);ReverseCharge=0;previousTrigger=false;return true;}
                }
                if(mode==BioToolMode.Bio&&phag&&trigger&&!scan)world.immunity.Activate(Held.GetComponent<JourneyPhagocyteEntity>(),dt);
                else if(mode==BioToolMode.Bio&&trigger&&!previousTrigger&&!scan&&carrier&&!world.bloodEntities.Owns(carrier))world.Primary(carrier);
                if(mode==BioToolMode.Capture&&!trigger&&!scan)Release();
                else
                {
                    if(desktop&&mode==BioToolMode.Capture)depth=Mathf.Clamp(depth+JourneyDesktopControls.WheelSteps*.12f,.65f,6);
                    var destination=aim.position+aim.forward*depth;if(!desktop||!scan){dock.position=Vector3.SmoothDamp(dock.position,destination,ref velocity,.13f,9,dt);dock.rotation=aim.rotation;}
                }
                previousTrigger=trigger;
                // Scanner still sees the actual held object and drives normal journal/mission scan.
                return !scan;
            }
            if(world.plateletLaunch&&mode==BioToolMode.Reverse&&trigger&&!previousTrigger&&!scan&&carrier&&world.mission.Current?.action==StudyAction.Direct&&world.mission.Current.target==carrier.targetId)
            {previousTrigger=true;world.Primary(carrier);return true;}
            previousTrigger=trigger;
            if(mode==BioToolMode.Impulse)return !scan;
            return false;
        }
        public bool ScanFlow(float dt,bool scan,JourneyTarget hit)
        {
            if(world.mission.SceneNumber==3)return false;
            if(!scan){scanTarget=null;scanAge=0;scanConsumed=false;return false;}
            if(!hit||!world.bloodEntities.Owns(hit)&&!payload.Available(hit))return false;
            if(world.immunity.Active&&hit.GetComponent<JourneyPhagocyteEntity>())return false;
            if(scanTarget!=hit){scanTarget=hit;scanAge=0;scanConsumed=false;}
            if(!scanConsumed){scanAge+=dt;if(scanAge>=.75f){scanConsumed=true;var page=world.research.Find(hit.targetId);if(page){Inspection=page.theory;InspectionUntil=Time.time+10;world.mission.FeedbackMessage("Изучен элемент потока: "+page.theory.displayName);}var phag=hit.GetComponent<JourneyPhagocyteEntity>();if(phag)phag.Studied=true;if(world.plateletLaunch&&Held==hit){var platelet=hit.GetComponent<JourneyPlateletEntity>();if(platelet){platelet.ActivateByScanner();world.mission.GetComponent<JourneyInstrumentRig>().ScannerActivated(hit.transform.position);world.mission.FeedbackMessage("Тромбоцит активирован. РЕВЕРС: направь в повреждённую стенку.");}}}}
            return true;
        }
        public void ModeChanged(BioToolMode mode){previousTrigger=false;ReverseCharge=reverseAge=0;}
        public void Release(bool returnToFlow=true)
        {
            Selecting=false;previousTrigger=false;
            ReturnSuppressed=!returnToFlow;
            var target=Held;Held=null;var grab=selected;selected=null;acquireAge=reverseAge=0;
            if(grab&&grab.isSelected&&hand.interactionManager)hand.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            if(target&&returnToFlow&&world.bloodEntities.Owns(target))world.bloodEntities.Release(target,true);
            ReturnSuppressed=false;
        }
        void OnDisable(){Release();}
    }
    public sealed class JourneyBioFieldInteractor:XRBaseInteractor
    {
        public JourneyFreeBioTool owner;
        public override bool isSelectActive=>owner&&owner.Selecting;
        public override bool isHoverActive=>owner&&owner.Selecting;
        public override void GetValidTargets(List<IXRInteractable> targets){targets.Clear();if(owner&&owner.Selected)targets.Add(owner.Selected);}
    }
}
