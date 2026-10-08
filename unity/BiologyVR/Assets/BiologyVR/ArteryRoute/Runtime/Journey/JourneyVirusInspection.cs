using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>One enlarged model persists across genome/capsid/epitope scans.</summary>
    public sealed class JourneyVirusInspection : MonoBehaviour
    {
        [SerializeField] JourneyWorld world;
        [SerializeField] Transform model;
        [SerializeField] Renderer intact,cutaway,genome,epitope;
        [SerializeField] JourneyTarget[] targets;
        [SerializeField] Text caption;
        [SerializeField] Vector3 studyBaseScale=Vector3.one*.24f;
        MaterialPropertyBlock block;
        int lastScene=-1,lastStep=-1;
        float displayedSize=1,release;
        public Vector3 FocusPoint=>world.mover.path.Offset(world.mover.path.Anchor(8)+1.75f,.22f,.10f);
        public Renderer CurrentSurface=>cutaway&&cutaway.enabled?cutaway:intact;
        public bool Enlarged=>displayedSize>1.95f;
        public void Configure(JourneyWorld w,Transform visual,Renderer full,Renderer section,Renderer genetic,Renderer spike,JourneyTarget[] proxies,Text label)
        {world=w;model=visual;intact=full;cutaway=section;genome=genetic;epitope=spike;targets=proxies;caption=label;studyBaseScale=targets[0].transform.localScale;model.gameObject.SetActive(false);}
        void LateUpdate()
        {
            if(!world||!world.mission||targets==null)return;
            var m=world.mission;bool active=m.SceneNumber==8;bool dormant=m.SceneNumber<=16&&world.IsEpisodeCompleted(8)&&!active;var study=targets[0];
            if(active&&(lastScene!=8||(m.StepIndex==0&&lastStep>0)))
            {
                displayedSize=1;release=0;
                if(study&&!study.grabbed)
                {
                    study.transform.localScale=studyBaseScale;
                    study.transform.SetPositionAndRotation(FocusPoint,world.mover.path.Frame(world.mover.path.Anchor(8)));
                    study.SetHomePose();
                }
            }
            lastScene=m.SceneNumber;lastStep=m.StepIndex;
            model.gameObject.SetActive(active||dormant);
            if(!active&&!dormant)return;
            if(block==null)block=new MaterialPropertyBlock();
            bool completed=dormant||m.Complete;release=completed?release+Time.deltaTime*.55f:0;
            float targetSize=study&&study.grabbed?Mathf.Clamp(study.transform.localScale.x/Mathf.Max(.0001f,studyBaseScale.x),.7f,2.4f):m.StepIndex>=2&&!completed?2:1;
            displayedSize=study&&study.grabbed?targetSize:Mathf.MoveTowards(displayedSize,targetSize,Time.deltaTime*1.8f);
            Vector3 position=study?study.transform.position:FocusPoint;
            Quaternion rotation=study?study.transform.rotation:world.mover.path.Frame(world.mover.path.Anchor(8));
            if(completed){position=world.mover.path.Offset(world.mover.path.Anchor(8)+1.75f+release,.22f,.1f);rotation=world.mover.path.Frame(world.mover.path.Anchor(8)+1.75f+release);}
            else if(study&&!study.grabbed&&!study.ReturningHome)rotation*=Quaternion.Euler(0,Time.time*9,0);
            model.SetPositionAndRotation(position,rotation);
            model.localScale=Vector3.one*displayedSize;
            intact.enabled=m.StepIndex<2||completed;cutaway.enabled=m.StepIndex>=2&&!completed;
            genome.enabled=cutaway.enabled;epitope.enabled=cutaway.enabled;
            Color accent=new Color(.025f,.19f,.23f);
            foreach(var surface in new[]{cutaway,genome,epitope})if(surface)
            {
                surface.GetPropertyBlock(block);bool selected=(surface==genome&&m.StepIndex==2)||(surface==cutaway&&m.StepIndex==3)||(surface==epitope&&m.StepIndex==4);
                block.SetColor("_EmissionColor",selected?accent:Color.black);surface.SetPropertyBlock(block);
            }
            // Completed visuals keep drifting, but shared gameplay targets (notably
            // epitope in Scene15) belong to the current episode, not this old model.
            if(active)foreach(var target in targets)if(target)
            {
                if(target!=study&&!target.grabbed)
                {
                    var surface=target.targetId=="genome"?genome:target.targetId=="epitope"?epitope:cutaway;
                    target.transform.position=surface.bounds.center;
                    var contact=target.GetComponent<SphereCollider>();
                    if(contact)contact.radius=Mathf.Max(.04f,surface.bounds.extents.magnitude*.7f)/Mathf.Max(.0001f,Mathf.Abs(target.transform.lossyScale.x));
                }
                foreach(var c in target.GetComponents<Collider>())c.enabled=active&&!completed&&(target.grabbed||m.Current!=null&&m.Current.target==target.targetId);
            }
            if(caption)
            {
                caption.text=completed?"Частица возвращается в поток":m.StepIndex>=2?$"Учебный масштаб ×{displayedSize:0.0} • условное увеличение":"Исследуй, затем увеличь двумя руками";
                caption.transform.SetPositionAndRotation(model.position+world.mover.viewCamera.transform.up*.40f,world.mover.viewCamera.transform.rotation);
            }
        }
    }
}
