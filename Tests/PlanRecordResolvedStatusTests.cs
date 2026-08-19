using System.Net.Http.Json;
using System.Text.Json;

namespace CarCareTracker.Tests
{
    /// <summary>
    /// Covers Phase 17 Increment 6: the lighter "mark resolved" status for MOT-linked Planner items.
    /// Deliberately does NOT touch PlanProgress/the Done pipeline - see PlanCompletionIdempotencyTests
    /// for that separate, untouched behavior. See PHASE_17.md.
    /// </summary>
    [Collection("CarTracker")]
    public class PlanRecordResolvedStatusTests
    {
        private readonly HttpClient _client;

        public PlanRecordResolvedStatusTests(CarTrackerWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<int> AddAdvisoryAndGetPlanRecordIdAsync(int vehicleId, string advisoryText)
        {
            var addResponse = await _client.PostAsync(
                $"/Vehicle/AddMotAdvisoryToPlanner?vehicleId={vehicleId}&advisoryText={Uri.EscapeDataString(advisoryText)}",
                content: null);
            addResponse.EnsureSuccessStatusCode();
            var planRecords = await _client.GetFromJsonAsync<JsonElement>($"/api/vehicle/planrecords?vehicleId={vehicleId}");
            foreach (var record in planRecords.EnumerateArray())
            {
                if (record.GetProperty("description").GetString() == advisoryText)
                {
                    return int.Parse(record.GetProperty("id").GetString()!);
                }
            }
            throw new InvalidOperationException("Newly-added advisory PlanRecord not found");
        }

        [Fact]
        public async Task MarkThenUnmarkResolved_BothSucceed_MarkingTwiceIsHarmless()
        {
            var vehicleId = await ApiTestHelpers.CreateVehicleAsync(_client, "MOTPLAN003");
            try
            {
                var planRecordId = await AddAdvisoryAndGetPlanRecordIdAsync(vehicleId, "Front tyre worn close to legal limit");

                var mark1 = await _client.PostAsync($"/Vehicle/MarkPlanRecordResolved?planRecordId={planRecordId}", content: null);
                mark1.EnsureSuccessStatusCode();
                Assert.True((await mark1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("success").GetBoolean());

                //marking an already-resolved item again is harmless, not an error.
                var mark2 = await _client.PostAsync($"/Vehicle/MarkPlanRecordResolved?planRecordId={planRecordId}", content: null);
                mark2.EnsureSuccessStatusCode();
                Assert.True((await mark2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("success").GetBoolean());

                var unmark = await _client.PostAsync($"/Vehicle/UnmarkPlanRecordResolved?planRecordId={planRecordId}", content: null);
                unmark.EnsureSuccessStatusCode();
                Assert.True((await unmark.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("success").GetBoolean());
            }
            finally
            {
                await ApiTestHelpers.DeleteVehicleAsync(_client, vehicleId);
            }
        }

        [Fact]
        public async Task MarkResolved_NonexistentPlanRecord_Fails()
        {
            var response = await _client.PostAsync("/Vehicle/MarkPlanRecordResolved?planRecordId=999999999", content: null);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(json.GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task ResolvingAnMotItem_DoesNotChangeItsProgress()
        {
            var vehicleId = await ApiTestHelpers.CreateVehicleAsync(_client, "MOTPLAN003");
            try
            {
                var planRecordId = await AddAdvisoryAndGetPlanRecordIdAsync(vehicleId, "Windscreen wiper blade worn or damaged");
                var mark = await _client.PostAsync($"/Vehicle/MarkPlanRecordResolved?planRecordId={planRecordId}", content: null);
                mark.EnsureSuccessStatusCode();

                var planRecords = await _client.GetFromJsonAsync<JsonElement>($"/api/vehicle/planrecords?vehicleId={vehicleId}");
                var record = planRecords.EnumerateArray().First(x => x.GetProperty("id").GetString() == planRecordId.ToString());
                //resolving must stay orthogonal to Progress - still sitting in the Idea lane it was
                //created in, never silently moved to Done (which would auto-create a ServiceRecord).
                Assert.Equal("Idea", record.GetProperty("progress").GetString());
            }
            finally
            {
                await ApiTestHelpers.DeleteVehicleAsync(_client, vehicleId);
            }
        }
    }
}
