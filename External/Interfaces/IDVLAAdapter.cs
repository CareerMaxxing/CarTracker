using CarCareTracker.Models;

namespace CarCareTracker.External.Interfaces
{
    /// <summary>
    /// Domain-facing interface for looking up vehicle tax/MOT-status data from the DVLA Vehicle
    /// Enquiry Service. Mocked-only per CLAUDE.md's locked "Government data" decision - do not
    /// implement a real HTTP-backed adapter without explicit sign-off, and never use real
    /// credentials during the mocked-adapter phase.
    /// </summary>
    public interface IDVLAAdapter
    {
        public DVLAVehicleData GetVehicleData(string registrationNumber);
    }
}
