namespace CarCareTracker.Models
{
    /// <summary>
    /// Form-binding DTO for PartPurchase, mirroring the SupplyRecordInput convention (string Date
    /// for datepicker binding). ExtraFields kept as an empty list for now - Part/PartPurchase don't
    /// have an ImportMode value yet, see docs/execution/PHASE_05.md.
    /// </summary>
    public class PartPurchaseInput
    {
        public int Id { get; set; }
        public int PartId { get; set; }
        public int VehicleId { get; set; }
        public string Date { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Cost { get; set; }
        public string Supplier { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<UploadedFiles> Files { get; set; } = new List<UploadedFiles>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<ExtraField> ExtraFields { get; set; } = new List<ExtraField>();

        /// <summary>
        /// Does not set QuantityRemaining - that's the caller's responsibility (full Quantity for a
        /// new purchase, preserved from the existing record when editing one - see
        /// Controllers/Vehicle/PartController.cs) since nothing here knows which case applies.
        /// </summary>
        public PartPurchase ToPartPurchase()
        {
            return new PartPurchase
            {
                Id = Id,
                PartId = PartId,
                VehicleId = VehicleId,
                Date = string.IsNullOrWhiteSpace(Date) ? DateTime.Now.Date : DateTime.Parse(Date),
                Quantity = Quantity,
                Cost = Cost,
                Supplier = Supplier,
                Notes = Notes,
                Files = Files,
                Tags = Tags,
                ExtraFields = ExtraFields
            };
        }
    }
}
