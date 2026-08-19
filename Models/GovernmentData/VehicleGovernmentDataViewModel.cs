namespace CarCareTracker.Models
{
    /// <summary>Combines the two mocked government data lookups for display - see IDVLAAdapter.cs / IDVSAAdapter.cs.</summary>
    public class VehicleGovernmentDataViewModel
    {
        public int VehicleId { get; set; }
        public DVLAVehicleData DVLAData { get; set; } = new DVLAVehicleData();
        public DVSAMotHistory MotHistory { get; set; } = new DVSAMotHistory();
        /// <summary>MOT advisory dedup keys (StaticHelper.GetMotAdvisoryKey) that already have a
        /// linked Planner item for this vehicle - lets the view show "Added" instead of "Add to
        /// Planner" without a separate round trip. See PHASE_17.md Increment 5.</summary>
        public List<string> ExistingMotPlanKeys { get; set; } = new List<string>();
        /// <summary>The subset of ExistingMotPlanKeys whose linked Planner item has been marked
        /// resolved (PlanRecord.ResolvedDate set) - lets the view distinguish "added but still open"
        /// from "added and actually addressed", answering "has everything been addressed" at a glance.
        /// See PHASE_17.md Increment 8.</summary>
        public List<string> ResolvedMotPlanKeys { get; set; } = new List<string>();
    }
}
