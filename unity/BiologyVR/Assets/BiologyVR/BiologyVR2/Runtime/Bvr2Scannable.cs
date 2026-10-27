using System;
using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>Anything in the world that the scanner can study (wall layers, features). Needs a collider.</summary>
    public sealed class Bvr2Scannable : MonoBehaviour
    {
        public string codexId;
        public string displayName;
        public float scanSeconds = 1.1f;
        public bool scannable = true;
        public Renderer highlight;
        public event Action OnScanned;
        public void Fire() => OnScanned?.Invoke();
    }
}
