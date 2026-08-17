using CarCareTracker.Models;

namespace CarCareTracker.External.Interfaces
{
    public interface IPartPurchaseDataAccess
    {
        public List<PartPurchase> GetPartPurchasesByVehicleId(int vehicleId);
        public List<PartPurchase> GetPartPurchasesByPartId(int partId);
        public PartPurchase GetPartPurchaseById(int partPurchaseId);
        public bool DeletePartPurchaseById(int partPurchaseId);
        public bool SavePartPurchaseToVehicle(PartPurchase partPurchase);
        public bool DeleteAllPartPurchasesByVehicleId(int vehicleId);
    }
}
