using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    /// <summary>Cheap, readable weaving motion for the rapid-fire immune wave.</summary>
    public sealed class JourneyAssaultVirion:MonoBehaviour
    {
        public float speed,phase;
        float age;
        public void Advance(Camera camera,float deltaTime)
        {
            age+=deltaTime;
            var toward=(camera.transform.position-transform.position).normalized;
            transform.position+=toward*(speed*deltaTime)+camera.transform.right*(Mathf.Sin(age*2.8f+phase)*.35f*deltaTime)+camera.transform.up*(Mathf.Cos(age*2.1f+phase)*.22f*deltaTime);
            transform.Rotate(20*deltaTime,65*deltaTime,15*deltaTime);
        }
    }
}
