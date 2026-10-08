using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Six shared low-LOD cells in the educational tube; no rigidbodies or particle volumes.</summary>
    public sealed class JourneyFlowModelView : MonoBehaviour
    {
        public JourneyMission mission;
        public Transform[] cells;
        float phase;
        void Update()
        {
            if(!mission||cells==null)return;
            float speed=Mathf.Clamp(mission.ModelFlow,.15f,2f);
            if(mission.StepIndex>=2)phase+=Time.deltaTime*.22f*speed;
            for(int i=0;i<cells.Length;i++)if(cells[i])
            {
                float t=Mathf.Repeat(phase+i/(float)cells.Length,1);
                cells[i].localPosition=new Vector3(.035f*Mathf.Sin(t*Mathf.PI*2)+(i%2==0?-.06f:.06f),.04f*Mathf.Sin(i*2.4f),Mathf.Lerp(-.45f,.45f,t));
                cells[i].localRotation=Quaternion.Euler(60,0,i*37+Time.time*5);
            }
        }
    }
}
