using CarCareTracker.Filter;
using CarCareTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers
{
    public partial class APIController
    {
        // Mocked DVLA/DVSA lookups only - see IDVLAAdapter.cs/IDVSAAdapter.cs and CLAUDE.md's
        // locked "Government data" decision. Looked up by the vehicle's LicensePlate specifically
        // (not the configurable VehicleIdentifier display field) since that's what a real DVLA/DVSA
        // lookup is always keyed by.
        [TypeFilter(typeof(CollaboratorFilter))]
        [HttpGet]
        [Route("/api/vehicle/governmentdata")]
        public IActionResult GetGovernmentDataForVehicle(int vehicleId)
        {
            if (vehicleId == default)
            {
                Response.StatusCode = 400;
                return Json(OperationResponse.Failed("Must provide a valid vehicle id"));
            }
            var vehicle = _dataAccess.GetVehicleById(vehicleId);
            if (vehicle == null || vehicle.Id == default)
            {
                Response.StatusCode = 404;
                return Json(OperationResponse.Failed("Vehicle not found"));
            }
            var result = new VehicleGovernmentDataViewModel
            {
                DVLAData = _dvlaAdapter.GetVehicleData(vehicle.LicensePlate),
                MotHistory = _dvsaAdapter.GetMotHistory(vehicle.LicensePlate)
            };
            return Json(result);
        }
    }
}
