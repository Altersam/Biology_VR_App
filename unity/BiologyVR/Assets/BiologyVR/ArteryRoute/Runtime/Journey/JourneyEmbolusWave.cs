using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyEmbolusWave:MonoBehaviour
    {
        public const int Count=6;
        public JourneyWorld world;
        public JourneyTarget[] targets;
        public Collider[] modeContacts;
        public Renderer[] modeVisuals;
        public int StabilizedCount{get;private set;}
        public int ActiveIndex{get;private set;}
        public JourneyTarget ActiveTarget=>targets!=null&&ActiveIndex>=0&&ActiveIndex<targets.Length?targets[ActiveIndex]:null;
        public bool Complete=>StabilizedCount==targets.Length;
        public bool[] Stabilized{get;private set;}
        float[] arc,phase;
        Vector2[] lane;
        Vector3[] drift;
        float[] driftClock;
        int scene=-1;
        bool initialized;
        void Awake()
        {
            Stabilized=new bool[targets.Length];arc=new float[targets.Length];phase=new float[targets.Length];lane=new Vector2[targets.Length];drift=new Vector3[targets.Length];driftClock=new float[targets.Length];initialized=true;ResetWave();
        }
        public void ResetWave()
        {
            if(!initialized)return;StabilizedCount=0;ActiveIndex=0;
            for(int i=0;i<targets.Length;i++)
            {Stabilized[i]=false;phase[i]=i*1.71f;float angle=i*2.399963f;lane[i]=new Vector2(Mathf.Cos(angle)*(.35f+i*.13f),Mathf.Sin(angle)*(.35f+i*.13f));ResetBubble(i);}
            world.bubble=targets[0].transform;
        }
        void ResetBubble(int index)
        {
            arc[index]=world.mover.path.Anchor(7)+3.1f+index*.50f;
            targets[index].transform.position=world.mover.path.Offset(arc[index],lane[index].x,lane[index].y+.30f);targets[index].gameObject.SetActive(world.mission.SceneNumber==7);
            foreach(var contact in targets[index].GetComponents<Collider>())contact.enabled=true;
        }
        public bool SelectForCapture(JourneyTarget target)
        {
            if(!initialized||!target)return false;
            for(int i=0;i<targets.Length;i++)if(targets[i]==target&&!Stabilized[i]){ActiveIndex=i;world.bubble=target.transform;return true;}
            return false;
        }
        public void DepositActive()
        {
            if(!initialized||Stabilized[ActiveIndex])return;
            Stabilized[ActiveIndex]=true;StabilizedCount++;targets[ActiveIndex].gameObject.SetActive(false);
            for(int i=0;i<targets.Length;i++)if(!Stabilized[i]){ActiveIndex=i;world.bubble=targets[i].transform;return;}
        }
        void Update()
        {
            if(!initialized||world.mission.Goals==null)return;bool active=world.mission.SceneNumber==7;
            foreach(var contact in modeContacts)if(contact)contact.enabled=!world.freeTools&&active&&world.mission.StepIndex==1;
            foreach(var visual in modeVisuals)if(visual)visual.enabled=!world.freeTools&&active&&world.mission.StepIndex==1;
            if(active&&scene!=7&&!world.IsEpisodeCompleted(7))ResetWave();scene=world.mission.SceneNumber;
            for(int i=0;i<targets.Length;i++)
            {
                targets[i].gameObject.SetActive(active&&!Stabilized[i]);if(!active||Stabilized[i]||world.mover.Moving)continue;
                if(world.freeTools){var grab=targets[i].GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();if(grab)grab.enabled=false;}
                if(world.EmbolusCaptured&&i==ActiveIndex)continue;
                float dt=Time.deltaTime;arc[i]+=dt*(.24f+i*.025f);phase[i]+=dt;
                if(world.freeTools)Drift(i,dt);
                else{var forward=world.mover.path.Forward(arc[i]);targets[i].transform.position+=forward*(dt*(.24f+i*.025f))+world.mover.path.Up(arc[i])*(Mathf.Sin(phase[i]*1.4f)*.04f*dt);}
                targets[i].transform.Rotate(11*dt,22*dt,7*dt);
                if((!world.freeTools&&arc[i]>world.mover.path.Anchor(7)+13)||(targets[i].transform.position-world.mover.viewCamera.transform.position).sqrMagnitude>14*14)
                {ResetBubble(i);if(i==ActiveIndex&&(world.mission.Current?.action==StudyAction.Capture||world.mission.Current?.action==StudyAction.Deposit))world.mission.RetryEmbolus(true);}
            }
        }
        public void DriftCaptured(float dt){if(initialized)Drift(ActiveIndex,dt);}
        void Drift(int i,float dt)
        {
            driftClock[i]-=dt;if(driftClock[i]<=0){driftClock[i]=Random.Range(.5f,1.4f);drift[i]=Random.insideUnitSphere*1.1f;}
            var frame=world.mover.path.Frame(arc[i]);var noise=frame*drift[i];var centre=world.mover.path.Centre(world.mover.path.Anchor(7)+5);
            var restore=Vector3.ClampMagnitude(centre-targets[i].transform.position,2)*.10f;
            targets[i].transform.position+=(noise+restore+world.mover.path.Forward(arc[i])*.16f)*dt;
        }
    }
}
