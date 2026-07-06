using CarCareTracker.Models;

namespace CarCareTracker.External.Interfaces
{
    public interface IDBHealthCheck
    {
        public DatabaseHealth GetDatabaseHealth();
    }
}
