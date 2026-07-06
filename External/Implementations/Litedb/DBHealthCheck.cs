using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models;

namespace CarCareTracker.External.Implementations
{
    public class DBHealthCheck : IDBHealthCheck
    {
        private ILiteDBHelper _liteDB { get; set; }
        private ILogger<DBHealthCheck> _logger { get; set; }
        public DBHealthCheck(ILiteDBHelper liteDB, ILogger<DBHealthCheck> logger)
        {
            _liteDB = liteDB;
            _logger = logger;
        }
        public DatabaseHealth GetDatabaseHealth()
        {
            try
            {
                var db = _liteDB.GetLiteDB();
                return new DatabaseHealth
                {
                    DatabaseName = "LiteDB",
                    Version = db.UserVersion.ToString(),
                    Status = "pass"
                };
            } catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return new DatabaseHealth();
            }
        }
    }
}
