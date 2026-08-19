using System.Net.Http.Json;
using System.Text.Json;

namespace CarCareTracker.Tests
{
    /// <summary>
    /// Covers Phase 17 Increment 10: the "Ignored" status for MOT-linked Planner items that aren't
    /// significant enough to act on (e.g. informational notes like "engine covers fitted"). Mirrors
    /// PlanRecordResolvedStatusTests but also covers IgnoreMotAdvisory's two distinct paths (create vs.
    /// mark-existing) since, unlike AddMotAdvisoryToPlanner, it's meant to work regardless of whether
    /// the advisory has already been added. See PHASE_17.md.
    /// </summary>
    [Collection("CarTracker")]
    public class PlanRecordIgnoredStatusTests
    {
        private readonly HttpClient _client;

        public PlanRecordIgnoredStatusTests(CarTrackerWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<int?> GetPlanRecordIdByDescriptionAsync(int vehicleId, string description)
        {
            var planRecords = await _client.GetFromJsonAsync<JsonElement>($"/api/vehicle/planrecords?vehicleId={vehicleId}");
            foreach (var record in planRecords.EnumerateArray())
            {
                if (record.GetProperty("description").GetString() == description)
                {
                    return int.Parse(record.GetProperty("id").GetString()!);
                }
            }
            return null;
        }

        [Fact]
        public async Task IgnoreMotAdvisory_NotYetAdded_CreatesOneIgnoredPlanRecord()
        {
            var vehicleId = await ApiTestHelpers.CreateVehicleAsync(_client, "MOTPLAN003");
            try
            {
                var ignoreResponse = await _client.PostAsync(
                    $"/Vehicle/IgnoreMotAdvisory?vehicleId={vehicleId}&advisoryText={Uri.EscapeDataString("Windscreen wiper blade worn or damaged")}",
                    content: null);
                ignoreResponse.EnsureSuccessStatusCode();
                Assert.True((await ignoreResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("success").GetBoolean());

                var planRecords = await _client.GetFromJsonAsync<JsonElement>($"/api/vehicle/planrecords?vehicleId={vehicleId}");
                Assert.Equal(1, planRecords.GetArrayLength());
            }
            finally
            {
                await ApiTestHelpers.DeleteVehicleAsync(_client, vehicleId);
            }
        }

        [Fact]
        public async Task IgnoreMotAdvisory_AlreadyAdded_MarksExistingRecordInsteadOfDuplicating()
        {
            var vehicleId = await ApiTestHelpers.CreateVehicleAsync(_client, "MOTPLAN003");
            try
            {
                var advisoryText = "Front tyre worn close to legal limit";
                var addResponse = await _client.PostAsync(
                    $"/Vehicle/AddMotAdvisoryToPlanner?vehicleId={vehicleId}&advisoryText={Uri.EscapeDataString(advisoryText)}",
                    content: null);
                addResponse.EnsureSuccessStatusCode();
                var originalId = await GetPlanRecordIdByDescriptionAsync(vehicleId, advisoryText);
                Assert.NotNull(originalId);

                var ignoreResponse = await _client.PostAsync(
                    $"/Vehicle/IgnoreMotAdvisory?vehicleId={vehicleId}&advisoryText={Uri.EscapeDataString(advisoryText)}",
                    content: null);
                ignoreResponse.EnsureSuccessStatusCode();
                Assert.True((await ignoreResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("success").GetBoolean());

                //must mark the SAME record ignored, not create a second one for the same advisory.
                var planRecords = await _client.GetFromJsonAsync<JsonElement>($"/api/vehicle/planrecords?vehicleId={vehicleId}");
                Assert.Equal(1, planRecords.GetArrayLength());
                var idAfterIgnore = await GetPlanRecordIdByDescriptionAsync(vehicleId, advisoryText);
                Assert.Equal(originalId, idAfterIgnore);
            }
            finally
            {
                await ApiTestHelpers.DeleteVehicleAsync(_client, vehicleId);
            }
        }

        [Fact]
        public async Task MarkThenUnmarkIgnored_BothSucceed_MarkingTwiceIsHarmless()
        {
            var vehicleId = await ApiTestHelpers.CreateVehicleAsync(_client, "MOTPLAN003");
            try
            {
                var advisoryText = "Nearside front brake pad(s) worn below 1.5mm";
                await (await _client.PostAsync(
                    $"/Vehicle/AddMotAdvisoryToPlanner?vehicleId={vehicleId}&advisoryText={Uri.EscapeDataString(advisoryText)}",
                    content: null)).Content.ReadAsStringAsync();
                var planRecordId = await GetPlanRecordIdByDescriptionAsync(vehicleId, advisoryText);
                Assert.NotNull(planRecordId);

                var mark1 = await _client.PostAsync($"/Vehicle/MarkPlanRecordIgnored?planRecordId={planRecordId}", content: null);
                mark1.EnsureSuccessStatusCode();
                Assert.True((await mark1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("success").GetBoolean());

                var mark2 = await _client.PostAsync($"/Vehicle/MarkPlanRecordIgnored?planRecordId={planRecordId}", content: null);
                mark2.EnsureSuccessStatusCode();
                Assert.True((await mark2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("success").GetBoolean());

                var unmark = await _client.PostAsync($"/Vehicle/UnmarkPlanRecordIgnored?planRecordId={planRecordId}", content: null);
                unmark.EnsureSuccessStatusCode();
                Assert.True((await unmark.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("success").GetBoolean());
            }
            finally
            {
                await ApiTestHelpers.DeleteVehicleAsync(_client, vehicleId);
            }
        }

        [Fact]
        public async Task MarkIgnored_NonexistentPlanRecord_Fails()
        {
            var response = await _client.PostAsync("/Vehicle/MarkPlanRecordIgnored?planRecordId=999999999", content: null);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(json.GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task IgnoringAnMotItem_DoesNotChangeItsProgress()
        {
            var vehicleId = await ApiTestHelpers.CreateVehicleAsync(_client, "MOTPLAN003");
            try
            {
                var advisoryText = "Front tyre worn close to legal limit";
                await (await _client.PostAsync(
                    $"/Vehicle/IgnoreMotAdvisory?vehicleId={vehicleId}&advisoryText={Uri.EscapeDataString(advisoryText)}",
                    content: null)).Content.ReadAsStringAsync();

                var planRecords = await _client.GetFromJsonAsync<JsonElement>($"/api/vehicle/planrecords?vehicleId={vehicleId}");
                var record = planRecords.EnumerateArray().First(x => x.GetProperty("description").GetString() == advisoryText);
                //ignoring must stay orthogonal to Progress, same as resolving - never silently moved
                //to Done or any other lane.
                Assert.Equal("Idea", record.GetProperty("progress").GetString());
            }
            finally
            {
                await ApiTestHelpers.DeleteVehicleAsync(_client, vehicleId);
            }
        }
    }
}
