using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace BiologyVR.ArteryRoute.Journey
{
    public sealed class JourneyDesktopPresentation : MonoBehaviour
    {
        MonoBehaviour[] visuals;LineRenderer[] lines;bool xr;
        void Start()
        {
            visuals=GetComponentsInChildren<MonoBehaviour>(true).Where(b=>b&&b.GetType().Name=="XRInteractorLineVisual").ToArray();
            lines=visuals.SelectMany(b=>b.GetComponentsInChildren<LineRenderer>(true)).Distinct().ToArray();Apply();
        }
        void Update(){if(xr!=XRSettings.isDeviceActive)Apply();}
        void Apply(){xr=XRSettings.isDeviceActive;if(visuals!=null)foreach(var visual in visuals)if(visual)visual.enabled=xr;if(lines!=null)foreach(var line in lines)if(line)line.enabled=xr;}
    }
}
