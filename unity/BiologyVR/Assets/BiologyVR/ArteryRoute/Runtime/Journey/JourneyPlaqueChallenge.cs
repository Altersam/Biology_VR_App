using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Scene06 arcade teaching model: a growing, destructible stenosing mass.</summary>
    public sealed class JourneyPlaqueChallenge:MonoBehaviour
    {
        public const int LipidCount=30;
        public const int LipidCapacity=90;
        public JourneyWorld world;
        public Transform site,bulk,repairResidue;
        public JourneyPlaqueLipid[] lipids;
        public Transform[] shards;
        public LineRenderer impactRing;
        public Collider diagnosisContact;
        public Collider[] legacyContacts;
        public Renderer[] legacyVisuals;
        public Collider[] measurementContacts,modeContacts;
        public Renderer[] measurementVisuals,modeVisuals;
        public float occlusionSeconds=55;
        public int DestroyedCount{get;private set;}
        public int Failures{get;private set;}
        public int AcceptedHits{get;private set;}
        public int SpawnedCount{get;private set;}
        public int IncomingCount{get;private set;}
        public int AttachedCount{get;private set;}
        public int RemainingCount=>SpawnedCount-DestroyedCount;
        public int ReinforcementCount=>SpawnedCount-LipidCount;
        public float Elapsed{get;private set;}
        public float Obstruction{get;private set;}=.56f;
        public float FlowFraction=>Mathf.Clamp01(1-Obstruction);
        public float RemainingSeconds=>Failed?0:Mathf.Max(0,occlusionSeconds-Elapsed);
        public bool Failed{get;private set;}
        public bool Cleared=>SpawnedCount>=LipidCount&&RemainingCount==0;
        public bool Combat=>world&&world.mission.SceneNumber==6&&world.mission.Current?.action==StudyAction.Pulse&&world.mission.Current.target=="lipid";
        public bool Ready=>formation>=1;
        public float PulseBpm=>Mathf.Lerp(72,116,Obstruction);
        public float OxygenPercent=>Mathf.Lerp(98,90,Obstruction);
        public int SelectedIndex{get;private set;}=-1;
        bool[] destroyed;
        bool[] present,incoming;
        float[] arrivalAge;
        Vector3[] arrivalStart;
        System.Random random;
        float nextArrival;
        float[] breakAge,shardAge;
        Vector3[] positions,scales,shardVelocity;
        Quaternion[] rotations;
        Vector3 bulkScale;
        MaterialPropertyBlock block;
        int scene=-1,shardCursor;
        bool wasCombat;
        float formation,failedAge,impactAge=1,refreshAt;
        Color cream=new Color(.91f,.65f,.25f),danger=new Color(1,.23f,.10f);
        void Awake()
        {
            destroyed=new bool[lipids.Length];breakAge=new float[lipids.Length];positions=new Vector3[lipids.Length];scales=new Vector3[lipids.Length];rotations=new Quaternion[lipids.Length];
            present=new bool[lipids.Length];incoming=new bool[lipids.Length];arrivalAge=new float[lipids.Length];arrivalStart=new Vector3[lipids.Length];random=new System.Random(System.Environment.TickCount^GetInstanceID());
            for(int i=0;i<lipids.Length;i++){positions[i]=lipids[i].transform.localPosition;scales[i]=lipids[i].transform.localScale;rotations[i]=lipids[i].transform.localRotation;}
            shardAge=new float[shards.Length];shardVelocity=new Vector3[shards.Length];foreach(var shard in shards)shard.gameObject.SetActive(false);
            bulkScale=bulk.localScale;block=new MaterialPropertyBlock();site.gameObject.SetActive(false);BeginAttempt();
        }
        void BeginAttempt()
        {
            DestroyedCount=0;SpawnedCount=LipidCount;IncomingCount=0;AttachedCount=LipidCount;SelectedIndex=-1;Elapsed=0;Obstruction=.56f;Failed=false;failedAge=0;impactAge=1;nextArrival=1;
            if(world&&world.mission.Goals!=null&&world.mission.Current?.action==StudyAction.Pulse)world.mission.Current.required=LipidCount;
            for(int i=0;i<destroyed.Length;i++){destroyed[i]=false;present[i]=i<LipidCount;incoming[i]=false;breakAge[i]=0;arrivalAge[i]=0;lipids[i].gameObject.SetActive(present[i]);lipids[i].transform.localScale=scales[i];}
            foreach(var shard in shards)shard.gameObject.SetActive(false);if(impactRing)impactRing.enabled=false;
        }
        public bool Select(JourneyTarget target)
        {
            if(!Combat||Failed||Cleared||!target)return false;var piece=target.GetComponent<JourneyPlaqueLipid>();
            if(!piece||piece.owner!=this||!present[piece.index]||destroyed[piece.index])return false;SelectedIndex=piece.index;return true;
        }
        public bool CanPulse=>Combat&&!Failed&&SelectedIndex>=0&&!destroyed[SelectedIndex];
        public void ApplySelectedPulse()
        {
            if(!CanPulse)return;int index=SelectedIndex;SelectedIndex=-1;destroyed[index]=true;DestroyedCount++;AcceptedHits++;if(incoming[index]){incoming[index]=false;IncomingCount--;}else AttachedCount--;breakAge[index]=0;lipids[index].contact.enabled=false;
            Vector3 point=lipids[index].transform.position;SpawnShards(point,index);impactAge=0;
            if(impactRing){impactRing.transform.SetPositionAndRotation(point,world.mover.viewCamera.transform.rotation);impactRing.enabled=true;}
            if(Cleared){Obstruction=0;world.mission.SetPlaquePressure(120);world.mission.FeedbackMessage("Просвет освобождён: липидная масса раздроблена, поток восстанавливается.");}
        }
        void SpawnShards(Vector3 point,int index)
        {
            for(int part=0;part<4;part++)
            {
                int slot=shardCursor++%shards.Length;var shard=shards[slot];shard.gameObject.SetActive(true);shard.position=point;shard.rotation=Quaternion.Euler(index*37,part*67,index*13);shard.localScale=Vector3.one*(.07f+(part%2)*.025f);shardAge[slot]=0;
                var toward=(world.mover.viewCamera.transform.position-point).normalized;shardVelocity[slot]=toward*(.8f+part*.12f)+world.mover.viewCamera.transform.right*Mathf.Cos(part*Mathf.PI*.5f)*.8f+world.mover.viewCamera.transform.up*Mathf.Sin(part*Mathf.PI*.5f)*.65f;
            }
        }
        public void ResetCheckpoint(){BeginAttempt();formation=0;wasCombat=false;}
        void SpawnIncoming()
        {
            int index=-1;for(int i=0;i<present.Length;i++)if(!present[i]||(destroyed[i]&&breakAge[i]>.25f)){index=i;break;}
            if(index<0)return;
            present[index]=true;destroyed[index]=false;incoming[index]=true;arrivalAge[index]=0;breakAge[index]=0;SpawnedCount++;IncomingCount++;
            arrivalStart[index]=new Vector3((float)random.NextDouble()*2.6f-1.3f,(float)random.NextDouble()*2-1,-2.8f-(float)random.NextDouble()*.6f);
            lipids[index].gameObject.SetActive(true);lipids[index].transform.localPosition=arrivalStart[index];lipids[index].transform.localScale=scales[index]*.32f;
            if(world.mission.Current?.action==StudyAction.Pulse)world.mission.Current.required=SpawnedCount;
        }
        void Update()
        {
            if(!world||world.mission.Goals==null)return;var mission=world.mission;bool here=mission.SceneNumber==6;
            if(mission.SceneNumber>=17){site.gameObject.SetActive(false);if(impactRing)impactRing.enabled=false;return;}
            foreach(var contact in legacyContacts)if(contact)contact.enabled=false;
            foreach(var visual in legacyVisuals)if(visual)visual.enabled=false;
            foreach(var contact in measurementContacts)if(contact)contact.enabled=!world.freeTools&&here&&mission.StepIndex==0;
            foreach(var contact in modeContacts)if(contact)contact.enabled=!world.freeTools&&here&&mission.StepIndex==2;
            foreach(var visual in measurementVisuals)if(visual)visual.enabled=!world.freeTools&&here&&mission.StepIndex==0;
            foreach(var visual in modeVisuals)if(visual)visual.enabled=!world.freeTools&&here&&mission.StepIndex==2;
            if(here&&scene!=6&&!world.IsEpisodeCompleted(6)){BeginAttempt();formation=0;wasCombat=false;}
            scene=mission.SceneNumber;site.gameObject.SetActive(here||world.IsEpisodeCompleted(6));if(!here&&!Cleared)return;
            float dt=Time.deltaTime;bool combat=Combat;
            if(here&&mission.Ready)formation=Mathf.Min(1,formation+dt/2.8f);
            if(combat&&!wasCombat){Elapsed=0;Failed=false;failedAge=0;}
            wasCombat=combat;
            if(combat&&!Failed&&!Cleared&&mission.Ready)
            {
                Elapsed+=dt;
                if(Elapsed>=nextArrival){int count=random.Next(1,3);for(int i=0;i<count;i++)SpawnIncoming();nextArrival=Elapsed+.90f+(float)random.NextDouble()*.20f;}
                Obstruction=Mathf.Clamp01(AttachedCount<=LipidCount?AttachedCount/(float)LipidCount*.56f:.56f+(AttachedCount-LipidCount)/40f*.44f);
                if(Elapsed>=occlusionSeconds||AttachedCount>=70){Failed=true;Failures++;failedAge=0;Obstruction=1;mission.RecordPhysicalError("ОККЛЮЗИЯ: новые липиды из потока перекрыли просвет. Повтор через 3 секунды.");}
            }
            if(Failed)
            {
                failedAge+=dt;
                if(failedAge>=3){BeginAttempt();formation=1;mission.RetryPlaqueChallenge();}
            }
            float growth=1;
            float visible=Mathf.SmoothStep(.35f,1,formation);
            bulk.localScale=bulkScale*(visible*Mathf.Pow(Mathf.Max(.01f,AttachedCount/(float)LipidCount),.5f));bulk.gameObject.SetActive(AttachedCount>0);
            repairResidue.gameObject.SetActive(Cleared);
            for(int i=0;i<lipids.Length;i++)
            {
                var piece=lipids[i];piece.contact.enabled=combat&&!Failed&&!destroyed[i];
                if(!present[i]){piece.contact.enabled=false;continue;}
                if(destroyed[i]){breakAge[i]+=dt;float t=Mathf.Clamp01(breakAge[i]/.18f);piece.transform.localScale=scales[i]*(growth*visible*(1-t));piece.gameObject.SetActive(t<1);}
                else
                {
                    if(incoming[i])
                    {arrivalAge[i]+=dt;float t=Mathf.Clamp01(arrivalAge[i]/1.8f);piece.transform.localPosition=Vector3.Lerp(arrivalStart[i],positions[i],t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.18f;piece.transform.localScale=scales[i]*Mathf.Lerp(.32f,1,Mathf.SmoothStep(0,1,t));if(t>=1){incoming[i]=false;IncomingCount--;AttachedCount++;}}
                    else{piece.transform.localPosition=positions[i];piece.transform.localScale=scales[i]*visible*(1+.012f*Mathf.Sin(Time.time*2.7f+i));}
                    piece.transform.localRotation=rotations[i]*Quaternion.Euler(0,0,Mathf.Sin(Time.time*1.5f+i)*1.5f);
                    piece.surface.GetPropertyBlock(block);block.SetColor("_BaseColor",Color.Lerp(cream,danger,Mathf.Max(0,Obstruction-.75f)*2.4f));piece.surface.SetPropertyBlock(block);
                }
            }
            diagnosisContact.enabled=here&&(world.freeTools?mission.Current?.target=="plaque"&&mission.Current.action==StudyAction.Scan:mission.StepIndex==1);
            for(int i=0;i<shards.Length;i++)if(shards[i].gameObject.activeSelf)
            {shardAge[i]+=dt;shards[i].position+=shardVelocity[i]*dt;shards[i].Rotate(120*dt,70*dt,90*dt);shards[i].localScale*=Mathf.Max(0,1-dt*3);if(shardAge[i]>.45f)shards[i].gameObject.SetActive(false);}
            impactAge+=dt;if(impactRing){impactRing.enabled=impactAge<.22f;impactRing.transform.localScale=Vector3.one*Mathf.Lerp(.22f,.85f,impactAge/.22f);impactRing.widthMultiplier=.016f*Mathf.Clamp01(1-impactAge/.22f);}
            if(here&&Time.time>=refreshAt){refreshAt=Time.time+.20f;mission.SetPlaquePressure(Cleared?120:world.freeTools?120+65*Obstruction:Mathf.Lerp(120,92,Obstruction));}
            RetargetLogical();
        }
        void RetargetLogical()
        {
            if(!Combat)return;var logical=world.Find("lipid");if(!logical)return;
            foreach(var piece in lipids)if(present[piece.index]&&!destroyed[piece.index]){logical.transform.position=piece.transform.position;return;}
        }
    }
}
