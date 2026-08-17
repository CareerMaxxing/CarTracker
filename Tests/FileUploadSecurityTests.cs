using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CarCareTracker.Tests
{
    /// <summary>
    /// Covers the Phase 14 fix: FilesController.UploadFile used to accept any file type, and since
    /// /documents and /images serve files back same-origin with a content-type inferred from
    /// extension, an uploaded .html/.svg with embedded script became stored XSS reachable by
    /// anyone who opened it. See PHASE_14.md.
    /// </summary>
    [Collection("CarTracker")]
    public class FileUploadSecurityTests
    {
        private readonly HttpClient _client;

        public FileUploadSecurityTests(CarTrackerWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private static MultipartFormDataContent SingleFilePayload(string fileName, string content)
        {
            var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
            form.Add(fileContent, "file", fileName);
            return form;
        }

        [Theory]
        [InlineData("malicious.html", "<script>alert(1)</script>")]
        [InlineData("malicious.svg", "<svg xmlns='http://www.w3.org/2000/svg'><script>alert(1)</script></svg>")]
        [InlineData("malicious.js", "alert(1)")]
        public async Task UploadingDangerousExtension_IsRejected(string fileName, string content)
        {
            var response = await _client.PostAsync("/Files/HandleFileUpload", SingleFilePayload(fileName, content));
            response.EnsureSuccessStatusCode();
            var body = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetString();
            Assert.True(string.IsNullOrEmpty(body), $"expected an empty/rejected response for {fileName}, got: {body}");
        }

        [Fact]
        public async Task UploadingNormalFile_Succeeds()
        {
            var response = await _client.PostAsync("/Files/HandleFileUpload", SingleFilePayload("receipt.txt", "a normal receipt"));
            response.EnsureSuccessStatusCode();
            var body = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetString();
            Assert.False(string.IsNullOrEmpty(body));
            Assert.StartsWith("/temp/", body);
        }

        [Fact]
        public async Task MixedMultiFileUpload_KeepsOnlyTheLegitimateFile()
        {
            var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("a normal receipt")), "file", "receipt.txt");
            form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("<script>alert(1)</script>")), "file", "malicious.html");

            var response = await _client.PostAsync("/Files/HandleMultipleFileUpload", form);
            response.EnsureSuccessStatusCode();
            var results = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(1, results.GetArrayLength());
            Assert.Equal("receipt.txt", results[0].GetProperty("name").GetString());
        }
    }
}
