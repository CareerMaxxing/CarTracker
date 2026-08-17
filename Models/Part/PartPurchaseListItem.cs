namespace CarCareTracker.Models
{
    /// <summary>
    /// Pairs a PartPurchase with its Part for list rendering, avoiding an N+1 lookup per row.
    /// </summary>
    public class PartPurchaseListItem
    {
        public PartPurchase Purchase { get; set; } = new PartPurchase();
        public Part Part { get; set; } = new Part();
    }
}
