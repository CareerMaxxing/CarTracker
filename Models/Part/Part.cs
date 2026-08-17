namespace CarCareTracker.Models
{
    /// <summary>
    /// A reusable part catalog entry - not vehicle-scoped and not owned by any single purchase.
    /// Price/quantity belong to PartPurchase, not here (see docs/DATA_MODEL.md Phase 1 notes).
    /// </summary>
    public class Part
    {
        public int Id { get; set; }
        public string PartNumber { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<UploadedFiles> Files { get; set; } = new List<UploadedFiles>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<ExtraField> ExtraFields { get; set; } = new List<ExtraField>();
    }
}
