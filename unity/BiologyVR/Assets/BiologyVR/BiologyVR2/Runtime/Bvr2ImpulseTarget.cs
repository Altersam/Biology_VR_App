using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>Anything the Impulse can hit precisely: lipid droplets (chapter 2), surplus clots (chapter 4).</summary>
    public abstract class Bvr2ImpulseTarget : MonoBehaviour
    {
        public float radius = 0.4f;
        public abstract bool Alive { get; }
        /// <summary>Can be hit right now without penalty.</summary>
        public virtual bool Open => true;
        /// <summary>Open, with a small timing grace for the bolt's flight.</summary>
        public virtual bool OpenWithGrace => Open;
        public abstract void Hit(bool open);
        protected virtual void OnEnable() { if (!Bvr2Impulse.Targets.Contains(this)) Bvr2Impulse.Targets.Add(this); }
        protected virtual void OnDisable() { Bvr2Impulse.Targets.Remove(this); }
    }
}
