using UnityEngine;
using UnityEngine.UI;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Renderer-only story guidance. It never modifies mission, input or vessel state.</summary>
    [DefaultExecutionOrder(3200)]
    public sealed class ArteryBioWorldV5View:MonoBehaviour
    {
        public JourneyWorld world;
        public Renderer[] decorations;
        public int[] decorationZone;
        public Text[] labels;
        public Vector3[] labelPositions;
        public string[] captions;
        public int[] firstScenes,lastScenes;
        public float[] zoneArcs;
        public Renderer[] walls;
        public float[] wallArcs;
        public Color cyan=new Color(.27f,.90f,.94f),lime=new Color(.69f,.88f,.30f);
        MaterialPropertyBlock zoneBlock;
        void LateUpdate()
        {
            if(!world||!world.mission||!world.mover||!world.mover.viewCamera)return;
            var camera=world.mover.viewCamera;int scene=world.mission.SceneNumber;
            bool artery=scene>=3&&scene<=16;
            if(walls!=null&&wallArcs!=null)
            {
                if(zoneBlock==null)zoneBlock=new MaterialPropertyBlock();
                var path=world.mover.path;
                for(int i=0;i<walls.Length&&i<wallArcs.Length;i++)
                {
                    var wall=walls[i];if(!wall)continue;float arc=wallArcs[i];
                    Color tint=Color.white;
                    if(arc<path.Anchor(5)-5)tint=new Color(1,.985f,.96f);
                    else if(arc<path.Anchor(6)-5)tint=new Color(1,.94f,.88f);
                    else if(arc<path.Anchor(7)-5)tint=new Color(1,.96f,.87f);
                    else if(arc<path.Anchor(10)-5)tint=new Color(1,.90f,.94f);
                    else if(arc<path.Anchor(11)-6)tint=new Color(.98f,.94f,.96f);
                    else if(arc<path.Anchor(15)-8)
                    {
                        tint=scene<12?new Color(.98f,.88f,.88f):new Color(1,.94f,.95f);
                        if(world.RepairGrowth>0)tint=Color.Lerp(tint,new Color(1,1,.96f),world.RepairGrowth);
                    }
                    wall.GetPropertyBlock(zoneBlock);zoneBlock.SetColor("_ZoneTint",tint);wall.SetPropertyBlock(zoneBlock);
                }
            }
            for(int i=0;i<decorations.Length;i++)
            {
                var r=decorations[i];if(!r)continue;int zone=decorationZone[i];
                bool nearby=(r.bounds.ClosestPoint(camera.transform.position)-camera.transform.position).sqrMagnitude<42*42;
                bool woundStage=zone<4||(scene>=firstScenes[zone]&&scene<=lastScenes[zone])||(zone==6&&world.IsEpisodeCompleted(14));
                r.enabled=artery&&nearby&&woundStage;
            }
            for(int i=0;i<labels.Length;i++)
            {
                var label=labels[i];if(!label)continue;
                bool current=artery&&scene>=firstScenes[i]&&scene<=lastScenes[i];label.enabled=current;
                if(!current)continue;
                Vector3 position=labelPositions[i];
                if(i==6&&scene>=15)position=world.mover.path.Offset(world.mover.path.Anchor(scene)+1.2f,-1.55f,1.20f);
                label.transform.SetPositionAndRotation(position,camera.transform.rotation);
                label.text=i==6&&scene==15?"07 · ИММУННАЯ ЗАЩИТА\nНейтрализация очага · очистка комплексов":i==6&&scene==16?"07 · ГОМЕОСТАЗ\nШесть измерений · восстановленный баланс":captions[i];
                label.color=i==0?lime:i==6?Color.Lerp(cyan,lime,world.RepairGrowth):cyan;
            }
        }
    }
}
