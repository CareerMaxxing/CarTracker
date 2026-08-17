namespace CarCareTracker.Models
{
    public class SearchResult
    {
        public int Id { get; set; }
        /// <summary>String, not ImportMode - Phase 11 added "Part"/"PartPurchase"/"Vehicle" result
        /// types that aren't ImportMode values (see StaticHelper.GetSearchResultIcon). The view/JS
        /// already treated this as a string via ToString() either way, so this is not a breaking
        /// change to rendered markup.</summary>
        public string RecordType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
    }
}
