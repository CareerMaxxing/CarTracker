using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarCareTracker.External.Implementations.Mock;
using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models;

namespace CarCareTracker.External.Implementations.Real
{
    /// <summary>
    /// Calls the real DVSA MOT History API when DVSAConfig credentials are configured (read live, per
    /// call, via IConfigHelper - no restart needed after the Setup UI saves credentials); falls back to
    /// MockDVSAAdapter's deterministic fake data otherwise. See IDVSAAdapter.cs - CLAUDE.md's
    /// "Government data" decision was revisited with the user's explicit sign-off in Phase 17.
    /// </summary>
    public class RealDVSAAdapter : IDVSAAdapter
    {
        private const string TokenScope = "https://tapi.dvsa.gov.uk/.default";
        private const string ApiBaseUrl = "https://history.mot.api.gov.uk/v1/trade/vehicles/registration/";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IConfigHelper _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RealDVSAAdapter> _logger;
        private readonly MockDVSAAdapter _mockAdapter = new MockDVSAAdapter();

        private readonly object _tokenLock = new object();
        private string? _cachedAccessToken;
        private DateTime _cachedTokenExpiresUtc = DateTime.MinValue;

        public RealDVSAAdapter(IConfigHelper config, IHttpClientFactory httpClientFactory, ILogger<RealDVSAAdapter> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public DVSAMotHistory GetMotHistory(string registrationNumber)
        {
            var normalized = (registrationNumber ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return _mockAdapter.GetMotHistory(normalized);
            }
            var dvsaConfig = _config.GetDVSAConfig();
            bool isConfigured = !string.IsNullOrWhiteSpace(dvsaConfig.TenantId)
                && !string.IsNullOrWhiteSpace(dvsaConfig.ClientId)
                && !string.IsNullOrWhiteSpace(dvsaConfig.ClientSecret)
                && !string.IsNullOrWhiteSpace(dvsaConfig.ApiKey);
            if (isConfigured)
            {
                try
                {
                    return FetchRealMotHistory(normalized, dvsaConfig);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to retrieve real DVSA MOT history for {Registration}", normalized);
                    return new DVSAMotHistory { Found = false, Registration = normalized.ToUpperInvariant(), IsMockData = false };
                }
            }
            var manualOverride = GetManualOverride(normalized);
            if (manualOverride != null)
            {
                return manualOverride;
            }
            return _mockAdapter.GetMotHistory(normalized);
        }

        /// <summary>Temporary bridge for real (not mock) data before real DVSA API credentials exist -
        /// see StaticHelper.DVSAMotOverridesPath. Superseded automatically the moment DVSAConfig is
        /// configured (checked above this call), so nothing needs cleaning up once the real API is
        /// wired up.</summary>
        private DVSAMotHistory? GetManualOverride(string registrationNumber)
        {
            if (!File.Exists(StaticHelper.DVSAMotOverridesPath))
            {
                return null;
            }
            try
            {
                var json = File.ReadAllText(StaticHelper.DVSAMotOverridesPath);
                var overrides = JsonSerializer.Deserialize<Dictionary<string, DVSAMotHistory>>(json, JsonOptions);
                if (overrides == null)
                {
                    return null;
                }
                var normalizedKey = new string(registrationNumber.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
                var match = overrides.FirstOrDefault(x => new string(x.Key.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant() == normalizedKey);
                return match.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to read manual DVSA MOT override data for {Registration}", registrationNumber);
                return null;
            }
        }

        private DVSAMotHistory FetchRealMotHistory(string registrationNumber, DVSAConfig dvsaConfig)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var accessToken = GetAccessToken(httpClient, dvsaConfig);

            using var request = new HttpRequestMessage(HttpMethod.Get, ApiBaseUrl + Uri.EscapeDataString(registrationNumber));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("x-api-key", dvsaConfig.ApiKey);

            using var response = httpClient.Send(request);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new DVSAMotHistory { Found = false, Registration = registrationNumber.ToUpperInvariant(), IsMockData = false };
            }
            response.EnsureSuccessStatusCode();

            using var stream = response.Content.ReadAsStream();
            var apiResponse = JsonSerializer.Deserialize<DVSAApiResponse>(stream, JsonOptions) ?? new DVSAApiResponse();
            return MapToMotHistory(apiResponse, registrationNumber);
        }

        /// <summary>Maps the real API's wire shape onto the app's own domain model - deliberately kept
        /// as its own DTO rather than deserializing directly into DVSAMotHistory, since the two do NOT
        /// actually match field-for-field (confirmed against a live response): the real API names its
        /// per-test list "defects", not "rfrAndComments" as originally assumed from older/deprecated
        /// docs - PropertyNameCaseInsensitive only handles casing, not a different name entirely, so
        /// that mismatch silently deserialized to an always-empty list. Keeping a dedicated wire DTO
        /// also means the app's own /api/vehicle/governmentdata response shape (which already shipped
        /// as "rfrAndComments") never has to change just because the real API calls something
        /// differently.</summary>
        private static DVSAMotHistory MapToMotHistory(DVSAApiResponse apiResponse, string registrationNumber)
        {
            return new DVSAMotHistory
            {
                Found = true,
                IsMockData = false,
                Registration = string.IsNullOrWhiteSpace(apiResponse.Registration) ? registrationNumber.ToUpperInvariant() : apiResponse.Registration,
                Make = apiResponse.Make,
                Model = apiResponse.Model,
                FirstUsedDate = apiResponse.FirstUsedDate,
                FuelType = apiResponse.FuelType,
                PrimaryColour = apiResponse.PrimaryColour,
                MotTests = apiResponse.MotTests.Select(t => new DVSAMotTest
                {
                    CompletedDate = t.CompletedDate.Length >= 10 ? t.CompletedDate.Substring(0, 10) : t.CompletedDate,
                    TestResult = t.TestResult,
                    ExpiryDate = t.ExpiryDate ?? string.Empty,
                    OdometerValue = t.OdometerValue,
                    OdometerUnit = t.OdometerUnit.ToLowerInvariant(),
                    MotTestNumber = t.MotTestNumber,
                    RfrAndComments = t.Defects.Select(d => new DVSAMotComment
                    {
                        Text = d.Text,
                        Type = d.Type,
                        Dangerous = d.Dangerous
                    }).ToList()
                }).ToList()
            };
        }

        private class DVSAApiResponse
        {
            public string Registration { get; set; } = string.Empty;
            public string Make { get; set; } = string.Empty;
            public string Model { get; set; } = string.Empty;
            public string FirstUsedDate { get; set; } = string.Empty;
            public string FuelType { get; set; } = string.Empty;
            public string PrimaryColour { get; set; } = string.Empty;
            public List<DVSAApiTest> MotTests { get; set; } = new List<DVSAApiTest>();
        }
        private class DVSAApiTest
        {
            public string CompletedDate { get; set; } = string.Empty;
            public string TestResult { get; set; } = string.Empty;
            public string? ExpiryDate { get; set; }
            public string OdometerValue { get; set; } = string.Empty;
            public string OdometerUnit { get; set; } = string.Empty;
            public string MotTestNumber { get; set; } = string.Empty;
            public List<DVSAApiDefect> Defects { get; set; } = new List<DVSAApiDefect>();
        }
        private class DVSAApiDefect
        {
            public string Text { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public bool Dangerous { get; set; }
        }

        private string GetAccessToken(HttpClient httpClient, DVSAConfig dvsaConfig)
        {
            lock (_tokenLock)
            {
                if (_cachedAccessToken != null && DateTime.UtcNow < _cachedTokenExpiresUtc)
                {
                    return _cachedAccessToken;
                }
                var tokenUrl = $"https://login.microsoftonline.com/{Uri.EscapeDataString(dvsaConfig.TenantId)}/oauth2/v2.0/token";
                var formData = new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = dvsaConfig.ClientId,
                    ["client_secret"] = dvsaConfig.ClientSecret,
                    ["scope"] = TokenScope
                };
                using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
                {
                    Content = new FormUrlEncodedContent(formData)
                };
                using var response = httpClient.Send(request);
                response.EnsureSuccessStatusCode();
                using var stream = response.Content.ReadAsStream();
                var tokenResult = JsonSerializer.Deserialize<DVSATokenResponse>(stream, JsonOptions);
                if (tokenResult == null || string.IsNullOrWhiteSpace(tokenResult.AccessToken))
                {
                    throw new InvalidOperationException("DVSA token endpoint did not return an access token.");
                }
                _cachedAccessToken = tokenResult.AccessToken;
                //refresh a minute early to avoid edge-of-expiry request failures
                _cachedTokenExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(tokenResult.ExpiresIn - 60, 60));
                return _cachedAccessToken;
            }
        }

        private class DVSATokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;
            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }
    }
}
