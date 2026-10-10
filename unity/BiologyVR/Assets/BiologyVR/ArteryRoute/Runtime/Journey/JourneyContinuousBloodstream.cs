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
        public int LastRecycledFrame{get;private set;}
        System.Random rng;
        bool[] reserved,local;
        float[] arc,progress,speed,spin,phase,laneX,laneY;
        int[] route,forcedBranch,visualType;
        Quaternion[] pose;
        Vector3[] turnStart;
        MeshFilter[] filters;
        Renderer[] renderers;
        readonly Plane[] planes=new Plane[6];
        readonly List<int> active=new List<int>();
        void Awake()
        {
            if(!flow)flow=GetComponent<JourneyBloodFlow>();if(!flow||palette==null||palette.Length==0)return;
            int count=flow.cells.Length;reserved=new bool[count];local=new bool[count];arc=new float[count];progress=new float[count];speed=new float[count];spin=new float[count];phase=new float[count];laneX=new float[count];laneY=new float[count];route=new int[count];forcedBranch=new int[count];visualType=new int[count];pose=new Quaternion[count];turnStart=new Vector3[count];filters=new MeshFilter[count];renderers=new Renderer[count];
            foreach(var pickup in flow.interactiveCells)if(pickup)reserved[pickup.poolIndex]=true;
            for(int i=0;i<count;i++)if(!reserved[i])active.Add(i);
            BackgroundCount=active.Count;BranchPassages=new int[flow.branchPaths.Length];CurrentSpeciesCounts=new int[palette.Length];RunSeed=varySeedEachRun?Environment.TickCount^GetInstanceID():randomSeed;rng=new System.Random(RunSeed);
            float anchor=flow.mover.path.Anchor(3);int localSlots=active.Count-active.Count/3;int localSerial=0,globalSerial=0;int branches=flow.branchPaths.Length;
            for(int serial=0;serial<active.Count;serial++)
            {
                int i=active[serial];filters[i]=flow.cells[i].GetComponent<MeshFilter>();renderers[i]=flow.cells[i].GetComponent<Renderer>();visualType[i]=-1;route[i]=forcedBranch[i]=-1;local[i]=serial%3!=2;
                // Interleave persistent-route/branch slots with local slots, so
                // mobile's lower visible budget retains every stream category.
                if(local[i])arc[i]=Mathf.Clamp(anchor-85+(localSerial++ + Range(0,1))*155/localSlots,.1f,flow.path.Length-.1f);
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
            if(visualType[i]>=0){CurrentSpeciesCounts[visualType[i]]--;if(visualType[i]!=type)RandomSpeciesChanges++;}
            visualType[i]=type;CurrentSpeciesCounts[type]++;var source=palette[type];filters[i].sharedMesh=source.mesh;renderers[i].sharedMaterials=source.materials;
            flow.species[i]=source.id;float extent=Mathf.Max(source.mesh.bounds.size.x,Mathf.Max(source.mesh.bounds.size.y,source.mesh.bounds.size.z));flow.cells[i].localScale=Vector3.one*(Range(source.minDiameter,source.maxDiameter)/Mathf.Max(.001f,extent));
            float angle=Range(0,Mathf.PI*2),radius=Mathf.Sqrt(Range(.08f,.85f))*3.25f;laneX[i]=Mathf.Cos(angle)*radius;laneY[i]=Mathf.Sin(angle)*radius;
            speed[i]=Range(1.15f,2.05f);spin[i]=Range(-18,18);phase[i]=Range(0,Mathf.PI*2);pose[i]=Quaternion.Euler(Range(25,155),Range(0,360),Range(0,360));
        }
        void Update()
        {
            if(reserved==null||!flow||!flow.mover||!flow.mover.viewCamera)return;
            if(flow.mission.SceneNumber>=17){foreach(int i in active)renderers[i].enabled=false;return;}
            float dt=Time.deltaTime;SimulatedSeconds+=dt;BranchCellCount=0;var camera=flow.mover.viewCamera;GeometryUtility.CalculateFrustumPlanes(camera,planes);
            int limit=Application.isMobilePlatform?flow.mobileCellLimit:flow.cells.Length;
            foreach(int i in active)
            {
                if(i>=limit){renderers[i].enabled=false;continue;}
                float advance=dt*speed[i]*flow.mission.FlowMultiplier;
                if(route[i]<0&&flow.mission.SceneNumber==6&&flow.mission.world.plaqueChallenge&&Mathf.Abs(arc[i]-flow.path.Anchor(6)-5)<14)
                    advance*=Mathf.Lerp(.10f,1,flow.mission.world.plaqueChallenge.FlowFraction);
                if(route[i]<0)
                {
                    float previous=arc[i];arc[i]+=advance;
                    for(int b=0;b<flow.branchEntries.Length;b++)
                        if(previous<flow.branchEntries[b]&&arc[i]>=flow.branchEntries[b]&&(forcedBranch[i]==b||forcedBranch[i]<0&&rng.NextDouble()<.28))
                        {route[i]=b;progress[i]=0;turnStart[i]=flow.cells[i].position;BranchPassages[b]++;break;}
                }
                else{progress[i]+=advance;BranchCellCount++;}
                Place(i,dt);
                float distance=(flow.cells[i].position-camera.transform.position).magnitude;
                bool pastEnd=route[i]>=0?progress[i]>flow.branchPaths[route[i]].Length+3:arc[i]>=flow.path.Length+3;
                bool leftLocal=local[i]&&route[i]<0&&Mathf.Abs(arc[i]-flow.mover.Distance)>95;
                // Both endpoints must be outside rendering or the camera frustum.
                // A turn only reveals already-moving particles, never a camera reset.
                if((pastEnd||leftLocal)&&Invisible(i,camera,distance))
                {
                    if(TryEntry(i,camera,out float entry))
                    {route[i]=-1;progress[i]=0;arc[i]=entry;Randomize(i);Place(i,dt);RecycledCells++;LastRecycledFrame=Time.frameCount;}
                }
                distance=(flow.cells[i].position-camera.transform.position).magnitude;
                renderers[i].enabled=distance>1.3f&&distance<visibleDistance;
            }
            flow.RecordContinuousBranchCount(BranchCellCount);
        }
        bool Invisible(int i,Camera camera,float distance)=>distance>visibleDistance+2||!GeometryUtility.TestPlanesAABB(planes,new Bounds(flow.cells[i].position,Vector3.one));
        bool TryEntry(int i,Camera camera,out float entry)
        {
            entry=0;
            for(int attempt=0;attempt<6;attempt++)
            {
                entry=forcedBranch[i]>=0?flow.branchEntries[forcedBranch[i]]-30-attempt*24:local[i]?flow.mover.Distance-85-attempt*20:Range(.1f,Mathf.Max(.2f,flow.path.Length-100));
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
                if(flow.mission&&flow.mission.SceneNumber==6&&flow.mission.world.plaqueState){float d=s-flow.path.Anchor(6)-4;x-=Mathf.Max(0,x-1.4f)*.6f*Mathf.Exp(-d*d/7)*flow.mission.world.plaqueState.PathologyFraction;}
                if(flow.mission&&flow.mission.SceneNumber==13&&flow.mission.ClotBalance>.86f){float d=s-flow.path.Anchor(11)-5;x-=Mathf.Max(0,x+.2f)*Mathf.Exp(-d*d/6)*.72f;}
                float sway=.065f*Mathf.Sin(SimulatedSeconds*.55f+phase[i]);point=flow.path.Offset(safe,x+sway,laneY[i]+sway*.7f);frame=flow.path.Frame(safe);
                if(s>flow.path.Length)point+=flow.path.Forward(safe)*(s-flow.path.Length);
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
