using System.Net.Http.Json;
using System.Text.Json;

namespace CarCareTracker.Tests
{
    /// <summary>
    /// Covers the Phase 17 Increment 2 fix: RealDVSAAdapter is now the registered IDVSAAdapter, but
    /// with no DVSAConfig credentials saved (the test app's default state) it must fall back to the
    /// exact same deterministic mock behavior MockDVSAAdapter always provided - no exceptions, no
    /// silent "not found" regression for a plate that used to return mock data. See PHASE_17.md.
    /// </summary>
    [Collection("CarTracker")]
    public class DVSAAdapterFallbackTests
    {
        private readonly HttpClient _client;

        public DVSAAdapterFallbackTests(CarTrackerWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task UnconfiguredDVSACredentials_FallsBackToMockMotHistory()
        {
            var vehicleId = await ApiTestHelpers.CreateVehicleAsync(_client, "DVSAFALLBK1");
            try
            {
                var response = await _client.GetAsync($"/api/vehicle/governmentdata?vehicleId={vehicleId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();

                var motHistory = json.GetProperty("motHistory");
                Assert.True(motHistory.GetProperty("found").GetBoolean());
                Assert.True(motHistory.GetProperty("isMockData").GetBoolean());
                Assert.True(motHistory.GetProperty("motTests").GetArrayLength() > 0);
            }
            finally
            {
                await ApiTestHelpers.DeleteVehicleAsync(_client, vehicleId);
            }
        }
    }
}
