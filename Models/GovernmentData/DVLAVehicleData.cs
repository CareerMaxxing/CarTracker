namespace CarCareTracker.Models
{
    /// <summary>
    /// Read-only, externally-sourced data - never persisted via the normal I*DataAccess pattern
    /// (see docs/DATA_MODEL.md "keep authoritative external data read-only" principle). Field
    /// names/shape deliberately mirror the real DVLA Vehicle Enquiry Service response so a future
    /// real IDVLAAdapter implementation is a drop-in swap for MockDVLAAdapter with no domain or
    /// controller changes - see docs/execution/PHASE_08.md.
    /// </summary>
    public class DVLAVehicleData
    {
        public bool Found { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string TaxStatus { get; set; } = string.Empty;
        public string TaxDueDate { get; set; } = string.Empty;
        public string MotStatus { get; set; } = string.Empty;
        public string MotExpiryDate { get; set; } = string.Empty;
        public string Make { get; set; } = string.Empty;
        public int YearOfManufacture { get; set; }
        public int EngineCapacity { get; set; }
        public int Co2Emissions { get; set; }
        public string FuelType { get; set; } = string.Empty;
        public string Colour { get; set; } = string.Empty;
        public bool MarkedForExport { get; set; }
        public string DateOfLastV5CIssued { get; set; } = string.Empty;
        public string Wheelplan { get; set; } = string.Empty;
        public string MonthOfFirstRegistration { get; set; } = string.Empty;
        /// <summary>True for MockDVLAAdapter, false once a real adapter is wired in - lets the UI
        /// clearly label mock data so users aren't misled into thinking it's authoritative.</summary>
        public bool IsMockData { get; set; }
    }
}
