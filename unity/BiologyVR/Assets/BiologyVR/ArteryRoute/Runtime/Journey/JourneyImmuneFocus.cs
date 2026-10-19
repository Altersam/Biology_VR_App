using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    public enum ImmuneFocusState{Hidden,Available,Targeted,Treated}
    public sealed class JourneyImmuneFocus:MonoBehaviour
    {
        public JourneyTarget target;
        public Renderer[] cells;
        public Transform surfacePoint;
        public int index,responders=3;
        public ImmuneFocusState State{get;set;}
        public Vector3[] initialScales;
        public Renderer[] fragments;
        MaterialPropertyBlock feedback;
        void Awake(){EnsureScales();}
        void EnsureScales(){if(initialScales!=null&&initialScales.Length==cells.Length)return;initialScales=new Vector3[cells.Length];for(int i=0;i<cells.Length;i++)initialScales[i]=cells[i].transform.localScale;}
        public void ResetFocus(){EnsureScales();State=ImmuneFocusState.Hidden;for(int i=0;i<cells.Length;i++){cells[i].transform.localScale=initialScales[i];cells[i].enabled=true;}if(fragments!=null)foreach(var part in fragments)if(part)part.enabled=false;}
        public void Present(bool visible,bool selected)
        {
            feedback??=new MaterialPropertyBlock();feedback.SetColor("_EmissionColor",selected?new Color(.025f,.055f,.06f):Color.black);
            foreach(var cell in cells){cell.enabled=visible&&State!=ImmuneFocusState.Treated;cell.SetPropertyBlock(feedback);}
            if(!visible&&fragments!=null)foreach(var part in fragments)if(part)part.enabled=false;
        }
        public void Shrink(float progress)
        {
            for(int i=0;i<cells.Length;i++)cells[i].transform.localScale=initialScales[i]*Mathf.Lerp(1,.02f,progress);
            if(fragments==null)return;
            for(int i=0;i<fragments.Length;i++)
            {
                var part=fragments[i];if(!part)continue;int cell=i/3;float a=(i%3)*Mathf.PI*2/3+cell;
                part.enabled=progress>.12f&&progress<.98f;float fade=1-Mathf.InverseLerp(.45f,1,progress);
                part.transform.localPosition=cells[cell].transform.localPosition+new Vector3(0,Mathf.Sin(a),Mathf.Cos(a))*(.035f+progress*.12f);
                part.transform.localScale=Vector3.one*(.045f*fade);
            }
        }
    }
}
