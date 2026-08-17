using CarCareTracker.External.Interfaces;
using CarCareTracker.Models;

namespace CarCareTracker.Logic
{
    public interface IOdometerLogic
    {
        int GetLastOdometerRecordMileage(int vehicleId, List<OdometerRecord> odometerRecords);
        bool AutoInsertOdometerRecord(OdometerRecord odometer);
        List<OdometerRecord> AutoConvertOdometerRecord(List<OdometerRecord> odometerRecords);
        bool IsSuspiciousMileageRegression(int vehicleId, int newMileage, int excludeRecordId = 0);
    }
    public class OdometerLogic: IOdometerLogic
    {
        private readonly IOdometerRecordDataAccess _odometerRecordDataAccess;
        private readonly IEquipmentRecordDataAccess _equipmentRecordDataAccess;
        private readonly IVehicleDataAccess _vehicleDataAccess;
        private readonly ILogger<IOdometerLogic> _logger;
        public OdometerLogic(IOdometerRecordDataAccess odometerRecordDataAccess, IEquipmentRecordDataAccess equipmentRecordDataAccess, IVehicleDataAccess vehicleDataAccess, ILogger<IOdometerLogic> logger)
        {
            _odometerRecordDataAccess = odometerRecordDataAccess;
            _equipmentRecordDataAccess = equipmentRecordDataAccess;
            _vehicleDataAccess = vehicleDataAccess;
            _logger = logger;
        }
        public bool IsSuspiciousMileageRegression(int vehicleId, int newMileage, int excludeRecordId = 0)
        {
            //FR-ODO-02: HasOdometerAdjustment means the vehicle's odometer was intentionally
            //replaced/rolled back (e.g. new instrument cluster) - regressions are expected there,
            //not suspicious.
            var vehicle = _vehicleDataAccess.GetVehicleById(vehicleId);
            if (vehicle == null || vehicle.Id == default || vehicle.HasOdometerAdjustment)
            {
                return false;
            }
            var existingRecords = _odometerRecordDataAccess.GetOdometerRecordsByVehicleId(vehicleId);
            if (excludeRecordId != default)
            {
                existingRecords.RemoveAll(x => x.Id == excludeRecordId);
            }
            var lastReportedMileage = GetLastOdometerRecordMileage(vehicleId, existingRecords);
            return lastReportedMileage != default && newMileage < lastReportedMileage;
        }
        public int GetLastOdometerRecordMileage(int vehicleId, List<OdometerRecord> odometerRecords)
        {
            if (!odometerRecords.Any())
            {
                odometerRecords = _odometerRecordDataAccess.GetOdometerRecordsByVehicleId(vehicleId);
            }
            if (!odometerRecords.Any())
            {
                //no existing odometer records for this vehicle.
                return 0;
            }
            return odometerRecords.Max(x => x.Mileage);
        }
        public bool AutoInsertOdometerRecord(OdometerRecord odometer)
        {
            if (odometer.Mileage == default)
            {
                return false;
            }
            var lastReportedMileage = GetLastOdometerRecordMileage(odometer.VehicleId, new List<OdometerRecord>());
            odometer.InitialMileage = lastReportedMileage != default ? lastReportedMileage : odometer.Mileage;
            //add equipment
            var equipmentRecords = _equipmentRecordDataAccess.GetEquipmentRecordsByVehicleId(odometer.VehicleId);
            var equippedEquipment = equipmentRecords.Where(x => x.IsEquipped);
            if (equippedEquipment.Any())
            {
                odometer.EquipmentRecordId = equippedEquipment.Select(x => x.Id).ToList();
            }
            var result = _odometerRecordDataAccess.SaveOdometerRecordToVehicle(odometer);
            return result;
        }
        public List<OdometerRecord> AutoConvertOdometerRecord(List<OdometerRecord> odometerRecords)
        {
            //perform ordering
            odometerRecords = odometerRecords.OrderBy(x => x.Date).ThenBy(x => x.Mileage).ToList();
            int previousMileage = 0;
            for (int i = 0; i < odometerRecords.Count; i++)
            {
                var currentObject = odometerRecords[i];
                if (previousMileage == default)
                {
                    //first record
                    currentObject.InitialMileage = currentObject.Mileage;
                }
                else
                {
                    //subsequent records
                    currentObject.InitialMileage = previousMileage;
                }
                //save to db.
                _odometerRecordDataAccess.SaveOdometerRecordToVehicle(currentObject);
                previousMileage = currentObject.Mileage;
            }
            return odometerRecords;
        }
    }
}
