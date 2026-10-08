// Recovery copy reconstructed from the original read and this session's patch.
using UnityEngine;
namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyBloodFlow : MonoBehaviour
    {
        public ArteryJourneyPath path;
        public JourneyMover mover;
        public JourneyMission mission;
        public Transform[] cells;
        public float[] offsets,lanesX,lanesY;
        public int mobileCellLimit=48;
        public bool composedFlow;
        public float flowSpan=160,flowBehind=74;
        public ArteryJourneyPath[] branchPaths;
        public float[] branchEntries;
        public string[] species;
        public JourneyFlowPickup[] interactiveCells;
        readonly System.Collections.Generic.Dictionary<int,JourneyFlowPickup> pickups=new System.Collections.Generic.Dictionary<int,JourneyFlowPickup>();
        public UnityEngine.UI.Text teachingLegend;
        int[] routes;float[] branchProgress,lastArc;Vector3[] turnStarts;float previousPlayerDistance;
        public int BranchRoutedCells{get;private set;}
        float[] phases,speeds;
        float[] preferredSpeed,wanderPhase,wanderRate,spinSpeed;
        Vector3[] baseScales;int[] generations;
        public int randomSeed=73519;
        public bool randomizedFlow=true;
        System.Random random;
        float[] absoluteArc;
        Quaternion[] poses;Renderer[][] visuals;int visibleCellLimit;
        void Awake()
        {
            BuildPickupMap();
            int count=cells==null?0:cells.Length;poses=new Quaternion[count];visuals=new Renderer[count][];
            for(int i=0;i<count;i++)
            {if(!cells[i]){visuals[i]=System.Array.Empty<Renderer>();continue;}poses[i]=cells[i].localRotation;visuals[i]=cells[i].GetComponentsInChildren<Renderer>();}
            visibleCellLimit=Application.isMobilePlatform?Mathf.Min(mobileCellLimit,count):count;
            InitializePhases();
            InitializeRoutes();
            InitializeWorldPositions();
        }
        void OnEnable(){BuildPickupMap();}
        void BuildPickupMap()
        {pickups.Clear();if(interactiveCells==null)return;foreach(var pickup in interactiveCells)if(pickup)pickups[pickup.poolIndex]=pickup;}
        public void SetVisibleCellLimit(int limit)
        {
            int requested=Mathf.Clamp(limit,1,cells==null?1:cells.Length);
            // The mobile profile repeats the serialized default at startup. Do not
            // randomize the already-initialized study lanes a second time.
            if(requested==visibleCellLimit&&phases!=null)return;
            visibleCellLimit=requested;
            InitializePhases();
            foreach(var pair in pickups){lanesX[pair.Key]=pair.Value.startLane.x;lanesY[pair.Key]=pair.Value.startLane.y;}
            for(int i=visibleCellLimit;i<(cells==null?0:cells.Length);i++)SetVisible(i,false);
        }
        void SetVisible(int index,bool show)
        {if(visuals==null||index<0||index>=visuals.Length||visuals[index]==null)return;foreach(var r in visuals[index])if(r)r.enabled=show;}
        void InitializePhases()
        {
            int count=cells==null?0:cells.Length;random??=new System.Random(randomSeed);phases=new float[count];speeds=new float[count];
            preferredSpeed=new float[count];wanderPhase=new float[count];wanderRate=new float[count];spinSpeed=new float[count];generations=new int[count];baseScales=new Vector3[count];random=new System.Random(randomSeed);
            for(int i=0;i<count;i++)
            {
                baseScales[i]=cells[i]?cells[i].localScale:Vector3.one;
                phases[i]=(i+Range(-.42f,.42f))*flowSpan/Mathf.Max(1,visibleCellLimit);RandomizeCell(i,false);speeds[i]=preferredSpeed[i];
            }
        }
        float Range(float low,float high)=>Mathf.Lerp(low,high,(float)random.NextDouble());
        void RandomizeCell(int i,bool scale)
        {
            if(!randomizedFlow)return;
            if(random==null)random=new System.Random(randomSeed+i*7919);
            if(preferredSpeed==null||preferredSpeed.Length!=cells.Length)InitializePhases();
            preferredSpeed[i]=Range(1.12f,2.12f);wanderPhase[i]=Range(0,Mathf.PI*2);wanderRate[i]=Range(.25f,.7f);spinSpeed[i]=Range(-13f,13f);
            if(i<lanesX.Length){float angle=Range(0,Mathf.PI*2),radius=Mathf.Sqrt(Range(.22f,.90f))*3.15f;lanesX[i]=Mathf.Cos(angle)*radius;lanesY[i]=Mathf.Sin(angle)*radius;}
            if(cells[i]){poses[i]=Quaternion.Euler(Range(25,145),Range(0,360),Range(0,360));if(scale)cells[i].localScale=baseScales[i]*Range(.82f,1.17f);}
        }
        void InitializeRoutes()
        {int count=cells==null?0:cells.Length;routes=new int[count];branchProgress=new float[count];lastArc=new float[count];turnStarts=new Vector3[count];for(int i=0;i<count;i++)routes[i]=-1;previousPlayerDistance=mover?mover.Distance:0;}
        void InitializeWorldPositions()
        {
            absoluteArc=new float[cells.Length];float anchor=mover?mover.path.Anchor(3):0;
            for(int i=0;i<absoluteArc.Length;i++)absoluteArc[i]=Mathf.Clamp(anchor-flowBehind+Mathf.Repeat(phases[i],flowSpan),.1f,path.Length-.1f);
            foreach(var pair in pickups)
            {
                int index=pair.Key;if(index<0||index>=absoluteArc.Length)continue;
                absoluteArc[index]=anchor+pair.Value.startDistance;lanesX[index]=pair.Value.startLane.x;lanesY[index]=pair.Value.startLane.y;
                speeds[index]=.18f;
            }
        }
        public void ResumePickupAt(int index,Vector3 point)
        {
            if(absoluteArc==null||index<0||index>=absoluteArc.Length)return;
            float best=float.MaxValue,arc=absoluteArc[index];
            for(int i=0;i<path.points.Length-1;i++)
            {
                var start=path.points[i];var delta=path.points[i+1]-start;float t=Mathf.Clamp01(Vector3.Dot(point-start,delta)/Mathf.Max(.0001f,delta.sqrMagnitude));
                var near=start+delta*t;float distance=(near-point).sqrMagnitude;
                if(distance<best){best=distance;arc=Mathf.Lerp(path.distances[i],path.distances[i+1],t);}
            }
            absoluteArc[index]=lastArc[index]=arc;routes[index]=-1;
            var radial=point-path.Centre(arc);lanesX[index]=Vector3.Dot(radial,path.Right(arc));lanesY[index]=Vector3.Dot(radial,path.Up(arc));
            cells[index].position=point;
        }
        void Update()
        {
            if(!path||!mover||!mover.viewCamera||cells==null||offsets==null||lanesX==null||lanesY==null)return;
            if(teachingLegend)teachingLegend.gameObject.SetActive(mission&&mission.SceneNumber>=3&&mission.SceneNumber<=16);
            if(phases==null||phases.Length!=cells.Length)InitializePhases();
            if(routes==null||routes.Length!=cells.Length)InitializeRoutes();
            if(absoluteArc==null||absoluteArc.Length!=cells.Length)InitializeWorldPositions();
            previousPlayerDistance=mover.Distance;BranchRoutedCells=0;
            if(mission&&(mission.SceneNumber>=18||(mission.SceneNumber>=17&&mission.world&&mission.world.labRoom&&mission.world.labRoom.activeInHierarchy))){for(int i=0;i<cells.Length;i++)SetVisible(i,false);return;}
            for(int i=0;i<cells.Length;i++)
            {
                if(!cells[i]||i>=offsets.Length||i>=lanesX.Length||i>=lanesY.Length)continue;
                if(i>=visibleCellLimit){SetVisible(i,false);continue;}
                pickups.TryGetValue(i,out var pickup);bool studying=pickup&&pickup.InStudyFlow;
                if(studying&&pickup.Held){SetVisible(i,false);continue;}
                float radial=Mathf.Sqrt(lanesX[i]*lanesX[i]+lanesY[i]*lanesY[i]);
                float rate=(randomizedFlow?preferredSpeed[i]:1.25f+(i%7)*.08f)*(mission?mission.FlowMultiplier:1);
                if(studying)rate=pickup.Rejoining?.45f:.18f;
                if(composedFlow)rate*=Mathf.Lerp(1.4f,.70f,Mathf.Clamp01(radial/3.5f));
                speeds[i]=Mathf.MoveTowards(speeds[i],rate,Time.deltaTime*2f);float oldPhase=phases[i];phases[i]+=Time.deltaTime*speeds[i];
                if(Mathf.FloorToInt(oldPhase/flowSpan)!=Mathf.FloorToInt(phases[i]/flowSpan)&&routes[i]<0){generations[i]++;RandomizeCell(i,true);}
                absoluteArc[i]+=Time.deltaTime*speeds[i];
                float s=absoluteArc[i];
                if(studying&&pickup.TryHiddenReentry(s,mover.viewCamera,out float incomingArc))
                {s=absoluteArc[i]=lastArc[i]=incomingArc;lanesX[i]=pickup.startLane.x;lanesY[i]=pickup.startLane.y;routes[i]=-1;}
                // Recycle only invisible, distant instances. Nearby cells retain
                // absolute world arc coordinates even when the player travels.
                bool distant=cells[i]&&(cells[i].position-mover.viewCamera.transform.position).sqrMagnitude>(studying?18*18:75*75);
                if(!studying&&routes[i]<0&&distant&&(s<mover.Distance-flowBehind||s>mover.Distance+flowSpan-flowBehind||s>=path.Length-.5f))
                {s=Mathf.Clamp(mover.Distance-flowBehind+Mathf.Repeat(phases[i],flowSpan),.1f,path.Length-.1f);absoluteArc[i]=s;lastArc[i]=s;}
                s=Mathf.Clamp(s,.1f,path.Length-.1f);
                if(routes[i]<0&&branchPaths!=null&&branchEntries!=null&&(i*37+generations[i]*17)%100<24&&lastArc[i]>0&&s>=lastArc[i])
                {
                    if(!studying)
                    for(int branch=0;branch<branchPaths.Length;branch++)if(lastArc[i]<branchEntries[branch]&&s>=branchEntries[branch])
                    {routes[i]=branch;branchProgress[i]=0;turnStarts[i]=path.Offset(branchEntries[branch],lanesX[i],lanesY[i]);break;}
                }
                if(routes[i]>=0)
                {
                    int branch=routes[i];var route=branchPaths[branch];branchProgress[i]+=Time.deltaTime*speeds[i];float progress=branchProgress[i];
                    if(progress>route.Length-.5f||(route.Centre(Mathf.Min(progress,route.Length))-mover.viewCamera.transform.position).sqrMagnitude>90*90)
                    {routes[i]=-1;phases[i]=.25f;s=Mathf.Max(.1f,mover.Distance-flowBehind+.25f);absoluteArc[i]=s;}
                    else
                    {
                        float laneScale=Mathf.Min(1,1.6f/Mathf.Max(.01f,radial));var point=route.Offset(progress,lanesX[i]*laneScale,lanesY[i]*laneScale);
                        cells[i].position=Vector3.Lerp(turnStarts[i]+path.Forward(branchEntries[branch])*progress*.15f,point,Mathf.SmoothStep(0,1,Mathf.Clamp01(progress/4)));
                        cells[i].rotation=Quaternion.Slerp(path.Frame(branchEntries[branch]),route.Frame(progress),Mathf.Clamp01(progress/4))*poses[i];BranchRoutedCells++;
                    }
                }
                if(routes[i]<0)
                {
                    float x=lanesX[i];if(mission&&mission.SceneNumber==6&&mission.world.plaqueState){float d=s-path.Anchor(6)-4;x-=Mathf.Max(0,x-1.4f)*.6f*Mathf.Exp(-d*d/7)*mission.world.plaqueState.PathologyFraction;}
                    if(mission&&mission.SceneNumber==13&&mission.ClotBalance>.86f)
                    {float d=s-path.Anchor(11)-5;float obstruction=Mathf.Exp(-d*d/6);x-=Mathf.Max(0,x+.2f)*obstruction*.72f;}
                    float sway=randomizedFlow?.09f*Mathf.Sin(Time.time*wanderRate[i]+wanderPhase[i]):0;
                    cells[i].position=path.Offset(s,x+sway,lanesY[i]+sway*.75f);cells[i].rotation=path.Frame(s)*poses[i]*Quaternion.AngleAxis(Time.time*(randomizedFlow?spinSpeed[i]:7),Vector3.up);
                }
                lastArc[i]=s;
                // Visibility never depends on camera heading. Wrapping happens outside
                // the far clip in both directions; a turn exposes already-moving cells.
                float distance=(cells[i].position-mover.viewCamera.transform.position).sqrMagnitude;
                bool show=distance>1.4f&&distance<75f*75f;
                SetVisible(i,show&&!studying);
                if(studying)pickup.FollowPool(cells[i],Time.deltaTime);
            }
        }
    }
}
