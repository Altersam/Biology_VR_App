using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using BiologyVR.ArteryRoute.Journey;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryHudDiagnostic
    {
        public const string Revision="hud-diagnostic-20261002-v1";
        [MenuItem("Biology VR/Inspect Saved HUD Structure")]
        public static void Inspect()
        {
            var rows=new List<object>();
            foreach(var g in UnityEngine.Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                bool cached=false;string error=null;try{cached=g.canvasRenderer!=null;}catch(Exception e){error=e.Message;}
                rows.Add(new{name=g.name,id=g.GetInstanceID(),type=g.GetType().Name,active=g.gameObject.activeInHierarchy,enabled=g.enabled,renderer=g.GetComponent<CanvasRenderer>()!=null,cachedRenderer=cached,error});
            }
            var h=UnityEngine.Object.FindFirstObjectByType<JourneyHud>();var cr=UnityEngine.Object.FindObjectsByType<CanvasRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var panel=h&&h.panelRoot?h.panelRoot:null;
            Directory.CreateDirectory(ArteryPolishReview.Reports);
            File.WriteAllText(ArteryPolishReview.Reports+"/HudDiagnostic.json",JsonConvert.SerializeObject(new{revision=Revision,playing=EditorApplication.isPlaying,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,graphicCount=rows.Count,canvasRendererCount=cr.Length,panelPosition=panel?panel.localPosition.ToString("R"):"missing",panelScale=panel?panel.localScale.ToString("R"):"missing",panelSize=panel?panel.sizeDelta.ToString("R"):"missing",graphics=rows},Formatting.Indented));
        }
    }
}
