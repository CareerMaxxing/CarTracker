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
    }
}
