namespace CarCareTracker.Models
{
    /// <summary>Combines the two mocked government data lookups for display - see IDVLAAdapter.cs / IDVSAAdapter.cs.</summary>
    public class VehicleGovernmentDataViewModel
    {
        public DVLAVehicleData DVLAData { get; set; } = new DVLAVehicleData();
        public DVSAMotHistory MotHistory { get; set; } = new DVSAMotHistory();
    }
}
