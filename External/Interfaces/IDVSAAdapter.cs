using CarCareTracker.Models;

namespace CarCareTracker.External.Interfaces
{
    /// <summary>
    /// Domain-facing interface for looking up MOT test history from the DVSA MOT History API.
    /// Mocked-only per CLAUDE.md's locked "Government data" decision - see IDVLAAdapter.cs.
    /// </summary>
    public interface IDVSAAdapter
    {
        public DVSAMotHistory GetMotHistory(string registrationNumber);
    }
}
