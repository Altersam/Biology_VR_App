using UnityEngine;

namespace BiologyVR.ArteryRoute.Journey
{
    public enum JourneyResearchState{NotInvestigated,Investigating,Investigated}
    /// <summary>Independent, run-persistent journal state on an existing interaction target.</summary>
    [RequireComponent(typeof(JourneyTarget))]
    public sealed class JourneyResearchableObject:MonoBehaviour
    {
        public JourneyResearchTheory theory;
        public JourneyResearchManager manager;
        public JourneyTarget target;
        [SerializeField] JourneyResearchState state;
        [SerializeField] bool introduced;
        [SerializeField] int page;
        public JourneyResearchState State=>state;
        public bool Introduced=>introduced;
        public int Page=>page;
        public void ResetRun(){state=JourneyResearchState.NotInvestigated;introduced=false;page=0;}
        internal bool Begin(){bool first=!introduced;introduced=true;if(state==JourneyResearchState.NotInvestigated)state=JourneyResearchState.Investigating;return first;}
        internal bool CompleteResearch(){if(state==JourneyResearchState.Investigated)return false;state=JourneyResearchState.Investigated;return true;}
        internal void SetPage(int value){page=Mathf.Clamp(value,0,theory.pages.Length-1);}
    }
}
