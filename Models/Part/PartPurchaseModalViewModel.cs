namespace CarCareTracker.Models
{
    /// <summary>
    /// Wraps a PartPurchaseInput with the full Part catalog so the add/edit modal can render a
    /// picker without a separate round-trip.
    /// </summary>
    public class PartPurchaseModalViewModel
    {
        public PartPurchaseInput Input { get; set; } = new PartPurchaseInput();
        public List<Part> AvailableParts { get; set; } = new List<Part>();
    }
}
