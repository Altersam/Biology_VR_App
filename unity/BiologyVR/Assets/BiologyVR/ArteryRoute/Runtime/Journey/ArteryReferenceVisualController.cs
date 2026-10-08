using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class ArteryReferenceVisualController : MonoBehaviour
    {
        public JourneyMission mission;
        public GameObject[] sceneRoots;
        public int[] sceneIds;
        void Start(){if(mission)mission.Changed+=Refresh;Refresh();}
        void OnDestroy(){if(mission)mission.Changed-=Refresh;}
        public void Refresh()
        {
            if(sceneRoots==null||sceneIds==null)return;
            for(int i=0;i<sceneRoots.Length;i++)if(sceneRoots[i])
            {
                int scene=i<sceneIds.Length?sceneIds[i]:-1;
                sceneRoots[i].SetActive(mission&&mission.SceneNumber==scene);
            }
        }
    }
}
