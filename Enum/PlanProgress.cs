namespace CarCareTracker.Models
{
    public enum PlanProgress
    {
        /// <summary>
        /// Renamed from Backlog (same underlying value - existing/serialized data reads correctly
        /// unchanged) to match the target 5-stage pipeline: Idea/Costed/PartsSourced/InProgress/Done.
        /// See docs/execution/PHASE_06.md.
        /// </summary>
        Idea = 0,
        InProgress = 1,
        Testing = 2,
        Done = 3,
        Costed = 4,
        PartsSourced = 5
    }
}
