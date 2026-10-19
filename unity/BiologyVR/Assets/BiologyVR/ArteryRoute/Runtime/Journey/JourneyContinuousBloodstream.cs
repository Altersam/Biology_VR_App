using System;
using System.Collections.Generic;
using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Endless visual circulation; existing study targets retain their original ownership.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class JourneyContinuousBloodstream:MonoBehaviour
    {
        [Serializable] public sealed class VisualSpecies
        {
            public string id;
            public Mesh mesh;
            public Material[] materials;
            public int weight;
            public float minDiameter,maxDiameter;
        }
        public JourneyBloodFlow flow;
        public VisualSpecies[] palette;
        public float visibleDistance=75;
        public int randomSeed=78131;
        public bool varySeedEachRun=true;
        public int RunSeed{get;private set;}
        public int RecycledCells{get;private set;}
        public int RandomSpeciesChanges{get;private set;}
        public int BranchCellCount{get;private set;}
        public int[] BranchPassages{get;private set;}
        public float SimulatedSeconds{get;private set;}
        public int[] CurrentSpeciesCounts{get;private set;}
        public int BackgroundCount{get;private set;}
        public int ActiveWoundLeakCount{get;private set;}
        public int WoundCandidates{get;private set;}
        public int WoundLeakedTotal{get;private set;}
        public int WoundContinuedTotal{get;private set;}
        public int LastRecycledFrame{get;private set;}
        public int DensityRefills{get;private set;}
        public int NearbyMainCells{get;private set;}
        public int VisibleMovingCells{get;private set;}
        public bool uniformCirculation;
        float LocalHalfSpan=>uniformCirculation?24f:48f;
        float nextUniformReturn;
        const int DensityBins=12;
        readonly int[] density=new int[DensityBins];
        int refillCursor,localBudget;
        float coverageArc;
        System.Random rng;
        bool[] reserved,local;
        float[] arc,progress,speed,spin,phase,laneX,laneY;
        int[] route,forcedBranch,visualType;
        Quaternion[] pose;
        Vector3[] turnStart;
        bool[] leaking;
        float[] leakTime;
        Vector3[] leakStart,leakHole;
        MeshFilter[] filters;
        Renderer[] renderers;
        bool[] leased;
        int[] generations;
        float[] reuseAfter;
        float[] brokenUntil;
        string requestedInflux;
        float nextRequestedInflux;
        bool injectingInflux;
        public int PoolReturns{get;private set;}
        public int PromotionCount{get;private set;}
        readonly Plane[] planes=new Plane[6];
        readonly RaycastHit[] occlusionHits=new RaycastHit[32];
        readonly List<int> active=new List<int>();
        public IEnumerable<Transform> MainCellsBetween(float from,float to)
        {
            if(arc==null)yield break;
            foreach(int i in active)if(route[i]<0&&!leaking[i]&&arc[i]>=from&&arc[i]<to&&renderers[i].enabled&&flow.cells[i].gameObject.activeInHierarchy)yield return flow.cells[i];
        }
        void Awake()
        {
            if(!flow)flow=GetComponent<JourneyBloodFlow>();if(!flow||palette==null||palette.Length==0)return;
            int count=flow.cells.Length;reserved=new bool[count];local=new bool[count];arc=new float[count];progress=new float[count];speed=new float[count];spin=new float[count];phase=new float[count];laneX=new float[count];laneY=new float[count];route=new int[count];forcedBranch=new int[count];visualType=new int[count];pose=new Quaternion[count];turnStart=new Vector3[count];filters=new MeshFilter[count];renderers=new Renderer[count];
            if(!flow.mission.world.bloodEntities)foreach(var pickup in flow.interactiveCells)if(pickup)reserved[pickup.poolIndex]=true;
            leased=new bool[count];generations=new int[count];reuseAfter=new float[count];brokenUntil=new float[count];
            leaking=new bool[count];leakTime=new float[count];leakStart=new Vector3[count];leakHole=new Vector3[count];
            for(int i=0;i<count;i++)if(!reserved[i])active.Add(i);
            BackgroundCount=active.Count;BranchPassages=new int[flow.branchPaths.Length];CurrentSpeciesCounts=new int[palette.Length];RunSeed=varySeedEachRun?Environment.TickCount^GetInstanceID():randomSeed;rng=new System.Random(RunSeed);
            float anchor=flow.mover.path.Anchor(3);int localSlots=active.Count-active.Count/3;int localSerial=0,globalSerial=0;int branches=flow.branchPaths.Length;
            for(int serial=0;serial<active.Count;serial++)
            {
                int i=active[serial];filters[i]=flow.cells[i].GetComponent<MeshFilter>();renderers[i]=flow.cells[i].GetComponent<Renderer>();visualType[i]=-1;route[i]=forcedBranch[i]=-1;local[i]=serial%3!=2;
                // Interleave persistent-route/branch slots with local slots, so
                // mobile's lower visible budget retains every stream category.
                if(local[i]){float distribution=uniformCirculation?Mathf.Repeat(localSerial++*.618033989f+.17f,1):(localSerial++ + Range(0,1))/localSlots;arc[i]=anchor-LocalHalfSpan+distribution*LocalHalfSpan*2;}
                else
                {
                    int global=globalSerial++;
                    if(global%3==0&&branches>0)
                    {
                        int branch=(global/3)%branches;forcedBranch[i]=branch;
                        float start=flow.branchEntries[branch];float p=Range(-26,flow.branchPaths[branch].Length);
                        if(p<0)arc[i]=start+p;
                        else{arc[i]=start;route[i]=branch;progress[i]=p;turnStart[i]=flow.path.Offset(start,0,0);}
                    }
                    else arc[i]=Range(.1f,flow.path.Length-.1f);
                }
                Randomize(i);Place(i,0);
            }
        }
        float Range(float a,float b)=>Mathf.Lerp(a,b,(float)rng.NextDouble());
        void Randomize(int i)
        {
            int total=0;foreach(var p in palette)total+=p.weight;int roll=rng.Next(total),type=0;
            for(;type<palette.Length-1;type++){if(roll<palette[type].weight)break;roll-=palette[type].weight;}
            if(injectingInflux&&!string.IsNullOrEmpty(requestedInflux)){for(int n=0;n<palette.Length;n++)if(palette[n].id==requestedInflux){type=n;requestedInflux=null;break;}}
            if(visualType[i]>=0){CurrentSpeciesCounts[visualType[i]]--;if(visualType[i]!=type)RandomSpeciesChanges++;}
            visualType[i]=type;CurrentSpeciesCounts[type]++;var source=palette[type];filters[i].sharedMesh=source.mesh;renderers[i].sharedMaterials=source.materials;
            flow.species[i]=source.id;float extent=Mathf.Max(source.mesh.bounds.size.x,Mathf.Max(source.mesh.bounds.size.y,source.mesh.bounds.size.z));flow.cells[i].localScale=Vector3.one*(Range(source.minDiameter,source.maxDiameter)/Mathf.Max(.001f,extent));
            float angle=Range(0,Mathf.PI*2),radius=Mathf.Sqrt(Range(.08f,.85f))*3.25f;laneX[i]=Mathf.Cos(angle)*radius;laneY[i]=Mathf.Sin(angle)*radius;
            speed[i]=uniformCirculation?Range(1.40f,1.60f):Range(1.15f,2.05f);spin[i]=Range(-18,18);phase[i]=Range(0,Mathf.PI*2);pose[i]=Quaternion.Euler(Range(25,155),Range(0,360),Range(0,360));
            generations[i]++;
            if(flow.mission.world.freeTools)flow.mission.world.freeTools.payload.UpdateSource(i,source.id);
        }
        void Update()
        {
            if(reserved==null||!flow||!flow.mover||!flow.mover.viewCamera)return;
            float dt=Time.deltaTime;SimulatedSeconds+=dt;BranchCellCount=0;ActiveWoundLeakCount=0;var camera=flow.mover.viewCamera;GeometryUtility.CalculateFrustumPlanes(camera,planes);
            coverageArc=flow.mission.SceneNumber<=16?flow.mover.Distance:flow.path.Anchor(16);
            if(flow.mission.SceneNumber<=16&&(camera.transform.position-flow.path.Centre(coverageArc)).sqrMagnitude>10*10)coverageArc=NearestArc(camera.transform.position);
            var wound=flow.mission.world.woundState;float woundArc=flow.path.Anchor(11)+5;
            int limit=flow.VisibleCellLimit>0?flow.VisibleCellLimit:Application.isMobilePlatform?flow.mobileCellLimit:flow.cells.Length;
            EnsureRequestedInflux(camera,limit);
            CountDensity(limit);
            foreach(int i in active)
            {
                if(i>=limit){renderers[i].enabled=false;continue;}
                if(brokenUntil[i]>0){arc[i]+=dt*speed[i];Place(i,dt);renderers[i].enabled=false;if(flow.mission.world.freeTools&&flow.mission.world.freeTools.payload.backgroundCargo[i])flow.mission.world.freeTools.payload.backgroundCargo[i].gameObject.SetActive(false);if(brokenUntil[i]>Time.time||!Invisible(i,camera,Vector3.Distance(flow.cells[i].position,camera.transform.position)))continue;brokenUntil[i]=0;Randomize(i);}
                if(leaking[i])
                {
                    leakTime[i]+=dt;PlaceLeak(i);
                    float leakDistance=(flow.cells[i].position-camera.transform.position).magnitude;
                    if(leakTime[i]>4.5f&&Invisible(i,camera,leakDistance)&&TryEntry(i,camera,out float entry))
                    {leaking[i]=false;route[i]=-1;progress[i]=0;SetEntry(i,entry);Randomize(i);Place(i,dt);RecycledCells++;LastRecycledFrame=Time.frameCount;}
                    if(leaking[i]&&leakTime[i]<4.5f)ActiveWoundLeakCount++;
                    renderers[i].enabled=!leased[i]&&leakDistance>1.3f&&leakDistance<visibleDistance;continue;
                }
                // Clinical/mission flow measurements do not freeze or thin the
                // persistent visual circulation, including failures and late stages.
                float advance=dt*speed[i];
                if(route[i]<0&&flow.mission.world.hemodynamics)advance*=flow.mission.world.hemodynamics.LocalSpeed(arc[i]);
                if(route[i]<0&&flow.mission.SceneNumber==6&&flow.mission.world.plaqueChallenge&&arc[i]>flow.path.Anchor(6)-8&&arc[i]<flow.path.Anchor(6)+5)
                    advance*=flow.mission.world.freeTools?Mathf.Pow(flow.mission.world.plaqueChallenge.FlowFraction,2):Mathf.Lerp(.82f,1,flow.mission.world.plaqueChallenge.FlowFraction);
                if(route[i]<0)
                {
                    float previous=arc[i];arc[i]+=advance;
                    bool split=wound&&wound.splitMainFlow&&flow.mission.SceneNumber>=11&&flow.mission.SceneNumber<=13;
                    if(split&&previous<woundArc-3&&arc[i]>=woundArc-3)
                    {
                        WoundCandidates++;
                        if(rng.NextDouble()<.25*Mathf.Clamp01(wound.LeakStrength))
                        {leaking[i]=true;leakTime[i]=0;leakStart[i]=flow.cells[i].position;float offset=Range(-.92f,.92f);float s=woundArc+offset;leakHole[i]=flow.path.Offset(s,flow.path.Radius(s),Range(-.025f,.025f));WoundLeakedTotal++;PlaceLeak(i);ActiveWoundLeakCount++;continue;}
                        WoundContinuedTotal++;
                    }
                    if(split&&arc[i]>woundArc&&arc[i]<woundArc+14)arc[i]-=advance*.18f*wound.LeakStrength;
                    for(int b=0;!local[i]&&b<flow.branchEntries.Length;b++)
                        if(previous<flow.branchEntries[b]&&arc[i]>=flow.branchEntries[b]&&(forcedBranch[i]==b||forcedBranch[i]<0&&rng.NextDouble()<.28))
                        {route[i]=b;progress[i]=0;turnStart[i]=flow.cells[i].position;BranchPassages[b]++;break;}
                }
                else{progress[i]+=advance;BranchCellCount++;}
                Place(i,dt);
                float distance=(flow.cells[i].position-camera.transform.position).magnitude;
                bool pastEnd=route[i]>=0?progress[i]>flow.branchPaths[route[i]].Length+3:arc[i]>=flow.path.Length+3;
                bool leftLocal=local[i]&&route[i]<0&&Mathf.Abs(arc[i]-coverageArc)>LocalHalfSpan;
                // Both endpoints must be outside rendering or the camera frustum.
                // A turn only reveals already-moving particles, never a camera reset.
                if(!leased[i]&&(local[i]?leftLocal:pastEnd)&&Time.time>=reuseAfter[i]&&(!uniformCirculation||!local[i]||Time.time>=nextUniformReturn)&&Invisible(i,camera,distance))
                {
                    if(TryEntry(i,camera,out float entry))
                    {route[i]=-1;progress[i]=0;SetEntry(i,entry);Randomize(i);Place(i,dt);RecycledCells++;PoolReturns++;reuseAfter[i]=Time.time+Range(.08f,.65f);LastRecycledFrame=Time.frameCount;}
                }
                distance=(flow.cells[i].position-camera.transform.position).magnitude;
                renderers[i].enabled=!leased[i]&&distance>1.3f&&distance<visibleDistance;
            }
            if(!flow.mission.world.freeTools||flow.mission.SceneNumber!=6||!flow.mission.world.plaqueChallenge||flow.mission.world.plaqueChallenge.Cleared)RefillCoverage(camera,limit);
            NearbyMainCells=VisibleMovingCells=0;
            foreach(int i in active)if(i<limit&&renderers[i].enabled)
            {
                if(route[i]<0&&!leaking[i]&&Mathf.Abs(arc[i]-coverageArc)<35)NearbyMainCells++;
                if(GeometryUtility.TestPlanesAABB(planes,renderers[i].bounds))VisibleMovingCells++;
            }
            flow.RecordContinuousBranchCount(BranchCellCount);
        }
        float NearestArc(Vector3 point)
        {
            float best=float.MaxValue,result=coverageArc;
            for(int n=0;n<flow.path.points.Length-1;n++)
            {
                Vector3 a=flow.path.points[n],delta=flow.path.points[n+1]-a;float t=Mathf.Clamp01(Vector3.Dot(point-a,delta)/Mathf.Max(.0001f,delta.sqrMagnitude));float distance=(a+delta*t-point).sqrMagnitude;
                if(distance<best){best=distance;result=Mathf.Lerp(flow.path.distances[n],flow.path.distances[n+1],t);}
            }
            return result;
        }
        int Bin(float value)=>Mathf.FloorToInt((value-coverageArc+LocalHalfSpan)/(LocalHalfSpan*2/DensityBins));
        void CountDensity(int limit)
        {
            Array.Clear(density,0,density.Length);localBudget=0;
            foreach(int i in active)if(i<limit&&local[i]){localBudget++;int bin=Bin(arc[i]);if(!leaking[i]&&route[i]<0&&bin>=0&&bin<DensityBins)density[bin]++;}
        }
        void SetEntry(int i,float entry)
        {
            if(local[i]){int old=Bin(arc[i]);if(old>=0&&old<DensityBins)density[old]=Mathf.Max(0,density[old]-1);int next=Bin(entry);if(next>=0&&next<DensityBins)density[next]++;}
            arc[i]=entry;
        }
        void RefillCoverage(Camera camera,int limit)
        {
            if(uniformCirculation&&!flow.mover.Moving)return;
            CountDensity(limit);
            int allowance=limit<flow.cells.Length?3:6,moved=0;
            // Only invisible existing slots can refill under-populated windows.
            // Nearby visible cells keep their absolute trajectory through travel.
            for(int examined=0;examined<active.Count&&moved<allowance;examined++)
            {
                int i=active[refillCursor];refillCursor=(refillCursor+1)%active.Count;if(i>=limit||!local[i]||leaking[i]||leased[i]||route[i]>=0||Time.time<reuseAfter[i])continue;
                int old=Bin(arc[i]);if(old>=0&&old<DensityBins&&density[old]<=Mathf.CeilToInt(localBudget/(float)DensityBins)+1)continue;
                float distance=Vector3.Distance(flow.cells[i].position,camera.transform.position);if(!Invisible(i,camera,distance)||!TryLocalEntry(i,camera,out float entry))continue;
                int next=Bin(entry);
                // Never shuttle cells between already full hidden bins: let that
                // upstream supply advance naturally into the visible forward flow.
                if(next<0||next>=DensityBins||next==old||density[next]>=Mathf.CeilToInt(localBudget/(float)DensityBins))continue;
                SetEntry(i,entry);Randomize(i);Place(i,0);DensityRefills++;RecycledCells++;PoolReturns++;reuseAfter[i]=Time.time+Range(.08f,.65f);LastRecycledFrame=Time.frameCount;moved++;
                distance=Vector3.Distance(flow.cells[i].position,camera.transform.position);renderers[i].enabled=distance>1.3f&&distance<visibleDistance;
            }
        }
        Vector3 MainPoint(float value,float x,float y)
        {
            float safe=Mathf.Clamp(value,0,flow.path.Length);var point=flow.path.Offset(safe,x,y);
            if(value<0)point+=flow.path.Forward(0)*value;
            if(value>flow.path.Length)point+=flow.path.Forward(flow.path.Length)*(value-flow.path.Length);
            return point;
        }
        bool TryLocalEntry(int i,Camera camera,out float entry)
        {
            if(uniformCirculation&&!flow.mover.Moving&&arc[i]>=coverageArc)
            {
                entry=coverageArc-LocalHalfSpan+Range(-.6f,.6f);
                if(Time.time<nextUniformReturn||GeometryUtility.TestPlanesAABB(planes,new Bounds(MainPoint(entry,0,0),Vector3.one*7.5f)))return false;
                nextUniformReturn=Time.time+LocalHalfSpan*2/Mathf.Max(1,localBudget*1.5f);return true;
            }
            if(!uniformCirculation&&(flow.mission.SceneNumber==3&&flow.mission.world.bloodEntities||flow.mission.SceneNumber==6&&flow.mission.world.freeTools)&&!flow.mover.Moving)
            {entry=coverageArc-14.5f+Range(-2,0);var inlet=MainPoint(entry,0,0);return !GeometryUtility.TestPlanesAABB(planes,new Bounds(inlet,Vector3.one*7.5f));}
            entry=0;int selected=-1,best=int.MaxValue;
            for(int bin=0;bin<DensityBins;bin++)
            {
                if(density[bin]>=best)continue;
                float candidate=coverageArc-LocalHalfSpan+(bin+.5f)*LocalHalfSpan*2/DensityBins;
                // A full vessel cross-section is hidden, so lane/type randomization
                // cannot expose the incoming endpoint after choosing it.
                var centre=MainPoint(candidate,0,0);if(Vector3.Distance(centre,camera.transform.position)<5)continue;
                if(GeometryUtility.TestPlanesAABB(planes,new Bounds(centre,Vector3.one*7.5f))&&Vector3.Distance(centre,camera.transform.position)<visibleDistance+6)continue;
                selected=bin;best=density[bin];entry=candidate;
            }
            if(selected<0)return false;
            // Keep jitter inside the selected bin, including the narrower uniform-flow bins.
            float jitter=Mathf.Min(2.5f,LocalHalfSpan/DensityBins*.99f);entry+=Range(-jitter,jitter);return true;
        }
        public int Generation(int index)=>generations[index];
        public bool Leased(int index)=>leased!=null&&leased[index];
        public void RequestInflux(string species)=>requestedInflux=species;
        void EnsureRequestedInflux(Camera camera,int limit)
        {
            if(string.IsNullOrEmpty(requestedInflux)||Time.time<nextRequestedInflux)return;
            float entry=coverageArc-6.5f;Vector3 point=MainPoint(entry,.55f,-.35f);
            if(GeometryUtility.TestPlanesAABB(planes,new Bounds(point,Vector3.one*.7f)))return;
            foreach(int i in active)
            {
                if(i>=limit||leased[i]||leaking[i]||!local[i]||Time.time<reuseAfter[i]||!Invisible(i,camera,Vector3.Distance(flow.cells[i].position,camera.transform.position)))continue;
                route[i]=-1;progress[i]=0;arc[i]=entry;injectingInflux=true;Randomize(i);injectingInflux=false;laneX[i]=.55f+Range(-.3f,.3f);laneY[i]=-.35f+Range(-.25f,.25f);reuseAfter[i]=Time.time+8;nextRequestedInflux=Time.time+.75f;Place(i,0);RecycledCells++;PoolReturns++;return;
            }
        }
        public void SetLease(int index,bool value){leased[index]=value;renderers[index].enabled=!value&&brokenUntil[index]<=Time.time;if(value)PromotionCount++;}
        public void BreakCell(int index){brokenUntil[index]=Time.time+2.5f;renderers[index].enabled=false;}
        public bool LeaseLeukocyte(Vector3 point,out int index)
        {
            index=-1;float best=8*8;foreach(int i in active){if(i>=flow.VisibleCellLimit||leased[i]||leaking[i]||!renderers[i].enabled||flow.species[i]!="leukocyte")continue;float distance=(flow.cells[i].position-point).sqrMagnitude;if(distance<best){best=distance;index=i;}}if(index<0){RequestInflux("leukocyte");return false;}SetLease(index,true);return true;
        }
        public void ReturnResponder(int index){ResumeLease(index,flow.cells[index].position);SetLease(index,false);}
        public void ResumeLease(int index,Vector3 position)
        {
            float value=NearestArc(position);route[index]=-1;progress[index]=0;arc[index]=value;var radial=position-flow.path.Centre(value);laneX[index]=Mathf.Clamp(Vector3.Dot(radial,flow.path.Right(value)),-2.8f,2.8f);laneY[index]=Mathf.Clamp(Vector3.Dot(radial,flow.path.Up(value)),-2.8f,2.8f);Place(index,0);
        }
        public Vector3 VelocityAt(Vector3 point)=>flow.path.Forward(NearestArc(point))*1.5f;
        public bool PickNear(Vector3 point,float reach,out int index)
        {
            index=-1;float best=reach*reach;
            foreach(int i in active){if(i>=flow.VisibleCellLimit||leased[i]||leaking[i]||!renderers[i].enabled)continue;float distance=(renderers[i].bounds.ClosestPoint(point)-point).sqrMagnitude;if(distance<best){best=distance;index=i;}}
            return index>=0;
        }
        public bool Pick(Ray ray,float maxDistance,out int index,out float distance)
        {
            index=-1;distance=maxDistance;int limit=flow.VisibleCellLimit;
            foreach(int i in active)
            {
                if(i>=limit||leased[i]||leaking[i]||!renderers[i].enabled)continue;
                var delta=flow.cells[i].position-ray.origin;float projection=Vector3.Dot(delta,ray.direction);if(projection<.15f||projection>distance)continue;
                float radius=Mathf.Max(.045f,renderers[i].bounds.extents.magnitude*.72f);float across=delta.sqrMagnitude-projection*projection;
                if(across>radius*radius)continue;float hit=projection-Mathf.Sqrt(Mathf.Max(0,radius*radius-across));
                if(hit<distance){index=i;distance=hit;}
            }
            return index>=0;
        }
        bool Invisible(int i,Camera camera,float distance)
        {
            if(distance>visibleDistance+2||!GeometryUtility.TestPlanesAABB(planes,new Bounds(flow.cells[i].position,Vector3.one)))return true;
            if(!uniformCirculation||!local[i]||Mathf.Abs(arc[i]-coverageArc)<LocalHalfSpan)return false;
            var point=flow.cells[i].position;var right=camera.transform.right*.28f;
            return BehindOpaqueWall(camera.transform.position,point)&&BehindOpaqueWall(camera.transform.position,point+right)&&BehindOpaqueWall(camera.transform.position,point-right);
        }
        bool BehindOpaqueWall(Vector3 from,Vector3 point)
        {
            var delta=point-from;int count=Physics.RaycastNonAlloc(new Ray(from,delta.normalized),occlusionHits,delta.magnitude-.45f,~0,QueryTriggerInteraction.Ignore);
            if(count==occlusionHits.Length)return false;
            for(int i=0;i<count;i++){var c=occlusionHits[i].collider;if(c&&!c.transform.IsChildOf(flow.mover.origin.transform)&&!c.GetComponentInParent<JourneyTarget>()&&!c.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>())return true;}return false;
        }
        void PlaceLeak(int i)
        {
            float woundArc=flow.path.Anchor(11)+5;var outward=flow.path.Right(woundArc);var forward=flow.path.Forward(woundArc);float t=leakTime[i]/3;
            Vector3 point;
            if(t<1)
            {
                float u=1-t;var a=leakStart[i];var b=leakHole[i]-outward*1.3f-forward*1.5f;var c=leakHole[i]-outward*.50f;var d=leakHole[i];
                point=u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;
            }
            else point=leakHole[i]+outward*(leakTime[i]-3)*1.45f+forward*Mathf.Min(.35f,(leakTime[i]-3)*.1f);
            flow.cells[i].SetPositionAndRotation(point,Quaternion.LookRotation(outward,flow.path.Up(woundArc))*pose[i]*Quaternion.AngleAxis(SimulatedSeconds*spin[i],Vector3.up));
        }
        bool TryEntry(int i,Camera camera,out float entry)
        {
            entry=0;
            if(local[i])return TryLocalEntry(i,camera,out entry);
            for(int attempt=0;attempt<6;attempt++)
            {
                entry=forcedBranch[i]>=0?flow.branchEntries[forcedBranch[i]]-30-attempt*24:Range(.1f,Mathf.Max(.2f,flow.path.Length-100));
                entry=Mathf.Clamp(entry,.1f,flow.path.Length-.1f);var center=flow.path.Centre(entry);
                if((center-camera.transform.position).sqrMagnitude>(visibleDistance+6)*(visibleDistance+6)||!GeometryUtility.TestPlanesAABB(planes,new Bounds(center,Vector3.one*8)))return true;
            }
            return false;
        }
        void Place(int i,float dt)
        {
            float s=arc[i];Vector3 point;Quaternion frame;
            if(route[i]<0)
            {
                float safe=Mathf.Clamp(s,0,flow.path.Length);float x=laneX[i];
                if(flow.mission.world.woundState&&flow.mission.world.woundState.splitMainFlow&&flow.mission.SceneNumber>=11&&flow.mission.SceneNumber<=13)
                {float d=s-flow.path.Anchor(11)-5;x+=.20f*flow.mission.world.woundState.LeakStrength*Mathf.Exp(-d*d/7);}
                if(flow.mission&&flow.mission.SceneNumber==6&&flow.mission.world.plaqueState){float d=s-flow.path.Anchor(6)-4;x-=Mathf.Max(0,x-1.4f)*.6f*Mathf.Exp(-d*d/7)*flow.mission.world.plaqueState.PathologyFraction;}
                if(flow.mission&&flow.mission.SceneNumber==13&&flow.mission.ClotBalance>.86f){float d=s-flow.path.Anchor(11)-5;x-=Mathf.Max(0,x+.2f)*Mathf.Exp(-d*d/6)*.72f;}
                float sway=.065f*Mathf.Sin(SimulatedSeconds*.55f+phase[i]);float radius=flow.mission.world.hemodynamics?flow.mission.world.hemodynamics.LocalRadius(s):1;point=MainPoint(s,(x+sway)*radius,(laneY[i]+sway*.7f)*radius);frame=flow.path.Frame(safe);
            }
            else
            {
                var branch=flow.branchPaths[route[i]];float p=progress[i],safe=Mathf.Min(p,branch.Length);float radius=Mathf.Sqrt(laneX[i]*laneX[i]+laneY[i]*laneY[i]);float laneScale=Mathf.Min(1,1.4f/Mathf.Max(.01f,radius));
                var next=branch.Offset(safe,laneX[i]*laneScale,laneY[i]*laneScale);
                if(p>branch.Length)next+=branch.Forward(branch.Length)*(p-branch.Length);
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(p/4));point=Vector3.Lerp(turnStart[i]+flow.path.Forward(flow.branchEntries[route[i]])*p*.15f,next,t);frame=Quaternion.Slerp(flow.path.Frame(flow.branchEntries[route[i]]),branch.Frame(safe),t);
            }
            flow.cells[i].SetPositionAndRotation(point,frame*pose[i]*Quaternion.AngleAxis(SimulatedSeconds*spin[i],Vector3.up));
        }
    }
}
