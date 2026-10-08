using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyFinalFocus : MonoBehaviour
    {
        [SerializeField] JourneyMission mission;
        [SerializeField] Renderer membrane,nucleus;
        [SerializeField] Renderer[] buds,particles;
        MaterialPropertyBlock block;
        float cueTime;
        void LateUpdate()
        {
            if(!mission||mission.SceneNumber!=15)return;
            if(block==null)block=new MaterialPropertyBlock();
            int step=mission.StepIndex;float pulse=.5f+.5f*Mathf.Sin(Time.time*2.2f);
            var audio=GetComponentInParent<AudioSource>();if(step==0&&audio&&Time.time>cueTime){audio.Play();cueTime=Time.time+2.7f;}
            if(membrane){membrane.GetPropertyBlock(block);block.SetColor("_BaseColor",Color.Lerp(new Color(.92f,.36f,.48f),new Color(1,.48f,.50f),pulse*.08f));membrane.SetPropertyBlock(block);}
            if(nucleus){nucleus.GetPropertyBlock(block);block.SetFloat("_Smoothness",.42f);nucleus.SetPropertyBlock(block);}
            for(int i=0;i<buds.Length;i++)if(buds[i])
            {
                bool visible=step<=1||(step==2&&i<4)||step==3;
                buds[i].enabled=visible;float angle=i*Mathf.PI*2/buds.Length+Time.time*.12f;
                buds[i].transform.localPosition=new Vector3(Mathf.Cos(angle)*(.48f+.035f*Mathf.Sin(Time.time*1.3f+i)),Mathf.Sin(angle*1.7f)*.30f,Mathf.Sin(angle)*.48f);
                buds[i].transform.Rotate(0,Time.deltaTime*19,0,Space.Self);
            }
            for(int i=0;i<particles.Length;i++)if(particles[i]){particles[i].enabled=step<=1;particles[i].transform.Rotate(0,Time.deltaTime*(10+i),0,Space.Self);}
        }
    }
}
