using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    public enum ImmunePatrolStage{Discovery,Scan,Reveal,Targeting,Response,Complete}
    /// <summary>Scene10 stage layered on Mission, existing cutaway, spline and the real visual WBC pool.</summary>
    [DefaultExecutionOrder(600)]
    public sealed class JourneyImmunePatrol:MonoBehaviour
    {
        public JourneyWorld world;
        public GameObject root;
        public JourneyTarget wallTarget,revealTarget;
        public Renderer alteredWall;
        public JourneyImmuneFocus[] focuses;
        public Mesh colliderWindow;
        public MeshFilter[] responderBodies;
        public LineRenderer signal;
        public bool Active=>world.mission.SceneNumber==10;
        public ImmunePatrolStage Stage{get;private set;}
        public int Treated{get;private set;}
        public int TotalResponders{get;private set;}
        public int MaximumResponders{get;private set;}
        public bool ReadyToLeave=>Treated==6&&reveal<=.001f&&groupCount==0;
        public bool ProcessingScan{get;private set;}
        public bool ProcessingTreatment{get;private set;}
        public float ScanProgress=>Mathf.Clamp01(scanAge/.75f);
        public float TargetProgress=>Mathf.Clamp01(targetAge/.6f);
        public float RevealProgress=>reveal;
        public JourneyImmuneFocus CurrentFocus{get;private set;}
        public float Station=>world.mover.path.Anchor(10)+3.2f;
        float reveal,scanAge,targetAge,responseAge;
        int scene=-1,groupCount;
        JourneyTarget scanTarget;
        JourneyImmuneFocus aiming;
        readonly int[] sourceIndices={-1,-1,-1,-1,-1};
        readonly Vector3[] origins=new Vector3[5];
        readonly Vector3[] sizes=new Vector3[5];
        bool didExplainContact;
        bool scanNeedsRelease;
        MaterialPropertyBlock activatedTint;
        JourneyToolFeedback feedback;
        bool previousTool;
        public void ResetRun(){ReturnGroup();Treated=TotalResponders=MaximumResponders=0;reveal=scanAge=targetAge=0;CurrentFocus=null;aiming=null;scanTarget=null;scanNeedsRelease=previousTool=didExplainContact=false;if(focuses!=null)foreach(var focus in focuses)if(focus)focus.ResetFocus();}
        public void EnterStage()
        {
            ResetRun();Stage=ImmunePatrolStage.Discovery;root.SetActive(true);feedback=world.mission.GetComponent<JourneyToolFeedback>();
            foreach(var probe in world.hemodynamics.probes)probe.gameObject.SetActive(false);world.hemodynamics.arRoot.SetActive(false);world.hemodynamics.toneContact.gameObject.SetActive(false);
            foreach(var id in new[]{"pressure-low","pressure-stable","pressure-return","tone-mode","tone","hdx-final","hdx-diameter"}){var old=world.Find(id);if(old)old.gameObject.SetActive(false);}
            foreach(var target in new[]{wallTarget,revealTarget}){target.transform.position=alteredWall.transform.position;target.SetHomePose();}
            world.mission.FeedbackMessage("Кровоток обычный. Осмотри изменённую структуру стенки и удержи левый курок Scanner.");
        }
        void Update()
        {
            if(world.mission.Goals==null)return;
            if(scene!=world.mission.SceneNumber){if(scene==10){ReturnGroup();world.layerInspection.EndExternalReveal();}scene=world.mission.SceneNumber;scanAge=targetAge=0;if(Active){Stage=ImmunePatrolStage.Discovery;world.mission.FeedbackMessage("На стенке необычный участок. Осмотрись и проверь Scanner.");}}
            root.SetActive(Active);if(!Active){signal.enabled=false;return;}
            if(!world.mission.Ready)return;
            float wanted=world.mission.StepIndex>=2&&Treated<6?1:0;reveal=Mathf.MoveTowards(reveal,wanted,Time.deltaTime/.85f);
            if(world.mission.StepIndex>=2)world.layerInspection.ExternalReveal(Station,reveal,colliderWindow);
            wallTarget.gameObject.SetActive(world.mission.StepIndex==0);revealTarget.gameObject.SetActive(world.mission.StepIndex==1);wallTarget.GetComponent<Collider>().enabled=world.mission.StepIndex==0;revealTarget.GetComponent<Collider>().enabled=world.mission.StepIndex==1;
            if(Treated==6){Stage=ImmunePatrolStage.Complete;if(reveal<=.001f)world.layerInspection.EndExternalReveal();}
            else if(world.mission.StepIndex==0)Stage=ImmunePatrolStage.Discovery;else if(world.mission.StepIndex==1)Stage=ImmunePatrolStage.Reveal;else if(!CurrentFocus)Stage=ImmunePatrolStage.Targeting;
            if(alteredWall)alteredWall.enabled=Treated<6&&reveal<.4f;
            if(CurrentFocus)TickResponse();RefreshFocuses();
        }
        bool Allowed(int index)=>index>=0&&index<focuses.Length&&focuses[index].State!=ImmuneFocusState.Treated;
        void RefreshFocuses()
        {
            var view=world.mover.viewCamera.transform;
            foreach(var focus in focuses)
            {
                bool eligible=Allowed(focus.index)&&focus.State!=ImmuneFocusState.Treated;if(focus.State==ImmuneFocusState.Hidden&&eligible&&reveal>.8f)focus.State=ImmuneFocusState.Available;
                focus.target.gameObject.SetActive(reveal>.1f&&focus.State!=ImmuneFocusState.Treated);
                int closer=0;float angle=Vector3.Angle(view.forward,focus.transform.position-view.position);foreach(var other in focuses)if(other!=focus&&other.State!=ImmuneFocusState.Treated&&Vector3.Angle(view.forward,other.transform.position-view.position)<angle)closer++;
                var contact=focus.target.GetComponent<Collider>();contact.enabled=eligible&&reveal>.95f&&!CurrentFocus&&closer<3;
                focus.Present(reveal>.2f&&closer<3,focus==aiming||focus==CurrentFocus);
            }
        }
        public bool Handle(float dt,bool scan,bool tool,JourneyTarget hit,Transform aim,BioToolMode mode)
        {
            if(!Active)return false;signal.enabled=false;
            if(tool&&mode!=BioToolMode.Reverse&&!scan){if(!previousTool)world.mission.FeedbackMessage("В этом участке нужен РЕВЕРС: A / B переключают режимы. Наведи сигнал на очаг, а не на сосуд.");previousTool=true;return true;}previousTool=tool;
            if(world.mission.StepIndex<2)
            {
                if(!scan){scanNeedsRelease=false;scanAge=0;scanTarget=null;return false;}if(scanNeedsRelease)return true;
                var desired=world.mission.StepIndex==0?wallTarget:revealTarget;
                if(scan&&hit==desired){Stage=ImmunePatrolStage.Scan;if(scanTarget!=hit){scanTarget=hit;scanAge=0;}scanAge+=dt;if(feedback)feedback.PresentLock(hit,ScanProgress,true,true,world.mission.GetComponent<JourneyInput>().scanner);if(scanAge>=.75f){ProcessingScan=true;world.mission.Accept(StudyAction.Scan,desired.targetId);ProcessingScan=false;scanAge=0;scanNeedsRelease=true;world.mission.GetComponent<JourneyInstrumentRig>().ScannerActivated(desired.transform.position);world.mission.ObservePatrol("ИММУННЫЙ НАДЗОР","В организме постоянно возникают клетки с повреждениями и изменениями ДНК. Большинство устраняется естественными защитными механизмами.\n\nNK-клетки и цитотоксические лимфоциты распознают некоторые аномальные клетки по изменениям поверхности и запускают их контролируемую гибель — апоптоз.\n\nСначала иммунной клетке нужны сигнал и путь в поражённую ткань.","ОБНАРУЖИТЬ → НАПРАВИТЬ → АКТИВИРОВАТЬ → УСТРАНИТЬ. Отпусти Scanner; повторное сканирование участка откроет глубокий слой.");}}
                else{scanAge=0;scanTarget=null;}return true;
            }
            if(CurrentFocus||Treated==6)return true;
            var focus=hit?hit.GetComponent<JourneyImmuneFocus>():null;
            if(tool&&!scan&&mode==BioToolMode.Reverse&&focus&&Allowed(focus.index)&&focus.State==ImmuneFocusState.Available&&reveal>.95f)
            {
                if(aiming!=focus){aiming=focus;targetAge=0;}targetAge+=dt;signal.enabled=true;signal.positionCount=2;signal.SetPosition(0,aim.position);signal.SetPosition(1,focus.transform.position);
                if(feedback)feedback.PresentLock(hit,TargetProgress,true,false,aim);
                if(targetAge>=.6f){CurrentFocus=focus;focus.State=ImmuneFocusState.Targeted;Stage=ImmunePatrolStage.Response;responseAge=0;groupCount=0;targetAge=0;world.mission.FeedbackMessage("Локальный сигнал принят. Клетки выходят из потока к очагу.");}
            }
            else{targetAge=0;aiming=null;}return true;
        }
        void TickResponse()
        {
            var stream=world.bloodEntities.stream;
            if(groupCount<CurrentFocus.responders)
            {
                if(stream.LeaseLeukocyte(CurrentFocus.surfacePoint.position,out int index))
                {
                    int n=groupCount++;sourceIndices[n]=index;origins[n]=stream.flow.cells[index].position;sizes[n]=stream.flow.cells[index].localScale;responderBodies[n].sharedMesh=stream.flow.cells[index].GetComponent<MeshFilter>().sharedMesh;responderBodies[n].transform.SetPositionAndRotation(origins[n],stream.flow.cells[index].rotation);responderBodies[n].transform.localScale=sizes[n];responderBodies[n].gameObject.SetActive(true);TotalResponders++;MaximumResponders=Mathf.Max(MaximumResponders,groupCount);
                }
                return;
            }
            responseAge+=Time.deltaTime;float t=responseAge;
            for(int i=0;i<groupCount;i++)
            {
                var wall=CurrentFocus.surfacePoint.position;var inner=wall-world.mover.path.Right(Station)*.20f;var contact=CurrentFocus.transform.position+world.mover.path.Up(Station)*Mathf.Sin(i*2.4f)*.25f+world.mover.path.Forward(Station)*Mathf.Cos(i*2.4f)*.25f;
                float approach=Mathf.SmoothStep(0,1,Mathf.Clamp01(t));var control=origins[i]+world.mover.path.Forward(Station)*.8f+(inner-origins[i])*.2f;var curved=origins[i]*(1-approach)*(1-approach)+control*2*approach*(1-approach)+inner*approach*approach;
                Vector3 point=t<1?curved:t<1.5f?inner:t<2.5f?Vector3.Lerp(inner,wall+world.mover.path.Right(Station)*.20f,Mathf.SmoothStep(0,1,(t-1.5f))):Vector3.Lerp(wall+world.mover.path.Right(Station)*.20f,contact,Mathf.SmoothStep(0,1,(t-2.5f)/1.2f));
                responderBodies[i].transform.position=point;responderBodies[i].transform.localScale=Vector3.Scale(sizes[i],t>1.5f&&t<2.5f?new Vector3(.45f,1.15f,1.15f):Vector3.one);
                activatedTint??=new MaterialPropertyBlock();activatedTint.SetColor("_BaseColor",t>2.5f?new Color(.73f,.9f,1,1):Color.white);responderBodies[i].GetComponent<Renderer>().SetPropertyBlock(activatedTint);
            }
            if(t>3.7f){if(!didExplainContact){didExplainContact=true;world.mission.ObservePatrol("ЦИТОТОКСИЧЕСКИЙ КОНТАКТ","ПЕРФОРИН / ГРАНЗИМЫ → АПОПТОЗ\n\nПосле контакта аномальные клетки уменьшаются; их фрагменты удаляются. Иммунные клетки активируются, сохраняя свой тип.","Направляющий сигнал принят. Дальнейший ответ выполняют клетки организма.");}CurrentFocus.Shrink(Mathf.Clamp01((t-3.7f)/1.2f));}
            if(t<5.8f)return;CurrentFocus.State=ImmuneFocusState.Treated;Treated++;ReturnGroup();CurrentFocus=null;ProcessingTreatment=true;world.mission.Accept(StudyAction.Direct,"patrol-focus");ProcessingTreatment=false;
            if(Treated==6)world.mission.ObservePatrol("ИММУННЫЙ НАДЗОР ЗАВЕРШЁН","Иммунная система постоянно контролирует состояние тканей. Некоторые её клетки способны распознавать и уничтожать аномальные клетки ещё до формирования крупного патологического очага.","Иммунитет — это не только борьба с инфекциями. Он также контролирует собственные изменённые клетки организма.");
        }
        void ReturnGroup(){for(int i=0;i<5;i++){if(sourceIndices[i]>=0&&world&&world.bloodEntities){world.bloodEntities.stream.ReturnResponder(sourceIndices[i]);sourceIndices[i]=-1;}if(responderBodies!=null&&i<responderBodies.Length&&responderBodies[i])responderBodies[i].gameObject.SetActive(false);}groupCount=0;}
        public bool CanApply(StudyAction action,string id)=>action==StudyAction.Scan?ProcessingScan:ProcessingTreatment;
        void OnDisable(){ReturnGroup();if(world&&world.layerInspection)world.layerInspection.EndExternalReveal();}
    }
}
