using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Low-cost educational transition polish: soft wall pulse and scene accent, never horror VFX.</summary>
    public sealed class JourneyArteryPresentation : MonoBehaviour
    {
        public JourneyWorld world;
        public Renderer[] wall;
        MaterialPropertyBlock block;
        int previousScene=-1;float transition;
        static readonly int Pulse=Shader.PropertyToID("_PulseAmplitude");
        static readonly int Tone=Shader.PropertyToID("_Tone");
        static readonly int Emission=Shader.PropertyToID("_EmissionColor");
        void OnEnable(){if(!world)world=GetComponentInParent<JourneyWorld>();if(world&&world.mission)world.mission.Changed+=Changed;}
        void OnDisable(){if(world&&world.mission)world.mission.Changed-=Changed;}
        void Changed(){if(world&&world.mission&&world.mission.SceneNumber!=previousScene){previousScene=-1;transition=0;}}
        void LateUpdate()
        {
            if(!world||!world.mission||wall==null)return;
            if(block==null)block=new MaterialPropertyBlock();
            int scene=world.mission.SceneNumber;if(previousScene!=scene){previousScene=scene;transition=0;}
            transition=Mathf.MoveTowards(transition,1,Time.deltaTime/.72f);
            float wave=.005f+(1-Mathf.SmoothStep(0,1,transition))*.003f;
            Color accent=scene==6?new Color(.55f,.18f,.03f):scene==15?new Color(.15f,.10f,.55f):new Color(.01f,.18f,.18f);
            for(int i=0;i<wall.Length;i++)if(wall[i])
            {
                wall[i].GetPropertyBlock(block);block.SetFloat(Pulse,wave);block.SetFloat(Tone,world.VesselTone);block.SetColor(Emission,accent*(wave*.08f));wall[i].SetPropertyBlock(block);
            }
        }
    }
}
