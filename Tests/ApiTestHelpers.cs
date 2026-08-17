using System.Net.Http.Json;
using System.Text.Json;

namespace CarCareTracker.Tests
{
    /// <summary>Small shared helpers so each test doesn't hand-roll vehicle setup/teardown - mirrors
    /// the throwaway-vehicle-create-then-delete pattern used throughout manual curl verification in
    /// docs/execution/PHASE_*.md.</summary>
    public static class ApiTestHelpers
    {
        public static async Task<int> CreateVehicleAsync(HttpClient client, string licensePlate)
        {
            var response = await client.PostAsJsonAsync("/api/vehicles/add", new
            {
                year = "2020",
                make = "TestMake",
                model = "TestModel",
                licensePlate
            });
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("additionalData").GetProperty("vehicleId").GetInt32();
        }

        public static async Task DeleteVehicleAsync(HttpClient client, int vehicleId)
        {
            var response = await client.DeleteAsync($"/api/vehicles/delete?id={vehicleId}");
            response.EnsureSuccessStatusCode();
        }
    }
}
