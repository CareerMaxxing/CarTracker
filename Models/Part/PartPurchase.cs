namespace CarCareTracker.Models
{
    /// <summary>
    /// A specific purchase/transaction of a Part - owns cost and quantity, vehicle-scoped like
    /// SupplyRecord (VehicleId == 0 means shop-wide, matching the existing convention).
    /// Cost is set once at purchase time and is not mutated afterward; Quantity/QuantityRemaining
    /// follow the same consume/restore mechanism SupplyRecord already uses (not yet wired to any
    /// consuming record type in this increment - see docs/execution/PHASE_05.md).
    /// </summary>
    public class PartPurchase
    {
        public int Id { get; set; }
        public int PartId { get; set; }
        public int VehicleId { get; set; }
        public DateTime Date { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityRemaining { get; set; }
        public decimal Cost { get; set; }
        public string Supplier { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public List<UploadedFiles> Files { get; set; } = new List<UploadedFiles>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<ExtraField> ExtraFields { get; set; } = new List<ExtraField>();
        public List<SupplyUsageHistory> RequisitionHistory { get; set; } = new List<SupplyUsageHistory>();
    }
}
