using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace BiologyVR.ArteryRoute
{
    /// <summary>XRI 3.4.1 indexes its Awake-only poke cache during editor-only OnDisable.
    /// Base cleanup runs first; ignore only that missing editor cache. Runtime errors remain visible.</summary>
    public sealed class ArteryTrackedRaycaster : TrackedDeviceGraphicRaycaster
    {
#if UNITY_EDITOR
        static bool stoppingEditorPlay;
        [UnityEditor.InitializeOnLoadMethod]
        static void ObserveEditorStop()
        {UnityEditor.EditorApplication.playModeStateChanged+=state=>stoppingEditorPlay=state==UnityEditor.PlayModeStateChange.ExitingPlayMode;}
#endif
        static bool EditorTeardown
        {
            get
            {
#if UNITY_EDITOR
                return !Application.isPlaying||stoppingEditorPlay;
#else
                return false;
#endif
            }
        }
        protected override void OnDisable()
        {
            try {base.OnDisable();}
            catch(KeyNotFoundException) when(EditorTeardown) { }
        }
    }
}
