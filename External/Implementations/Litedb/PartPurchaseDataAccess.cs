using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models;
using LiteDB;

namespace CarCareTracker.External.Implementations
{
    public class PartPurchaseDataAccess : IPartPurchaseDataAccess
    {
        private ILiteDBHelper _liteDB { get; set; }
        private static string tableName = "partpurchases";
        public PartPurchaseDataAccess(ILiteDBHelper liteDB)
        {
           _liteDB = liteDB;
        }
        public List<PartPurchase> GetPartPurchasesByVehicleId(int vehicleId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<PartPurchase>(tableName);
            var partPurchases = table.Find(Query.EQ(nameof(PartPurchase.VehicleId), vehicleId));
            return partPurchases.ToList() ?? new List<PartPurchase>();
        }
        public List<PartPurchase> GetPartPurchasesByPartId(int partId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<PartPurchase>(tableName);
            var partPurchases = table.Find(Query.EQ(nameof(PartPurchase.PartId), partId));
            return partPurchases.ToList() ?? new List<PartPurchase>();
        }
        public PartPurchase GetPartPurchaseById(int partPurchaseId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<PartPurchase>(tableName);
            return table.FindById(partPurchaseId);
        }
        public bool DeletePartPurchaseById(int partPurchaseId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<PartPurchase>(tableName);
            table.Delete(partPurchaseId);
            db.Checkpoint();
            return true;
        }
        public bool SavePartPurchaseToVehicle(PartPurchase partPurchase)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<PartPurchase>(tableName);
            table.Upsert(partPurchase);
            db.Checkpoint();
            return true;
        }
        public bool DeleteAllPartPurchasesByVehicleId(int vehicleId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<PartPurchase>(tableName);
            table.DeleteMany(Query.EQ(nameof(PartPurchase.VehicleId), vehicleId));
            db.Checkpoint();
            return true;
        }
    }
}
