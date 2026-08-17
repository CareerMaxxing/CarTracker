namespace CarCareTracker.Models
{
    /// <summary>
    /// Read-only, externally-sourced data - see DVLAVehicleData.cs for the same rationale. Shape
    /// mirrors the real DVSA MOT History API response.
    /// </summary>
    public class DVSAMotHistory
    {
        public bool Found { get; set; }
        public string Registration { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string FirstUsedDate { get; set; } = string.Empty;
        public string FuelType { get; set; } = string.Empty;
        public string PrimaryColour { get; set; } = string.Empty;
        public List<DVSAMotTest> MotTests { get; set; } = new List<DVSAMotTest>();
        public bool IsMockData { get; set; }
    }
    public class DVSAMotTest
    {
        public string CompletedDate { get; set; } = string.Empty;
        /// <summary>"PASSED" or "FAILED".</summary>
        public string TestResult { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string OdometerValue { get; set; } = string.Empty;
        public string OdometerUnit { get; set; } = string.Empty;
        public string MotTestNumber { get; set; } = string.Empty;
        public List<DVSAMotComment> RfrAndComments { get; set; } = new List<DVSAMotComment>();
    }
    public class DVSAMotComment
    {
        public string Text { get; set; } = string.Empty;
        /// <summary>"ADVISORY", "MINOR", "MAJOR", "DANGEROUS", or "FAIL" per the real API.</summary>
        public string Type { get; set; } = string.Empty;
    }
}
