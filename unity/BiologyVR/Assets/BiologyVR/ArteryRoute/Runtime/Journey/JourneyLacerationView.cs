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
        public Text plateletHint;
        public LineRenderer interactionRim;
        public Material rimIdle,rimReady,rimSuccess;
        Transform suggestedPlatelet;
        float nextSuggestion;
        readonly Plane[] viewPlanes=new Plane[6];
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
            UpdateInteraction(near);
        }
        void UpdateInteraction(bool near)
        {
            var m=world.mission;var launch=world.plateletLaunch;var held=world.freeTools?world.freeTools.Held:null;var platelet=held?held.GetComponent<JourneyPlateletEntity>():null;var input=m.GetComponent<JourneyInput>();var mode=m.GetComponent<BioToolModeController>().CurrentMode;
            bool aimed=launch&&platelet&&platelet.ScannerActivated&&mode==BioToolMode.Reverse&&launch.TryAimWound(input.bioTool,out _);
            bool recent=launch&&Time.time-launch.LastAdhesionTime<.9f;
            if(interactionRim)
            {
                interactionRim.enabled=near&&m.SceneNumber<=13&&!m.Complete;
                interactionRim.sharedMaterial=recent?rimSuccess:aimed?rimReady:rimIdle;interactionRim.positionCount=48;interactionRim.loop=true;interactionRim.widthMultiplier=recent?.018f:aimed?.014f:.009f;
                for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;interactionRim.SetPosition(i,world.woundSite.position-outward*.10f+forward*(Mathf.Cos(a)*1.29f)+up*(Mathf.Sin(a)*.25f));}
            }
            if(label)
            {
                label.gameObject.SetActive(near);
                label.text=m.SceneNumber==11?$"РАЗРЫВ · ПРОВЕРКА {Mathf.Min(3,m.StepIndex+1)} / 3\nScanner внутрь контура":m.SceneNumber==12?m.Complete?"ПРОБКА СТАБИЛЬНА\nУТЕЧКА ОСТАНОВЛЕНА":recent?"ТРОМБОЦИТ ПРИКРЕПИЛСЯ":launch&&launch.Flying?"ТРОМБОЦИТ ЛЕТИТ":aimed?(world.freeTools.ReverseCharge>.8f?"ОТПУСТИ ПРАВЫЙ КУРОК":"ЦЕЛЬ ВЫБРАНА\nУДЕРЖИ ПРАВЫЙ КУРОК"):"ТРОМБОЦИТЫ → В КОНТУР":m.SceneNumber==13?"ПРИТЯЖЕНИЕ\nПЕРЕНЕСИ И ОТПУСТИ":world.RepairGrowth>.99f?"ЭНДОТЕЛИЙ ВОССТАНОВЛЕН":"ВОССТАНОВЛЕНИЕ СТЕНКИ";
                label.transform.SetPositionAndRotation(world.woundSite.position+up*.58f-outward*.22f,world.mover.viewCamera.transform.rotation);
            }
            if(!plateletHint)return;
            bool seeking=near&&launch&&m.SceneNumber==12&&!m.Complete&&m.Current?.target=="platelet"&&!launch.Flying;
            plateletHint.gameObject.SetActive(seeking);if(!seeking)return;
            if(held)
            {
                plateletHint.text=!platelet?"ДРУГОЙ ОБРАЗЕЦ\nОтпусти в притяжении":!platelet.ScannerActivated?"SCANNER → КЛЕТКА":mode!=BioToolMode.Reverse?"A / B → РЕВЕРС":"АКТИВИРОВАН · К РАНЕ";
                plateletHint.transform.SetPositionAndRotation(held.GetComponent<Collider>().bounds.center+world.mover.viewCamera.transform.up*.20f,world.mover.viewCamera.transform.rotation);return;
            }
            if(Time.time>=nextSuggestion)
            {
                nextSuggestion=Time.time+.35f;suggestedPlatelet=null;float nearest=7;var stream=world.bloodEntities.stream;GeometryUtility.CalculateFrustumPlanes(world.mover.viewCamera,viewPlanes);
                for(int i=0;i<stream.flow.VisibleCellLimit;i++)
                {
                    var cell=stream.flow.cells[i];var renderer=cell.GetComponent<Renderer>();if(stream.Leased(i)||stream.flow.species[i]!="platelet"||!renderer.enabled||!GeometryUtility.TestPlanesAABB(viewPlanes,renderer.bounds))continue;
                    float distance=Vector3.Distance(cell.position,world.mover.viewCamera.transform.position);if(distance<1.3f||distance>=nearest)continue;
                    if(Physics.Linecast(world.mover.viewCamera.transform.position,cell.position,~0,QueryTriggerInteraction.Ignore))continue;
                    nearest=distance;suggestedPlatelet=cell;
                }
                if(!suggestedPlatelet)stream.RequestInflux("platelet");
            }
            plateletHint.gameObject.SetActive(suggestedPlatelet);
            if(suggestedPlatelet){plateletHint.text="1 · ТРОМБОЦИТ\nПРИТЯЖЕНИЕ + правый курок";plateletHint.transform.SetPositionAndRotation(suggestedPlatelet.position+world.mover.viewCamera.transform.up*.22f,world.mover.viewCamera.transform.rotation);}
        }
    }
}
