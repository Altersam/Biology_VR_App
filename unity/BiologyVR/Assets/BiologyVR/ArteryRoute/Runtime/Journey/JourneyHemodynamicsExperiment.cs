using System;
using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>In-vessel controlled-flow experiment and later independent pressure diagnosis.</summary>
    public sealed class JourneyHemodynamicsExperiment:MonoBehaviour
    {
        public JourneyWorld world;
        public GameObject arRoot,cardBoard;
        public JourneyTarget[] probes;
        public JourneyTarget toneContact;
        public Transform cardSlot;
        public JourneyHemodynamicsCard[] cards;
        public LineRenderer[] rings,streamlines;
        public Text[] readings;
        public Text sectionCaption,formulaCaption;
        public Transform[] tracers;
        public JourneyHemodynamicsInterface display;
        public float RadiusRatio{get;private set;}=1;
        public float ResistanceRatio=>1/Mathf.Pow(RadiusRatio,4);
        public float P1=>Active&&Case==3?(Late?92:100):120;
        public float DeltaP=>world.freeTools?8:Case==3?5:8*ResistanceRatio;
        public float P2=>P1-DeltaP;
        public float FlowRatio=>world.freeTools?Mathf.Pow(RadiusRatio,4):Case==3?DeltaP/8:1;
        public float VelocityRatio=>FlowRatio/(RadiusRatio*RadiusRatio);
        public float DiameterMm=>4.8f*RadiusRatio;
        public bool Active=>world&&world.mission&&(world.mission.SceneNumber==5||world.mission.SceneNumber==10);
        public bool Late=>world.mission.SceneNumber==10;
        public int Case{get;private set;}
        public bool Adjusting{get;private set;}
        public float Stability{get;private set;}
        public int Measurements{get;private set;}
        public int Mistakes{get;private set;}
        public int AcceptedHypotheses{get;private set;}
        public int TracerBursts{get;private set;}
        public float SectionArc=>world.freeTools?world.mover.Distance+3.2f:world.mover.path.Anchor(Late?10:5)+3.2f;
        public bool FormulaDiscovered{get;private set;}
        public bool ProcessingHypothesis{get;private set;}
        public bool Critical=>RadiusRatio<.65f||RadiusRatio>1.45f;
        public string Title=>Late?"ДИАГНОСТИКА НИЗКОГО АД":Case==0?"ГЕМОДИНАМИЧЕСКИЙ ЭКСПЕРИМЕНТ":Case==1?"ОПЫТ 1 / СОПРОТИВЛЕНИЕ":Case==2?"ОПЫТ 2 / ЛОКАЛЬНАЯ СКОРОСТЬ":"ОПЫТ 3 / ПРОВЕРКА ГИПОТЕЗЫ";
        public string Summary=>Late?"Собери данные участка. Выбери обоснованное действие.":Case==0?"Поток возникает из-за разности давления. Измерь участок A → B.":Case<3?"Расход в учебном участке фиксирован. Настрой геометрию и сравни измерения.":"АД снизилось. Сначала измерь диаметр, скорость и давление A/B.";
        readonly bool[] seen=new bool[5];
        float revealUntil,tracerAge=100,stable,previousRadius,handRadius,desktopOffset;
        readonly float[] tracerArc=new float[7];
        Vector3 handStart;
        Vector3[] probeOrigins,readingOrigins;
        Vector3 captionOrigin;
        int lastScene=-1;
        bool previousTool,previousScan;
        void CacheGraphics()
        {
            if(probeOrigins!=null)return;probeOrigins=new Vector3[probes.Length];readingOrigins=new Vector3[readings.Length];
            for(int i=0;i<probes.Length;i++)probeOrigins[i]=probes[i].transform.localPosition;
            for(int i=0;i<readings.Length;i++)readingOrigins[i]=readings[i].transform.localPosition;
            captionOrigin=sectionCaption.transform.localPosition;
        }
        void Update()
        {
            if(world&&world.freeTools){UpdateFieldLesson();return;}
            if(!world||world.mission.Goals==null)return;
            var m=world.mission;
            if(lastScene!=m.SceneNumber)
            {
                lastScene=m.SceneNumber;Adjusting=false;previousTool=previousScan=false;Stability=stable=0;
                if(Active){Case=Late?3:0;RadiusRatio=previousRadius=1;Array.Clear(seen,0,seen.Length);Measurements=0;FormulaDiscovered=false;foreach(var card in cards)card.ResetCard();}
            }
            arRoot.SetActive(Active);if(!Active){Shader.SetGlobalFloat("_HemoRadius",1);return;}
            if(world.flowModel)world.flowModel.gameObject.SetActive(false);
            foreach(var id in new[]{"flow","flow-model","normal-flow","small-radius","large-radius","pressure","pressure-low","pressure-stable","pressure-return","tone-mode","tone"}){var legacy=world.Find(id);if(legacy)legacy.gameObject.SetActive(false);}
            if(!m.Ready)return;
            int wanted=Late?3:m.Complete?0:m.StepIndex<4?0:m.StepIndex<6?1:m.StepIndex<8?2:3;
            if(wanted!=Case)
            {
                Case=wanted;RadiusRatio=previousRadius=Case==1?.73f:Case==2?1.32f:1;
                Array.Clear(seen,0,seen.Length);Measurements=0;Stability=stable=0;Adjusting=false;
                if(Case==3)foreach(var card in cards)card.ResetCard();
            }
            float pressure=Case==3?(Late?92:100):120;
            if(m.Complete&&!Late)pressure=120;
            m.UpdateHemodynamicState(RadiusRatio,pressure);
            arRoot.transform.SetPositionAndRotation(world.mover.path.Centre(SectionArc),world.mover.path.Frame(SectionArc));
            toneContact.transform.position=world.mover.path.Offset(SectionArc,world.mover.path.Radius(SectionArc)*Mathf.Min(1,RadiusRatio)-.35f,-.2f);
            UpdateProbes();UpdateGraphics();
        }
        void UpdateProbes()
        {
            var m=world.mission;string id=m.Current?.target;
            foreach(var probe in probes)
            {
                probe.gameObject.SetActive(true);
                foreach(var contact in probe.GetComponents<Collider>())contact.enabled=m.Ready&&id==probe.targetId;
            }
            toneContact.gameObject.SetActive(true);bool control=m.Current?.action==StudyAction.Radius;
            foreach(var collider in toneContact.GetComponents<Collider>())collider.enabled=control;
            cardBoard.SetActive(Case==3&&m.Current?.action==StudyAction.Bind);
            if(cardBoard.activeSelf)foreach(var card in cards){var text=card.GetComponentInChildren<Text>(true);if(text)text.text=Late?(card.code==0?"СУЗИТЬ\nУЧАСТОК":card.code==1?"РАСШИРИТЬ\nУЧАСТОК":"ПРОДОЛЖИТЬ\nДИАГНОСТИКУ"):(card.code==0?"СУЖЕНИЕ\nУЧАСТКА":card.code==1?"РАСШИРЕНИЕ\nУЧАСТКА":"ПРИЧИНА\nНЕ ЗДЕСЬ");}
        }
        public void ObserveScanner(bool held,JourneyTarget hit)
        {
            if(!Active){previousScan=false;return;}
            if(held){revealUntil=Time.time+4;if(!previousScan){TracerBursts++;tracerAge=0;for(int i=0;i<tracerArc.Length;i++)tracerArc[i]=-2.6f-i*.23f;}}
            previousScan=held;
        }
        public bool Control(float dt,bool held,Transform aim,JourneyTarget hit,bool desktop)
        {
            if(world.freeTools)return ControlFieldLesson(dt,held,aim,desktop);
            var m=world.mission;if(!Active||m.Current?.action!=StudyAction.Radius){previousTool=held;Adjusting=false;return false;}
            if(!held||!aim){Adjusting=false;Stability=stable=0;previousTool=false;return true;}
            if(!Adjusting)
            {
                if(previousTool||hit!=toneContact){previousTool=true;return true;}
                Adjusting=true;handStart=world.mover.origin.transform.InverseTransformPoint(aim.position);handRadius=RadiusRatio;desktopOffset=0;previousRadius=RadiusRatio;
                m.FeedbackMessage(desktop?"Удерживай E: колесо меняет радиус. Добейся нормального диаметра и проверь сканером.":"Удерживай Trigger: подними руку, чтобы расширить участок; опусти, чтобы сузить. Удержи стабильную настройку.");
            }
            if(desktop){desktopOffset+=JourneyDesktopControls.WheelSteps*.025f;RadiusRatio=Mathf.Clamp(handRadius+desktopOffset,.65f,1.45f);}
            else
            {
                var hand=world.mover.origin.transform.InverseTransformPoint(aim.position);
                var up=world.mover.origin.transform.InverseTransformDirection(world.mover.path.Up(SectionArc));
                RadiusRatio=Mathf.Clamp(handRadius+Vector3.Dot(hand-handStart,up)*.85f,.65f,1.45f);
            }
            float rate=Mathf.Abs(RadiusRatio-previousRadius)/Mathf.Max(.0001f,dt);previousRadius=RadiusRatio;
            stable=RadiusRatio>=.95f&&RadiusRatio<=1.05f&&rate<.10f?stable+dt:0;Stability=Mathf.Clamp01(stable/.9f);
            previousTool=true;revealUntil=Time.time+4;
            if(Stability>=1){m.Accept(StudyAction.Radius,m.Current.target);Adjusting=false;FormulaDiscovered=true;}
            return true;
        }
        public bool CanApply(StudyAction action,string id)
        {
            if(world.freeTools)return Active&&(action==StudyAction.Scan||action==StudyAction.Radius&&Adjusting);
            if(!Active||!id.StartsWith("hdx-"))return false;
            if(action==StudyAction.Radius)return Adjusting&&Stability>=1;
            if(action==StudyAction.Bind)return ProcessingHypothesis&&Measurements>=(Late?5:4)&&Mathf.Abs(RadiusRatio-1)<.001f;
            if(id=="hdx-check1"||id=="hdx-check2")return RadiusRatio>=.95f&&RadiusRatio<=1.05f;
            return action==StudyAction.Scan;
        }
        public void Accepted(StudyAction action,string id)
        {
            if(action==StudyAction.Scan)
            {
                int index=id=="hdx-p1"?0:id=="hdx-p2"?1:id=="hdx-speed"?2:id=="hdx-diameter"?3:id=="hdx-wall"?4:-1;
                if(index>=0&&!seen[index]){seen[index]=true;Measurements++;}
                revealUntil=Time.time+5;
            }
        }
        public bool ResolveCard(int code)
        {
            if(!Active||world.mission.Current?.action!=StudyAction.Bind)return false;
            if(code!=2){Mistakes++;world.mission.RecordPhysicalError(Late?"Данные не подтверждают локальную причину. Проверь выбранное действие по диаметру, скорости, стенке и давлениям.":"Диаметр нормальный, а давления A и B снижены вместе: изменение местного радиуса не устраняет снижение притока.");return false;}
            ProcessingHypothesis=true;bool accepted=world.mission.Accept(StudyAction.Bind,"hdx-hypothesis");ProcessingHypothesis=false;
            if(accepted){AcceptedHypotheses++;world.mission.FeedbackMessage(Late?"Локальный диаметр и стенка нормальны. Продолжи поиск источника потери давления дальше по сосуду.":"Верно: причина не в этом участке. Не каждое низкое давление исправляется изменением радиуса.");}
            return accepted;
        }
        public float LocalRadius(float arc)
        {if(!Active&&!world.freeTools)return 1;float weight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1.1f,Mathf.Abs(arc-SectionArc)));return Mathf.Lerp(1,RadiusRatio,weight);}
        void UpdateFieldLesson()
        {
            var m=world.mission;if(m.Goals==null)return;
            if(lastScene!=m.SceneNumber){lastScene=m.SceneNumber;RadiusRatio=1;Adjusting=false;stable=0;Case=0;}
            arRoot.SetActive(false);cardBoard.SetActive(false);if(!Active){Shader.SetGlobalFloat("_HemoRadius",RadiusRatio);Shader.SetGlobalVector("_HemoCentreWS",world.mover.path.Centre(SectionArc));Shader.SetGlobalVector("_HemoAxisWS",world.mover.path.Forward(SectionArc));return;}
            foreach(var reading in readings)reading.gameObject.SetActive(false);
            foreach(var probe in probes)probe.gameObject.SetActive(false);
            toneContact.gameObject.SetActive(true);toneContact.transform.position=world.mover.path.Offset(SectionArc,world.mover.path.Radius(SectionArc)*RadiusRatio,0);
            foreach(var contact in toneContact.GetComponents<Collider>())contact.enabled=true;
            // One invisible contact covers the actual wall; measurements stay on the screens.
            var sphere=toneContact.GetComponent<SphereCollider>();if(sphere){sphere.center=Vector3.zero;sphere.radius=1.1f;}
            var diameter=world.Find("hdx-diameter");diameter.gameObject.SetActive(m.Current?.target=="hdx-diameter");diameter.transform.position=toneContact.transform.position;
            foreach(var contact in diameter.GetComponents<Collider>())contact.enabled=m.Current?.target=="hdx-diameter";
            var scanSphere=diameter.GetComponent<SphereCollider>();if(scanSphere)scanSphere.radius=1.15f;
            var final=world.Find("hdx-final");if(final){final.gameObject.SetActive(m.Current?.target=="hdx-final");final.transform.position=toneContact.transform.position;var c=final.GetComponent<SphereCollider>();if(c){c.radius=1.15f;c.enabled=m.Current?.target=="hdx-final";}}
            foreach(var r in toneContact.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            foreach(var r in diameter.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            if(final)foreach(var r in final.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            float pressure=(Late?92:120)/Mathf.Pow(RadiusRatio,2);m.UpdateHemodynamicState(RadiusRatio,pressure);
            Shader.SetGlobalVector("_HemoCentreWS",world.mover.path.Centre(SectionArc));Shader.SetGlobalVector("_HemoAxisWS",world.mover.path.Forward(SectionArc));Shader.SetGlobalFloat("_HemoRadius",RadiusRatio);
        }
        bool ControlFieldLesson(float dt,bool held,Transform aim,bool desktop)
        {
            if(!held||!aim){Adjusting=false;previousTool=false;stable=0;return false;}
            if(!Adjusting){Adjusting=true;handStart=aim.position;handRadius=RadiusRatio;desktopOffset=0;}
            if(desktop){desktopOffset+=JourneyDesktopControls.WheelSteps*.035f;RadiusRatio=Mathf.Clamp(handRadius+desktopOffset,.5f,1.8f);}
            else RadiusRatio=Mathf.Clamp(handRadius+Vector3.Dot(aim.position-handStart,world.mover.path.Up(SectionArc))*1.2f,.5f,1.8f);
            var current=world.mission.Current;
            if(current?.target=="hdx-wide"&&RadiusRatio>1.20f)world.mission.Accept(StudyAction.Radius,current.target);
            else if(current?.target=="hdx-critical"&&RadiusRatio>1.48f)world.mission.Accept(StudyAction.Radius,current.target);
            else if(current?.target=="hdx-normal"){stable=RadiusRatio>.94f&&RadiusRatio<1.06f?stable+dt:0;Stability=Mathf.Clamp01(stable/.7f);if(Stability>=1)world.mission.Accept(StudyAction.Radius,current.target);}
            return true;
        }
        public float LocalSpeed(float arc)
        {float weight=Active||world.freeTools?1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1.1f,Mathf.Abs(arc-SectionArc))):0;return Mathf.Clamp(Mathf.Lerp(1,FlowRatio,weight)/Mathf.Pow(LocalRadius(arc),2),world.freeTools?.12f:.45f,world.freeTools?3.5f:2.5f);}
        void UpdateGraphics()
        {
            CacheGraphics();
            var path=world.mover.path;float arc=SectionArc;var camera=world.mover.viewCamera.transform;
            Shader.SetGlobalVector("_HemoCentreWS",path.Centre(arc));Shader.SetGlobalVector("_HemoAxisWS",path.Forward(arc));Shader.SetGlobalFloat("_HemoRadius",RadiusRatio);
            bool reveal=Time.time<revealUntil;
            for(int i=0;i<rings.Length;i++)
            {
                float s=arc+(i==0?-1.4f:i==1?1.4f:0);float radius=path.Radius(s)*LocalRadius(s);
                rings[i].enabled=Active; rings[i].positionCount=64;
                for(int n=0;n<64;n++){float angle=n*Mathf.PI*2/64;rings[i].SetPosition(n,path.Offset(s,Mathf.Cos(angle)*(radius-.035f),Mathf.Sin(angle)*(radius-.035f)));}
            }
            string[] labels={reveal?$"A / P₁ {P1:0} мм рт. ст.":"A / давление до",reveal?$"B / P₂ {P2:0} мм рт. ст.\nΔP = {DeltaP:0.0}":"B / давление после",reveal?$"v {VelocityRatio:0.00} × v₀":"v / локальная скорость",reveal?$"Ø {DiameterMm:0.00} мм":"Ø / диаметр",reveal?"Стенка / целая":"Стенка / осмотр"};
            for(int i=0;i<probes.Length;i++){var point=probeOrigins[i];float scale=Mathf.Min(1,LocalRadius(arc+point.z));probes[i].transform.localPosition=new Vector3(point.x*scale,point.y*scale,point.z);}
            for(int i=0;i<readings.Length;i++){var point=readingOrigins[i];float scale=Mathf.Min(1,LocalRadius(arc+point.z));readings[i].transform.localPosition=new Vector3(point.x*scale,point.y*scale,point.z);readings[i].text=labels[i];readings[i].transform.rotation=camera.rotation;}
            sectionCaption.transform.localPosition=captionOrigin*Mathf.Min(1,RadiusRatio);
            sectionCaption.text=Late?"ИЗМЕРЯЕМЫЙ УЧАСТОК":Case==3?"СРАВНИ ДАННЫЕ / ВЫБЕРИ ПРИЧИНУ":"УЧЕБНЫЙ ОПЫТ / Q ФИКСИРОВАН";sectionCaption.transform.rotation=camera.rotation;
            formulaCaption.gameObject.SetActive(FormulaDiscovered&&!Late&&Case<3);formulaCaption.text="R ∝ 1/r⁴\nпри Q = const: v ∝ 1/r²";formulaCaption.transform.rotation=camera.rotation;
            for(int i=0;i<streamlines.Length;i++)
            {
                var line=streamlines[i];line.enabled=reveal;line.positionCount=24;float a=i*Mathf.PI*2/streamlines.Length;
                for(int n=0;n<24;n++){float s=arc-2.8f+n*5.6f/23;float r=(.7f+(i%3)*.4f)*LocalRadius(s);line.SetPosition(n,path.Offset(s,Mathf.Cos(a)*r,Mathf.Sin(a)*r));}
            }
            tracerAge+=Time.deltaTime;
            for(int i=0;i<tracers.Length;i++)
            {
                tracerArc[i]+=Time.deltaTime*1.45f*LocalSpeed(arc+tracerArc[i]);bool show=tracerAge<6&&tracerArc[i]>=-2.7f&&tracerArc[i]<3;
                tracers[i].gameObject.SetActive(show);if(show)tracers[i].position=path.Offset(arc+tracerArc[i],(.45f+i*.13f)*LocalRadius(arc+tracerArc[i]),-.3f+(i%3)*.3f);
            }
        }
    }
}
