using System.Net.Http.Json;
using System.Text.Json;

namespace CarCareTracker.Tests
{
    /// <summary>
    /// Covers Phase 17 Increment 5: turning MOT advisories into Planner items, deduped per vehicle by
    /// StaticHelper.GetMotAdvisoryKey. "MOTPLAN003" is a deliberately chosen plate string - the mock
    /// DVSA generator is deterministically seeded per-plate, and this one reliably produces exactly 3
    /// distinct advisories (including "Front tyre worn close to legal limit") across its mock MOT
    /// tests, with no in-data recurrence - good enough to prove the controller wiring itself (the
    /// recurring-advisory collapse behavior is already covered at the pure-function level by
    /// MotAdvisoryNormalizationTests). See PHASE_17.md.
    /// </summary>
    [Collection("CarTracker")]
    public class MotAdvisoryPlannerLinkageTests
    {
        private readonly HttpClient _client;

        public MotAdvisoryPlannerLinkageTests(CarTrackerWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task AddSingleAdvisory_ThenAddAgain_IsDeduped()
        {
            var vehicleId = await ApiTestHelpers.CreateVehicleAsync(_client, "MOTPLAN003");
            try
            {
                var firstAdd = await _client.PostAsync(
                    $"/Vehicle/AddMotAdvisoryToPlanner?vehicleId={vehicleId}&advisoryText={Uri.EscapeDataString("Front tyre worn close to legal limit")}",
                    content: null);
                firstAdd.EnsureSuccessStatusCode();
                var firstAddJson = await firstAdd.Content.ReadFromJsonAsync<JsonElement>();
                Assert.True(firstAddJson.GetProperty("success").GetBoolean());

                var secondAdd = await _client.PostAsync(
                    $"/Vehicle/AddMotAdvisoryToPlanner?vehicleId={vehicleId}&advisoryText={Uri.EscapeDataString("Front tyre worn close to legal limit")}",
                    content: null);
                secondAdd.EnsureSuccessStatusCode();
                var secondAddJson = await secondAdd.Content.ReadFromJsonAsync<JsonElement>();
                Assert.False(secondAddJson.GetProperty("success").GetBoolean());

                var planRecords = await _client.GetFromJsonAsync<JsonElement>($"/api/vehicle/planrecords?vehicleId={vehicleId}");
                Assert.Equal(1, planRecords.GetArrayLength());
            }
            finally
            {
                await ApiTestHelpers.DeleteVehicleAsync(_client, vehicleId);
            }
        }

        [Fact]
        public async Task ImportAll_SkipsAlreadyAdded_AndIsFullyIdempotentOnRepeatedCalls()
        {
            var vehicleId = await ApiTestHelpers.CreateVehicleAsync(_client, "MOTPLAN003");
            try
            {
                //pre-add one of the three known advisories by hand first.
                var preAdd = await _client.PostAsync(
                    $"/Vehicle/AddMotAdvisoryToPlanner?vehicleId={vehicleId}&advisoryText={Uri.EscapeDataString("Front tyre worn close to legal limit")}",
                    content: null);
                preAdd.EnsureSuccessStatusCode();

                var firstImport = await _client.PostAsync($"/Vehicle/ImportAllMotAdvisoriesToPlanner?vehicleId={vehicleId}", content: null);
                firstImport.EnsureSuccessStatusCode();
                var firstImportJson = await firstImport.Content.ReadFromJsonAsync<JsonElement>();
                Assert.True(firstImportJson.GetProperty("success").GetBoolean());
                Assert.Equal(2, firstImportJson.GetProperty("additionalData").GetProperty("addedCount").GetInt32());

                var planRecordsAfterFirstImport = await _client.GetFromJsonAsync<JsonElement>($"/api/vehicle/planrecords?vehicleId={vehicleId}");
                Assert.Equal(3, planRecordsAfterFirstImport.GetArrayLength());

                var secondImport = await _client.PostAsync($"/Vehicle/ImportAllMotAdvisoriesToPlanner?vehicleId={vehicleId}", content: null);
                secondImport.EnsureSuccessStatusCode();
                var secondImportJson = await secondImport.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(0, secondImportJson.GetProperty("additionalData").GetProperty("addedCount").GetInt32());

                var planRecordsAfterSecondImport = await _client.GetFromJsonAsync<JsonElement>($"/api/vehicle/planrecords?vehicleId={vehicleId}");
                Assert.Equal(3, planRecordsAfterSecondImport.GetArrayLength());
            }
            finally
            {
                await ApiTestHelpers.DeleteVehicleAsync(_client, vehicleId);
            }
        }
    }
}
