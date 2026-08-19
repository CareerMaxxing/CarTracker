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
            if (!isConfigured)
            {
                return _mockAdapter.GetMotHistory(normalized);
            }
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
            //the model's shape already mirrors the real API response - see DVSAMotHistory.cs
            var motHistory = JsonSerializer.Deserialize<DVSAMotHistory>(stream, JsonOptions) ?? new DVSAMotHistory();
            motHistory.Found = true;
            motHistory.IsMockData = false;
            if (string.IsNullOrWhiteSpace(motHistory.Registration))
            {
                motHistory.Registration = registrationNumber.ToUpperInvariant();
            }
            return motHistory;
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
