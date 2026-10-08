using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace BiologyVR.ArteryRoute
{
    /// <summary>XRI 3.4.1 indexes its Awake-only poke cache during editor-only OnDisable.
    /// Base cleanup runs first; ignore only that missing editor cache. Runtime errors remain visible.</summary>
    public sealed class ArteryTrackedRaycaster : TrackedDeviceGraphicRaycaster
    {
        protected override void OnDisable()
        {
            try {base.OnDisable();}
            catch(KeyNotFoundException) when(!Application.isPlaying) { }
        }
    }
}
