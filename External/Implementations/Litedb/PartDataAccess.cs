using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models;

namespace CarCareTracker.External.Implementations
{
    public class PartDataAccess : IPartDataAccess
    {
        private ILiteDBHelper _liteDB { get; set; }
        private static string tableName = "parts";
        public PartDataAccess(ILiteDBHelper liteDB)
        {
           _liteDB = liteDB;
        }
        public List<Part> GetParts()
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<Part>(tableName);
            return table.FindAll().ToList();
        }
        public Part GetPartById(int partId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<Part>(tableName);
            return table.FindById(partId);
        }
        public bool DeletePartById(int partId)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<Part>(tableName);
            table.Delete(partId);
            db.Checkpoint();
            return true;
        }
        public bool SavePart(Part part)
        {
            var db = _liteDB.GetLiteDB();
            var table = db.GetCollection<Part>(tableName);
            table.Upsert(part);
            db.Checkpoint();
            return true;
        }
    }
}
