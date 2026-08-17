using System.Net.Http.Json;
using System.Text.Json;

namespace CarCareTracker.Tests
{
    /// <summary>
    /// Covers the two Phase 12 bugs: PartPurchase.Files being invisible to the deep-clean unlinked-
    /// file sweep (would have permanently deleted a live attachment), and PartPurchase rows being
    /// left orphaned when their vehicle was deleted (DeleteVehicleRecords never called the
    /// already-existing DeleteAllPartPurchasesByVehicleId). See PHASE_12.md.
    /// </summary>
    [Collection("CarTracker")]
    public class PartPurchaseReliabilityTests
    {
        private readonly CarTrackerWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PartPurchaseReliabilityTests(CarTrackerWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private async Task<int> AddPartAsync(string partNumber)
        {
            var response = await _client.PostAsJsonAsync("/api/parts/add", new { partNumber, description = "Test part" });
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("additionalData").GetProperty("partId").GetInt32();
        }

        [Fact]
        public async Task DeletingVehicle_AlsoDeletesItsPartPurchases()
        {
            var vehicleId = await ApiTestHelpers.CreateVehicleAsync(_client, "PARTCASCADE1");
            var partId = await AddPartAsync("CASCADE-PART-1");
            try
            {
                var purchaseResponse = await _client.PostAsJsonAsync($"/api/vehicle/partpurchases/add?vehicleId={vehicleId}", new
                {
                    partId = partId.ToString(),
                    quantity = "1",
                    cost = "9.99",
                    date = "17/08/2026"
                });
                purchaseResponse.EnsureSuccessStatusCode();

                await ApiTestHelpers.DeleteVehicleAsync(_client, vehicleId);

                var purchasesAfterDelete = await _client.GetFromJsonAsync<JsonElement>($"/api/vehicle/partpurchases/all?vehicleId={vehicleId}");
                Assert.Equal(0, purchasesAfterDelete.GetArrayLength());
            }
            finally
            {
                await _client.DeleteAsync($"/api/parts/delete?id={partId}");
            }
        }

        [Fact]
        public async Task DeepClean_DoesNotDeleteALivePartPurchaseAttachment()
        {
            var vehicleId = await ApiTestHelpers.CreateVehicleAsync(_client, "PARTATTACH1");
            var partId = await AddPartAsync("ATTACH-PART-1");
            var documentsDir = Path.Combine(_factory.TempDataRoot, "data", "documents");
            Directory.CreateDirectory(documentsDir);
            var testFileName = $"{Guid.NewGuid()}.txt";
            await File.WriteAllTextAsync(Path.Combine(documentsDir, testFileName), "test receipt");
            try
            {
                var purchaseResponse = await _client.PostAsJsonAsync($"/api/vehicle/partpurchases/add?vehicleId={vehicleId}", new
                {
                    partId = partId.ToString(),
                    quantity = "1",
                    cost = "9.99",
                    date = "17/08/2026",
                    files = new[] { new { name = "receipt.txt", location = $"/documents/{testFileName}" } }
                });
                purchaseResponse.EnsureSuccessStatusCode();

                var cleanupResponse = await _client.GetAsync("/api/cleanup?deepClean=true");
                cleanupResponse.EnsureSuccessStatusCode();

                Assert.True(File.Exists(Path.Combine(documentsDir, testFileName)),
                    "deep-clean deleted a PartPurchase's attachment that was still referenced by a live record");
            }
            finally
            {
                await ApiTestHelpers.DeleteVehicleAsync(_client, vehicleId);
                await _client.DeleteAsync($"/api/parts/delete?id={partId}");
                File.Delete(Path.Combine(documentsDir, testFileName));
            }
        }
    }
}
