using System.Net.Http.Json;
using System.Text.Json;

namespace CarCareTracker.Tests
{
    /// <summary>
    /// Covers the Phase 9 fix: manually entering a mileage lower than the vehicle's last reported
    /// reading should flag (not block) a warning, unless the vehicle has HasOdometerAdjustment set
    /// (an intentional odometer replacement/rollback). See PHASE_09.md.
    /// </summary>
    [Collection("CarTracker")]
    public class OdometerRegressionTests
    {
        private readonly HttpClient _client;

        public OdometerRegressionTests(CarTrackerWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<bool?> SaveOdometerRecordAsync(int vehicleId, string date, int mileage)
        {
            var form = new Dictionary<string, string>
            {
                ["odometerRecord.vehicleId"] = vehicleId.ToString(),
                ["odometerRecord.date"] = date,
                ["odometerRecord.mileage"] = mileage.ToString()
            };
            var response = await _client.PostAsync("/Vehicle/SaveOdometerRecordToVehicleId", new FormUrlEncodedContent(form));
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (json.TryGetProperty("additionalData", out var additionalData) &&
                additionalData.ValueKind != JsonValueKind.Null &&
                additionalData.TryGetProperty("isSuspiciousRegression", out var flag))
            {
                return flag.GetBoolean();
            }
            return null;
        }

        [Fact]
        public async Task LowerMileageThanLastReading_IsFlaggedButStillSaved()
        {
            var vehicleId = await ApiTestHelpers.CreateVehicleAsync(_client, "ODOREGRESS1");
            try
            {
                var firstFlag = await SaveOdometerRecordAsync(vehicleId, "17/08/2026", 5000);
                Assert.Null(firstFlag);

                var secondFlag = await SaveOdometerRecordAsync(vehicleId, "18/08/2026", 3000);
                Assert.True(secondFlag);

                var records = await _client.GetFromJsonAsync<JsonElement>($"/api/vehicle/odometerrecords/all?vehicleId={vehicleId}");
                Assert.Equal(2, records.GetArrayLength());
            }
            finally
            {
                await ApiTestHelpers.DeleteVehicleAsync(_client, vehicleId);
            }
        }

        [Fact]
        public async Task LowerMileage_WithHasOdometerAdjustmentSet_IsNotFlagged()
        {
            var vehicleId = await ApiTestHelpers.CreateVehicleAsync(_client, "ODOREGRESS2");
            try
            {
                await SaveOdometerRecordAsync(vehicleId, "17/08/2026", 5000);

                var saveVehicleForm = new Dictionary<string, string>
                {
                    ["id"] = vehicleId.ToString(),
                    ["year"] = "2020",
                    ["make"] = "TestMake",
                    ["model"] = "TestModel",
                    ["licensePlate"] = "ODOREGRESS2",
                    ["vehicleIdentifier"] = "LicensePlate",
                    ["hasOdometerAdjustment"] = "true"
                };
                var saveVehicleResponse = await _client.PostAsync("/Vehicle/SaveVehicle", new FormUrlEncodedContent(saveVehicleForm));
                saveVehicleResponse.EnsureSuccessStatusCode();

                var flag = await SaveOdometerRecordAsync(vehicleId, "19/08/2026", 100);
                Assert.Null(flag);
            }
            finally
            {
                await ApiTestHelpers.DeleteVehicleAsync(_client, vehicleId);
            }
        }
    }
}
