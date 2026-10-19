using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Fixed-budget opaque fragments, emitted on shell damage and final break.</summary>
    public sealed class JourneyVirusShatter:MonoBehaviour
    {
        public Transform[] pieces;
        public int Bursts{get;private set;}
        public int FinalBreaks{get;private set;}
        Vector3[] velocity;
        float[] age;
        int cursor;
        void Awake(){velocity=new Vector3[pieces.Length];age=new float[pieces.Length];foreach(var piece in pieces)piece.gameObject.SetActive(false);}
        public void Burst(Vector3 point,bool final)
        {
            Bursts++;if(final)FinalBreaks++;
            int count=final?12:4;
            for(int i=0;i<count;i++){int index=cursor++%pieces.Length;var part=pieces[index];part.SetPositionAndRotation(point,Random.rotation);part.localScale=Vector3.one*Random.Range(.035f,.075f);velocity[index]=Random.onUnitSphere*Random.Range(final?1.2f:.5f,final?2.5f:1.1f);age[index]=0;part.gameObject.SetActive(true);}
        }
        void Update(){for(int i=0;i<pieces.Length;i++){if(!pieces[i].gameObject.activeSelf)continue;age[i]+=Time.deltaTime;pieces[i].position+=velocity[i]*Time.deltaTime;velocity[i]*=Mathf.Exp(-Time.deltaTime*2);pieces[i].Rotate(80*Time.deltaTime,55*Time.deltaTime,0);pieces[i].localScale*=Mathf.Max(0,1-Time.deltaTime*.8f);if(age[i]>.85f)pieces[i].gameObject.SetActive(false);}}
    }
}
