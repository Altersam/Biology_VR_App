using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Low-cost educational transition polish: soft wall pulse and scene accent, never horror VFX.</summary>
    public sealed class JourneyArteryPresentation : MonoBehaviour
    {
        public JourneyWorld world;
        public Renderer[] wall;
        [Range(0,.02f)] public float PulseAmount=.005f;
        [Range(40,120)] public float PulseBpm=72;
        MaterialPropertyBlock block;
        int previousScene=-1;float transition;
        static readonly int Pulse=Shader.PropertyToID("_PulseAmplitude");
        static readonly int Bpm=Shader.PropertyToID("_PulseBpm");
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
            float wave=Mathf.Min(.02f,PulseAmount+(1-Mathf.SmoothStep(0,1,transition))*.0015f);
            for(int i=0;i<wall.Length;i++)if(wall[i])
            {
                wall[i].GetPropertyBlock(block);block.SetFloat(Pulse,wave);block.SetFloat(Bpm,PulseBpm);block.SetFloat(Tone,world.VesselTone);block.SetColor(Emission,Color.black);wall[i].SetPropertyBlock(block);
            }
        }
    }
}
