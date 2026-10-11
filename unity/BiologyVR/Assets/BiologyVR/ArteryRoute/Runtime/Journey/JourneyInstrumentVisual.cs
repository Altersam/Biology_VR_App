using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Articulated, collider-free source GLB view.</summary>
    public sealed class JourneyInstrumentVisual:MonoBehaviour
    {
        public bool scanner;
        public bool vertexPalette;
        public Transform rotor,muzzle;
        public Transform[] prongs;
        public Renderer[] surfaces;
        public Text modeCaption;
        public LineRenderer modeGlyph;
        public LineRenderer activeRim;
        public float RotationDegrees{get;private set;}
        public float HookAmount{get;private set;}
        public int ModeTransitions{get;private set;}
        public int ActivationEvents{get;private set;}
        public float ModeTransitionAmount{get;private set;}
        public float ActivationAmount{get;private set;}
        float modeAge=1,activationAge=1;
        bool wasWorking;
        Vector3[] restPositions;
        Quaternion[] restRotations;
        MaterialPropertyBlock colors;
        float speed,angle;
        Transform body;
        Vector3 bodyPosition;
        Quaternion bodyRotation;
        BioToolMode lastMode;
        void Awake()
        {
            int count=prongs==null?0:prongs.Length;restPositions=new Vector3[count];restRotations=new Quaternion[count];
            for(int i=0;i<count;i++){restPositions[i]=prongs[i].localPosition;restRotations[i]=prongs[i].localRotation;}
            body=transform.Find("Active Body");if(body){bodyPosition=body.localPosition;bodyRotation=body.localRotation;}
        }
        public void Present(bool working,bool captured,float charge,BioToolMode mode,string caption,bool popup)
        {
            if(restPositions==null||restPositions.Length!=(prongs==null?0:prongs.Length))Awake();
            float delta=Time.deltaTime;
            if(working&&!wasWorking){activationAge=0;ActivationEvents++;}wasWorking=working;
            modeAge+=delta;activationAge+=delta;
            ModeTransitionAmount=modeAge<.45f?Mathf.Sin(modeAge/.45f*Mathf.PI):0;
            ActivationAmount=activationAge<.32f?Mathf.Sin(activationAge/.32f*Mathf.PI):0;
            if(colors==null)colors=new MaterialPropertyBlock();
            var glow=Color.white*(working?.30f+.38f*charge:.065f);
            foreach(var surface in surfaces)if(surface)
            {surface.GetPropertyBlock(colors);if(vertexPalette)colors.SetFloat("_Work",Mathf.Max(working?.65f+.55f*charge:0,Mathf.Max(ModeTransitionAmount,ActivationAmount)*.8f));else{colors.SetColor("_EmissionColor",glow*(scanner&&surface.name=="Active Rotor"?1.4f:1));colors.SetColor("_BaseColor",scanner&&surface.name=="Active Rotor"?new Color(.75f,1,1):surface.name.StartsWith("Active Prong")?new Color(.72f,.82f,.87f):Color.white);}surface.SetPropertyBlock(colors);}
            if(scanner)
            {
                speed=Mathf.MoveTowards(speed,working?180+charge*220:0,delta*650);angle+=speed*delta;
                if(rotor)rotor.localRotation=Quaternion.AngleAxis(angle,Vector3.forward);
                RotationDegrees=angle;
            }
            else
            {
                bool capture=mode==BioToolMode.Capture;
                if(mode!=lastMode){lastMode=mode;if(capture)speed=0;}
                float wanted=capture?0:mode==BioToolMode.Tonus?(working?45:0):mode==BioToolMode.Bio?0:(working?180+charge*480:0);
                if(!capture&&mode!=BioToolMode.Bio)wanted=Mathf.Max(wanted,ModeTransitionAmount*180);
                speed=capture?0:Mathf.MoveTowards(speed,wanted,delta*900);if(!capture)angle+=speed*delta;
                HookAmount=Mathf.MoveTowards(HookAmount,capture?(captured?1:working?.58f:ModeTransitionAmount*.32f):0,delta*3.5f);
                for(int i=0;i<prongs.Length;i++)
                {
                    var radial=restPositions[i];radial.z=0;if(radial.sqrMagnitude<.00001f)radial=Quaternion.AngleAxis(i*90,Vector3.forward)*Vector3.up;
                    radial.Normalize();var tangent=Vector3.Cross(Vector3.forward,radial);
                    if(capture)
                    {prongs[i].localPosition=restPositions[i]+radial*.022f*(1-HookAmount)+Vector3.forward*.012f*HookAmount;prongs[i].localRotation=Quaternion.AngleAxis(Mathf.Lerp(-20,12,HookAmount),tangent)*restRotations[i];}
                    else if(mode==BioToolMode.Bio)
                    {float breathe=(working?Mathf.Sin(Time.time*13)*8:0)+ModeTransitionAmount*12+ActivationAmount*10;prongs[i].localPosition=restPositions[i]+radial*ActivationAmount*.009f;prongs[i].localRotation=Quaternion.AngleAxis(breathe,tangent)*restRotations[i];}
                    else
                     {
                         // Rotate the complete four-part crown about the device's
                         // muzzle axis: each pivot orbits, not just its own mesh.
                         var rotation=Quaternion.AngleAxis(angle,Vector3.forward);
                         prongs[i].localPosition=rotation*restPositions[i];
                         prongs[i].localRotation=rotation*restRotations[i];
                     }
                }
                RotationDegrees=angle;
            }
            if(modeCaption){modeCaption.text=caption;modeCaption.enabled=popup||working;modeCaption.color=HolographicStyle.Secondary;}
            if(modeGlyph)
            {
                modeGlyph.enabled=working||popup;int points=scanner?32:mode==BioToolMode.Impulse?3:mode==BioToolMode.Capture?4:mode==BioToolMode.Tonus?24:6;
                modeGlyph.positionCount=points;
                for(int i=0;i<points;i++){float a=i*Mathf.PI*2/points;modeGlyph.SetPosition(i,new Vector3(Mathf.Cos(a)*.018f,Mathf.Sin(a)*.018f,0));}
            }
            if(activeRim)
            {
                activeRim.enabled=working&&(scanner||mode==BioToolMode.Impulse||mode==BioToolMode.Bio);
                activeRim.widthMultiplier=scanner?.00125f:.00165f;
            }
        }
        public void StopEffects(){speed=0;if(modeGlyph)modeGlyph.enabled=false;if(activeRim)activeRim.enabled=false;}
        public void BeginModeTransition(){modeAge=0;ModeTransitions++;}
        public void AnimateHousing(float pulse,bool working)
        {
            if(!body)return;
            float breathe=working?Mathf.Sin(Time.time*(scanner?7:10))*.0007f:0;
            body.localPosition=bodyPosition+Vector3.back*(pulse*.008f)+Vector3.up*(breathe+ModeTransitionAmount*.002f);
            body.localRotation=bodyRotation*Quaternion.Euler(-pulse*3,0,(working&&scanner?Mathf.Sin(Time.time*6)*.4f:0)+ModeTransitionAmount*3);
        }
    }
}
