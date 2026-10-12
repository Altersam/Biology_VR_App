using System;
using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    [Serializable]
    public sealed class JourneyResearchPage
    {
        public string heading;
        [TextArea(2,5)] public string text;
        public string visualConnection;
    }
    [CreateAssetMenu(menuName="Biology VR/Blood research theory")]
    public sealed class JourneyResearchTheory:ScriptableObject
    {
        public string id,displayName;
        public bool requiredForCompletion;
        [TextArea(1,3)] public string professorComment;
        public JourneyResearchPage[] pages;
    }
}
