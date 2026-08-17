using System.Net.Http.Json;
using System.Text.Json;

namespace CarCareTracker.Tests
{
    /// <summary>
    /// Covers the Phase 7 fix: completing the same plan record more than once must not create
    /// duplicate ServiceRecords. This was a real bug (fixed by capturing the plan's prior Progress
    /// before overwriting it, gating the conversion on "transitioning TO Done") - see PHASE_07.md.
    /// </summary>
    [Collection("CarTracker")]
    public class PlanCompletionIdempotencyTests
    {
        private readonly HttpClient _client;

        public PlanCompletionIdempotencyTests(CarTrackerWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CompletingSamePlanRecordRepeatedly_CreatesExactlyOneServiceRecord()
        {
            var vehicleId = await ApiTestHelpers.CreateVehicleAsync(_client, "IDEMPOTENT1");
            try
            {
                var addResponse = await _client.PostAsJsonAsync($"/api/vehicle/planrecords/add?vehicleId={vehicleId}", new
                {
                    description = "Replace timing belt",
                    type = "ServiceRecord",
                    priority = "Normal",
                    progress = "InProgress",
                    cost = "150"
                });
                addResponse.EnsureSuccessStatusCode();
                var addJson = await addResponse.Content.ReadFromJsonAsync<JsonElement>();
                var planRecordId = addJson.GetProperty("additionalData").GetProperty("recordId").GetInt32();

                for (int i = 0; i < 3; i++)
                {
                    var completeResponse = await _client.PostAsync(
                        $"/Vehicle/UpdatePlanRecordProgress?planRecordId={planRecordId}&planProgress=Done&odometer=1000",
                        content: null);
                    completeResponse.EnsureSuccessStatusCode();
                }

                var serviceRecordsResponse = await _client.GetFromJsonAsync<JsonElement>($"/api/vehicle/servicerecords?vehicleId={vehicleId}");
                Assert.Equal(1, serviceRecordsResponse.GetArrayLength());
            }
            finally
            {
                await ApiTestHelpers.DeleteVehicleAsync(_client, vehicleId);
            }
        }
    }
}
