using UnityEngine;

namespace BiologyVR.V2
{
    /// <summary>Idle spin and gentle bob for beacons and props.</summary>
    public sealed class Bvr2Spin : MonoBehaviour
    {
        public Vector3 speed = new Vector3(0, 40, 0);
        public float bob = 0.05f;
        void Update()
        {
            transform.Rotate(speed * Time.deltaTime, Space.Self);
            if (transform.childCount > 0) transform.GetChild(0).localPosition = Vector3.up * Mathf.Sin(Time.time * 2f) * bob;
        }
    }
}
