using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace BiologyVR.ArteryRoute.Editor
{
    public static class ArteryVrValidation
    {
        public static string LastResult {get;private set;}="Not run";
        [MenuItem("Biology VR/Validate Runtime Transfer")]
        public static void Run()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode first");
            LastResult="Running";EditorCoroutineUtility.StartCoroutineOwnerless(Check());
        }
        static IEnumerator Check()
        {
            var route=UnityEngine.Object.FindFirstObjectByType<ArteryRouteController>();
            var study=UnityEngine.Object.FindFirstObjectByType<ArteryStudyPresenter>();
            if(!route||!study){LastResult="FAIL: route/presenter missing";yield break;}
            route.GoTo(0,true);yield return null;yield return null;
            var followers=UnityEngine.Object.FindObjectsByType<BloodCellFollower>(FindObjectsSortMode.None);
            if(followers.Length==0){LastResult="FAIL: no active blood flow";yield break;}
            var first=followers[0];Vector3 before=first.transform.position;
            yield return new EditorWaitForSeconds(.3f);
            bool flowMoved=(first.transform.position-before).sqrMagnitude>.00001f;
            int scans=study.ScanCount;study.Scan();bool scannerWorked=study.ScanCount==scans+1&&study.scanText.text.Contains("Эритроцит");
            int grabModels=UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None).Length;
            float startError=Vector3.Distance(route.viewCamera.transform.position,ArteryRouteController.Centre(-4)+ArteryRouteController.Up(-4)*.1f);
            BuildArteryVrScene.CapturePreview();
            route.GoTo(9,true);yield return null;yield return null;
            float haemostasisError=Vector3.Distance(route.viewCamera.transform.position,ArteryRouteController.Centre(212)+ArteryRouteController.Up(212)*.1f);
            BuildArteryVrScene.CapturePreview();
            route.GoTo(12,true);yield return null;yield return null;BuildArteryVrScene.CapturePreview();
            route.GoTo(0,true);route.GoTo(1);
            yield return new EditorWaitForSeconds(.8f);
            bool transitionMoved=route.RouteY>-4 && route.RouteY<20 && route.IsTravelling;
            route.GoTo(0,true);yield return null;
            var data=new {passed=flowMoved&&scannerWorked&&grabModels>=3&&startError<.1f&&haemostasisError<.1f&&transitionMoved,flowMoved,activeFlowObjects=followers.Length,scannerWorked,nearHandXRGrabModels=grabModels,startCameraError=startError,haemostasisCameraError=haemostasisError,curvedTransitionMoved=transitionMoved,headsetConnected=XRSettings.isDeviceActive,hardwareVRTested=false,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path};
            LastResult=JsonConvert.SerializeObject(data,Formatting.Indented);
            File.WriteAllText(BuildArteryVrScene.Root+"/Reports/RuntimeValidation.json",LastResult);
            Debug.Log("Biology VR runtime validation: "+LastResult);
        }
    }
}
