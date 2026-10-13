using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>One pooled cell-manipulation model for the first virus and later encounters.</summary>
    [DefaultExecutionOrder(650)]
    public sealed class JourneyInnateImmunity:MonoBehaviour
    {
        public JourneyWorld world;
        public JourneyImmuneVirus[] viruses;
        public LineRenderer trajectory;
        public Text status;
        public JourneyPhagocyteEntity[] phagocytes;
        [SerializeField] bool tutorialCompleted;
        public bool TutorialCompleted=>tutorialCompleted;
        public bool Active=>world.mission.SceneNumber==8||world.mission.SceneNumber==9||world.mission.SceneNumber==15;
        public bool Tutorial=>world.mission.SceneNumber==8;
        public JourneyPhagocyteEntity Held{get;private set;}
        public JourneyImmuneVirus TutorialVirus=>viruses[0];
        public int Neutralized{get;private set;}
        public int Missed{get;private set;}
        public int Launched{get;private set;}
        public int Spawned{get;private set;}
        public int MaximumLive{get;private set;}
        public int DamageContacts{get;private set;}
        public float ActivationProgress{get;private set;}
        public float ScanProgress{get;private set;}
        public float ChargeProgress{get;private set;}
        public float PullDistance{get;private set;}
        public bool ProcessingScan{get;private set;}
        public bool ProcessingCapture{get;private set;}
        public bool ProcessingSuccess{get;private set;}
        public Vector3 LastLaunchVelocity{get;private set;}
        public Vector3 LastBloodVelocity{get;private set;}
        public const float HoldDistance=.30f;
        JourneyTarget scanningTarget;
        int scannedGeneration=-1;
        JourneyPhagocyteEntity engagingCell;
        JourneyImmuneVirus engagingVirus;
        Vector3 handStart,handForward,holdVelocity,engulfStart;
        float scanAge,spawnClock,engulfAge,desktopPull,captureAge;
        bool scanConsumed,previousScan,previousTool;
        int lastScene=-1,serial;
        public int LiveCount{get{int count=0;foreach(var virus in viruses)if(virus.Live)count++;return count;}}
        public void ResetRun(){tutorialCompleted=false;Clear();}
        void Clear()
        {
            CancelHold();engagingCell=null;engagingVirus=null;
            foreach(var cell in phagocytes)if(cell&&world.bloodEntities.Owns(cell.target)){cell.SetState(JourneyPhagocyteState.Flowing);cell.launchVelocity=Vector3.zero;cell.Animate(0,Vector3.zero);}
            foreach(var virus in viruses)virus.Return();scanningTarget=null;scannedGeneration=-1;scanAge=captureAge=0;
            scanConsumed=previousScan=previousTool=false;ScanProgress=ChargeProgress=PullDistance=0;
        }
        void Update()
        {
            if(!world||world.mission.Goals==null)return;var mission=world.mission;
            if(lastScene!=mission.SceneNumber)
            {
                Clear();lastScene=mission.SceneNumber;Neutralized=Missed=Launched=Spawned=MaximumLive=DamageContacts=serial=0;spawnClock=0;
            }
            if(!Active){status.gameObject.SetActive(false);if(world.freeTools)MoveOutsideEncounter();return;}if(!mission.Ready)return;
            // Enter starts normal travel before the player reaches the new arc.
            // Place the single tutorial particle only at the arrived station.
            if(Tutorial&&!tutorialCompleted&&Spawned==0){Spawn(true);world.bloodEntities.stream.RequestInflux("phagocyte");}
            if(Tutorial)foreach(var id in new[]{"genome","capsid","epitope"}){var legacy=world.Find(id);if(legacy)legacy.gameObject.SetActive(false);}
            var mode=world.Find("immune-mode");if(mode)mode.gameObject.SetActive(false);
            bool wave=mission.Current?.target=="immune-engulf";
            if(!Tutorial&&wave&&!mission.Complete)
            {
                spawnClock+=Time.deltaTime;int limit=mission.SceneNumber==15?3:5;float interval=Spawned<2?.8f:Spawned<5?10:Spawned<9?8:6;
                if(spawnClock>=interval&&LiveCount<limit&&mission.Progress+LiveCount<mission.Current.required){spawnClock=0;Spawn(false);world.GetComponent<JourneyContinuousBloodstream>().RequestInflux("phagocyte");}
            }
            foreach(var virus in viruses)
            {
                if(!virus.Live||virus.State==JourneyImmuneVirusState.Engaged||virus.State==JourneyImmuneVirusState.BeingPhagocytosed)continue;
                virus.arc+=Time.deltaTime*virus.speed;virus.transform.SetPositionAndRotation(world.mover.path.Offset(virus.arc,virus.laneX,virus.laneY),world.mover.path.Frame(virus.arc)*Quaternion.Euler(0,Time.time*11,0));
                if(virus.arc>world.mover.Distance+15)
                {
                    if(Tutorial){if(!Visible(virus.transform.position))virus.Begin(world.mover.Distance-6,1.1f,.15f,.28f);}
                    else{virus.Return();Missed++;mission.UpdateInnateViralLoad(Mathf.Min(100,mission.ViralLoad+7));}
                }
            }
            foreach(var cell in phagocytes)
            {
                if(!cell||!cell.gameObject.activeInHierarchy)continue;cell.stateAge+=Time.deltaTime;
                if(cell.State==JourneyPhagocyteState.Launched)
                {
                    var from=cell.transform.position;var blood=BloodVelocity(from);var next=from+(cell.launchVelocity+blood)*Time.deltaTime;cell.transform.position=Confine(next);cell.launchVelocity*=Mathf.Exp(-Time.deltaTime*.7f);
                    foreach(var virus in viruses)if(virus.Live&&virus.State!=JourneyImmuneVirusState.Engaged&&virus.State!=JourneyImmuneVirusState.BeingPhagocytosed&&SegmentDistance(virus.transform.position,from,cell.transform.position)<(Tutorial?.60f:.42f)){BeginEngulf(cell,virus);break;}
                    if(cell.State==JourneyPhagocyteState.Launched&&cell.stateAge>2.8f){cell.SetState(JourneyPhagocyteState.Recovering);world.bloodEntities.stream.ResumeLease(cell.CellIndex,cell.transform.position);}
                }
                else if(cell.State==JourneyPhagocyteState.Recovering)
                {
                    cell.transform.position=Vector3.Lerp(cell.transform.position,world.bloodEntities.stream.flow.cells[cell.CellIndex].position,1-Mathf.Exp(-Time.deltaTime*8));
                    if(cell.stateAge>1.1f){cell.SetState(cell.RemainingCapacity>0?JourneyPhagocyteState.Activated:JourneyPhagocyteState.Flowing);cell.target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().enabled=true;cell.target.GetComponent<SphereCollider>().enabled=true;world.bloodEntities.Touch(cell.target);if(cell.RemainingCapacity==0){world.bloodEntities.Release(cell.target);world.bloodEntities.stream.RequestInflux("phagocyte");}}
                }
                if(cell.State!=JourneyPhagocyteState.Phagocytosis)cell.Animate(cell.State==JourneyPhagocyteState.Flowing||cell.State==JourneyPhagocyteState.Scanned||world.freeTools&&cell.State==JourneyPhagocyteState.Captured?0:.25f,Vector3.zero);
            }
            if(engagingCell)TickEngulf();MaximumLive=Mathf.Max(MaximumLive,LiveCount);
            status.gameObject.SetActive(engagingCell||Held);status.text=engagingCell?"ФАГОЦИТОЗ / окружение → поглощение → разрушение":$"ПОЛЕ / натяжение {PullDistance*100:0} см";
            if(status.gameObject.activeSelf){var point=engagingCell?engagingCell.transform.position:Held.transform.position;status.transform.SetPositionAndRotation(point+world.mover.viewCamera.transform.up*.4f,world.mover.viewCamera.transform.rotation);}
        }
        readonly Plane[] visibilityPlanes=new Plane[6];
        bool Visible(Vector3 point){GeometryUtility.CalculateFrustumPlanes(world.mover.viewCamera,visibilityPlanes);return GeometryUtility.TestPlanesAABB(visibilityPlanes,new Bounds(point,Vector3.one));}
        void MoveOutsideEncounter()
        {
            foreach(var cell in phagocytes)
            {
                if(!cell||!world.bloodEntities.Owns(cell.target)||!cell.gameObject.activeInHierarchy)continue;
                if(cell.State==JourneyPhagocyteState.Launched){cell.stateAge+=Time.deltaTime;cell.transform.position=Confine(cell.transform.position+(cell.launchVelocity+BloodVelocity(cell.transform.position))*Time.deltaTime);cell.launchVelocity*=Mathf.Exp(-Time.deltaTime*.7f);if(cell.stateAge>2.8f){cell.SetState(JourneyPhagocyteState.Recovering);world.bloodEntities.stream.ResumeLease(cell.CellIndex,cell.transform.position);}}
                else if(cell.State==JourneyPhagocyteState.Recovering){cell.stateAge+=Time.deltaTime;cell.transform.position=Vector3.Lerp(cell.transform.position,world.bloodEntities.stream.flow.cells[cell.CellIndex].position,1-Mathf.Exp(-Time.deltaTime*8));if(cell.stateAge>1.1f){cell.SetState(JourneyPhagocyteState.Activated);cell.target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().enabled=true;cell.target.GetComponent<SphereCollider>().enabled=true;world.bloodEntities.Touch(cell.target);}}
            }
        }
        void Spawn(bool tutorial)
        {
            JourneyImmuneVirus virus=null;foreach(var candidate in viruses)if(!candidate.Live){virus=candidate;break;}if(!virus)return;
            float arc=world.mover.Distance+(world.freeTools?3.2f:-(tutorial?3.5f:6f));float a=serial++*2.399963f;virus.target.targetId=tutorial?"virus-study":"incoming-virus";
            virus.Begin(arc,tutorial?.4f:Mathf.Cos(a)*1.1f,tutorial?.15f:Mathf.Sin(a)*.8f,world.freeTools?(tutorial?.055f:.11f):tutorial?.28f:.40f+(serial%4)*.08f);Spawned++;
        }
        public bool HandleInput(float dt,bool scan,bool tool,JourneyTarget hit,Transform aim,bool desktop)
        {
            if(world.freeTools)return FreeScanner(dt,scan,hit);
            if(!Active||!Tutorial&&world.mission.Current?.target!="immune-engulf")return false;ScanProgress=0;
            if(!scan){scanConsumed=false;scanningTarget=null;scanAge=0;}
            var phag=hit?hit.GetComponent<JourneyPhagocyteEntity>():null;var virus=hit?hit.GetComponent<JourneyImmuneVirus>():null;
            if(scan&&scanConsumed&&phag&&(scanningTarget!=hit||scannedGeneration!=phag.Generation)){scanConsumed=false;scanAge=0;scanningTarget=null;}
            if(scan&&!scanConsumed&&(phag&&!phag.Busy&&(!Tutorial||world.mission.StepIndex>=1)||virus&&Tutorial&&world.mission.Current?.target=="virus-study"))
            {
                if(scanningTarget!=hit){scanningTarget=hit;scanAge=0;if(phag&&phag.State==JourneyPhagocyteState.Flowing)phag.SetState(JourneyPhagocyteState.Scanned);}
                scanAge+=dt;ScanProgress=Mathf.Clamp01(scanAge/.9f);
                if(ScanProgress>=1)
                {
                    scanConsumed=true;if(phag)scannedGeneration=phag.Generation;ProcessingScan=true;
                    if(phag&&phag.RemainingCapacity<=0){phag.SetState(JourneyPhagocyteState.Flowing);world.bloodEntities.Release(hit);world.bloodEntities.stream.RequestInflux("phagocyte");world.mission.FeedbackMessage("Клетка возвращается в поток. Найди другую активную клетку.");}
                    else if(phag){phag.SetState(JourneyPhagocyteState.Activated);world.bloodEntities.Touch(hit);if(Tutorial&&world.mission.Current?.target=="phagocyte")world.mission.Accept(StudyAction.Activate,"phagocyte");else world.mission.FeedbackMessage("Иммунная реакция инициирована в модели. Фагоцит готов к захвату.");world.mission.ObserveImmunePhenomenon("Фагоцит / активное состояние","Иммунная реакция инициирована в учебной модели. В организме клетки распознают сигналы и контактируют с материалом; Scanner не является настоящим механизмом активации иммунитета.",Tutorial?"Теперь можно направить эту клетку к чужеродной частице.":"");if(UnityEngine.XR.XRSettings.isDeviceActive){var device=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);if(device.isValid)device.SendHapticImpulse(0,.08f,.04f);}}
                    else{virus.SetState(JourneyImmuneVirusState.Targetable);world.mission.Accept(StudyAction.Scan,"virus-study");world.mission.FeedbackMessage("Обнаружить угрозу недостаточно. Найди в потоке клетку, способную её поглотить.");world.bloodEntities.stream.RequestInflux("phagocyte");}
                    ProcessingScan=false;
                }
            }
            if(tool&&aim)
            {
                if(!Held&&phag&&phag.State==JourneyPhagocyteState.Activated&&(!Tutorial||world.mission.StepIndex>=2))
                {
                    captureAge+=dt;
                    if(captureAge>=.2f){Held=phag;phag.SetState(JourneyPhagocyteState.Captured);holdVelocity=Vector3.zero;handStart=aim.position;handForward=aim.forward;desktopPull=0;PullDistance=0;phag.target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().enabled=false;ProcessingCapture=true;if(Tutorial&&world.mission.Current?.action==StudyAction.Capture)world.mission.Accept(StudyAction.Capture,"phagocyte");ProcessingCapture=false;}
                }
                else if(!Held)captureAge=0;
                if(Held)
                {
                    if(desktop)desktopPull=Mathf.Clamp(desktopPull-JourneyDesktopControls.WheelSteps*.02f,0,.25f);
                    PullDistance=desktop?desktopPull:Mathf.Clamp(Vector3.Dot(handStart-aim.position,handForward),0,.25f);ChargeProgress=Mathf.InverseLerp(.03f,.25f,PullDistance);
                    if(PullDistance>.05f)Held.SetState(JourneyPhagocyteState.Charged);
                    var destination=aim.position+aim.forward*((desktop?.95f:HoldDistance)+PullDistance);Held.transform.position=Vector3.SmoothDamp(Held.transform.position,destination,ref holdVelocity,.15f,3.2f,Mathf.Clamp(dt,0,.05f));
                    trajectory.enabled=true;trajectory.positionCount=Tutorial?16:7;var velocity=LaunchVelocity(aim);var drift=BloodVelocity(Held.transform.position);
                    for(int i=0;i<trajectory.positionCount;i++){float time=i/(float)(trajectory.positionCount-1)*(Tutorial?1.8f:.9f);trajectory.SetPosition(i,Predict(Held.transform.position,velocity,time));}
                }
            }
            else
            {
                captureAge=0;
                if(Held&&previousTool)
                {var cell=Held;Held=null;cell.launchVelocity=LaunchVelocity(aim);LastLaunchVelocity=cell.launchVelocity;LastBloodVelocity=BloodVelocity(cell.transform.position);cell.SetState(JourneyPhagocyteState.Launched);cell.target.GetComponent<SphereCollider>().enabled=false;Launched++;trajectory.enabled=false;world.mission.GetComponent<JourneyInstrumentRig>().PhagocyteReleased();world.mission.FeedbackMessage(Tutorial?"Летит сама клетка. Смотри, как она контактирует с частицей.":"Клетка направлена к чужеродному материалу.");}
            }
            previousTool=tool;previousScan=scan;return true;
        }
        Vector3 LaunchVelocity(Transform aim)=>aim?aim.forward*Mathf.Lerp(.8f,4.2f,Mathf.InverseLerp(.03f,.25f,PullDistance)):Vector3.zero;
        public Vector3 BloodVelocity(Vector3 point){float arc=world.mover.Distance+Vector3.Dot(point-world.mover.path.Centre(world.mover.Distance),world.mover.path.Forward(world.mover.Distance));return world.mover.path.Forward(arc)*1.5f*(Tutorial?.15f:.45f);}
        public Vector3 Predict(Vector3 start,Vector3 velocity,float seconds)
        {for(float time=0;time<seconds;){float dt=Mathf.Min(.04f,seconds-time);start=Confine(start+(velocity+BloodVelocity(start))*dt);velocity*=Mathf.Exp(-dt*.7f);time+=dt;}return start;}
        public bool CanApply(StudyAction action,string id)
        {if(id=="virus-study")return ProcessingScan;if(id=="phagocyte")return action==StudyAction.Capture?ProcessingCapture:ProcessingScan;if(id=="immune-engulf")return ProcessingSuccess;return true;}
        void BeginEngulf(JourneyPhagocyteEntity cell,JourneyImmuneVirus virus)
        {
            if(engagingCell)return;engagingCell=cell;engagingVirus=virus;engulfAge=0;engulfStart=virus.transform.position;cell.SetState(JourneyPhagocyteState.Phagocytosis);virus.SetState(JourneyImmuneVirusState.Engaged);virus.contact.enabled=false;cell.launchVelocity=Vector3.zero;
        }
        void TickEngulf()
        {
            if(world.freeTools){TickDamage();return;}
            engulfAge+=Time.deltaTime;var cell=engagingCell;var virus=engagingVirus;float phase=Mathf.Clamp01(engulfAge/1.8f);var offset=engulfStart-cell.transform.position;
            cell.Animate(Mathf.Sin(phase*Mathf.PI),cell.transform.InverseTransformDirection(offset));virus.SetState(JourneyImmuneVirusState.BeingPhagocytosed);
            var surface=cell.transform.position+offset.normalized*.28f;virus.transform.position=phase<.4f?Vector3.Lerp(engulfStart,surface,Mathf.SmoothStep(0,1,phase/.4f)):Vector3.Lerp(surface,cell.transform.position,Mathf.SmoothStep(0,1,(phase-.4f)/.6f));
            virus.transform.localScale=virus.initialScale*Mathf.Lerp(1,.18f,Mathf.SmoothStep(.45f,1,phase));
            if(phase<1)return;virus.Return();cell.Used();cell.SetState(JourneyPhagocyteState.Recovering);world.bloodEntities.stream.ResumeLease(cell.CellIndex,cell.transform.position);Neutralized++;engagingCell=null;engagingVirus=null;
            if(!Tutorial)world.mission.UpdateInnateViralLoad(world.mission.SceneNumber==15?Mathf.Max(72,world.mission.ViralLoad-7):Mathf.Max(0,world.mission.ViralLoad-7));
            // Twelve contacts establish the learned cycle, but escaped material must
            // also be cleared. Keep the final goal open for real additional engulfs.
            bool cleared=world.mission.SceneNumber!=9||world.mission.Current==null||world.mission.Progress<world.mission.Current.required-1||world.mission.ViralLoad<=0;
            ProcessingSuccess=true;if(world.mission.Current?.target=="immune-engulf"&&cleared)world.mission.Accept(StudyAction.Direct,"immune-engulf");ProcessingSuccess=false;
            if(Tutorial){tutorialCompleted=true;world.RememberEpisode();world.mission.UpdateInnateViralLoad(0);world.mission.FeedbackMessage("Клетка окружила и поглотила частицу. Фагоцитоз — один из механизмов врождённой защиты; реальный противовирусный ответ значительно сложнее.");}
            else if(!cleared)world.mission.FeedbackMessage("Вирусная нагрузка ещё повышена. Продолжай направлять клетки к оставшемуся материалу.");
            world.mission.ObserveImmunePhenomenon("Фагоцитоз","Клетка окружила и поглотила чужеродный материал. Затем он разрушается внутри клетки. Это один из механизмов врождённой защиты; реальный противовирусный ответ значительно сложнее.",Tutorial?"Ты наблюдал контакт, окружение и поглощение — этот процесс называется фагоцитозом.":"");
        }
        public void CancelHold(){if(Held){Held.SetState(JourneyPhagocyteState.Activated);world.bloodEntities.stream.ResumeLease(Held.CellIndex,Held.transform.position);Held.target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().enabled=true;Held.target.GetComponent<SphereCollider>().enabled=true;Held=null;}trajectory.enabled=false;previousTool=false;}
        public void Captured(JourneyPhagocyteEntity cell)
        {
            if(!cell)return;cell.SetState(JourneyPhagocyteState.Captured);ActivationProgress=0;
            ProcessingCapture=true;if(Tutorial&&world.mission.Current?.action==StudyAction.Capture)world.mission.Accept(StudyAction.Capture,"phagocyte");ProcessingCapture=false;
        }
        bool FreeScanner(float dt,bool scan,JourneyTarget hit)
        {
            if(!Active)return false;
            if(!scan){scanAge=0;ScanProgress=0;scanningTarget=null;scanConsumed=false;return false;}
            var cell=hit?hit.GetComponent<JourneyPhagocyteEntity>():null;
            if(!cell||world.freeTools.Held!=hit){ScanProgress=0;scanAge=0;return false;}
            if(scanningTarget!=hit){scanningTarget=hit;scanAge=0;scanConsumed=false;}
            if(!scanConsumed){scanAge+=dt;ScanProgress=Mathf.Clamp01(scanAge/.75f);if(ScanProgress>=1){cell.Studied=true;scanConsumed=true;ProcessingScan=true;if(Tutorial&&world.mission.Current?.action==StudyAction.Scan&&world.mission.Current.target=="phagocyte")world.mission.Accept(StudyAction.Scan,"phagocyte");ProcessingScan=false;world.mission.FeedbackMessage("Фагоцит распознан. Переключи BioTool в АКТИВАЦИЮ и удержи курок.");}}
            return true;
        }
        public void Activate(JourneyPhagocyteEntity cell,float dt)
        {
            if(cell&&cell.RemainingCapacity<=0){world.mission.FeedbackMessage("Эта клетка уже использована. Отпусти её и найди свежий фагоцит в потоке.");return;}
            if(!cell||!cell.Studied){world.mission.FeedbackMessage("Сначала просканируй захваченный фагоцит.");return;}
            if(cell.State==JourneyPhagocyteState.Activated)return;
            ActivationProgress=Mathf.Clamp01(ActivationProgress+dt/.65f);if(ActivationProgress<1)return;
            cell.SetState(JourneyPhagocyteState.Activated);ProcessingScan=true;if(Tutorial&&world.mission.Current?.action==StudyAction.Activate)world.mission.Accept(StudyAction.Activate,"phagocyte");ProcessingScan=false;
            world.mission.FeedbackMessage("Клетка активирована. РЕВЕРС: удержи курок, наведи на вирус и отпусти.");
        }
        public void Preview(JourneyPhagocyteEntity cell,Transform aim,float charge)
        {
            trajectory.enabled=cell.Studied&&cell.State==JourneyPhagocyteState.Activated&&cell.RemainingCapacity>0;trajectory.positionCount=Tutorial?20:10;
            var velocity=aim.forward*Mathf.Lerp(1.5f,5,charge);for(int i=0;i<trajectory.positionCount;i++)trajectory.SetPosition(i,Predict(cell.transform.position,velocity,i/(float)(trajectory.positionCount-1)*1.8f));
        }
        public void LaunchFromField(JourneyPhagocyteEntity cell,Transform aim,float charge)
        {
            trajectory.enabled=false;if(!cell.Studied||cell.State!=JourneyPhagocyteState.Activated||cell.RemainingCapacity<=0){world.bloodEntities.Release(cell.target,true);world.mission.FeedbackMessage("Для запуска нужен свежий, просканированный и активированный фагоцит.");return;}
            cell.launchVelocity=aim.forward*Mathf.Lerp(1.5f,5,charge);LastLaunchVelocity=cell.launchVelocity;LastBloodVelocity=BloodVelocity(cell.transform.position);cell.SetState(JourneyPhagocyteState.Launched);cell.target.GetComponent<SphereCollider>().enabled=false;cell.target.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().enabled=false;Launched++;world.mission.GetComponent<JourneyInstrumentRig>().PhagocyteReleased();
        }
        void TickDamage()
        {
            engulfAge+=Time.deltaTime;var cell=engagingCell;var virus=engagingVirus;float t=Mathf.Clamp01(engulfAge/.9f);
            cell.Animate(Mathf.Sin(t*Mathf.PI),(virus.transform.position-cell.transform.position).normalized);virus.transform.localScale=virus.initialScale*(1-.1f*Mathf.Sin(t*Mathf.PI));
            if(t<1)return;virus.Damage();DamageContacts++;cell.RestoreCapacity(0);cell.SetState(JourneyPhagocyteState.Recovering);world.bloodEntities.stream.ResumeLease(cell.CellIndex,cell.transform.position);
            if(virus.Integrity==0){virus.Return();Neutralized++;}else{virus.SetState(JourneyImmuneVirusState.Targetable);virus.contact.enabled=true;virus.transform.localScale=virus.initialScale*Mathf.Lerp(.7f,1,virus.Integrity/3f);}
            if(Tutorial){world.mission.UpdateInnateViralLoad(virus.Integrity/3f*25);if(virus.Integrity==0){tutorialCompleted=true;world.RememberEpisode();}}
            else world.mission.UpdateInnateViralLoad(Mathf.Max(world.mission.SceneNumber==15?72:0,world.mission.ViralLoad-25f/12));
            engagingCell=null;engagingVirus=null;ProcessingSuccess=true;var goal=world.mission.Current;
            bool finalAllowed=world.mission.SceneNumber!=9||world.mission.Progress<goal.required-1||world.mission.ViralLoad<=0;
            if(goal?.target=="immune-engulf"&&(Tutorial||virus.Integrity==0)&&finalAllowed)world.mission.Accept(StudyAction.Direct,"immune-engulf");ProcessingSuccess=false;
            if(Tutorial&&tutorialCompleted)world.RememberEpisode();
            world.bloodEntities.stream.RequestInflux("phagocyte");world.mission.FeedbackMessage(virus.Integrity==0?"Вирусная частица нейтрализована.":"Контакт ослабил частицу. Найди следующий фагоцит в потоке.");
        }
        Vector3 Confine(Vector3 point)
        {float arc=world.mover.Distance;var centre=world.mover.path.Centre(arc);var forward=world.mover.path.Forward(arc);float along=Vector3.Dot(point-centre,forward);var radial=point-centre-forward*along;return centre+forward*along+Vector3.ClampMagnitude(radial,world.mover.path.Radius(arc)-.25f);}
        static float SegmentDistance(Vector3 point,Vector3 a,Vector3 b){var delta=b-a;float t=Mathf.Clamp01(Vector3.Dot(point-a,delta)/Mathf.Max(.0001f,delta.sqrMagnitude));return Vector3.Distance(point,a+delta*t);}
    }
}
