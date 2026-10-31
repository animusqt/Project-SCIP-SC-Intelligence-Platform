using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScipDashboard.Core.Data
{
    /// <summary>
    /// Supabase REST API client wrapper using HttpClient.
    /// Configured with Supabase URL and API key.
    /// </summary>
    public class SupabaseClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _apiKey;
        private string? _accessToken;

        public SupabaseClient(string supabaseUrl, string apiKey)
        {
            _supabaseUrl = supabaseUrl.TrimEnd('/');
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("apikey", apiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
        }

        /// <summary>
        /// Sets the JWT access token for authenticated requests.
        /// </summary>
        public void SetAccessToken(string? token)
        {
            _accessToken = token;
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        /// <summary>
        /// Clears the access token, reverting to anon key.
        /// </summary>
        public void ClearAccessToken()
        {
            SetAccessToken(null);
        }

        /// <summary>
        /// GET request to a Supabase table endpoint.
        /// </summary>
        public async Task<string> GetAsync(string table, string? filter = null, string? order = null, int? limit = null, int? offset = null)
        {
            var url = $"{_supabaseUrl}/rest/v1/{table}";
            var query = new List<string>();

            if (!string.IsNullOrEmpty(filter)) query.Add($"filter={Uri.EscapeDataString(filter)}");
            if (!string.IsNullOrEmpty(order)) query.Add($"order={Uri.EscapeDataString(order)}");
            if (limit.HasValue) query.Add($"limit={limit.Value}");
            if (offset.HasValue) query.Add($"offset={offset.Value}");

            if (query.Count > 0)
                url += "?" + string.Join("&", query);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// POST request to insert a new record.
        /// </summary>
        public async Task<string> PostAsync(string table, object data)
        {
            var url = $"{_supabaseUrl}/rest/v1/{table}";
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("charset", "utf-8"));

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// PATCH request to update a record by filter.
        /// </summary>
        public async Task<string> PatchAsync(string table, string filter, object data)
        {
            var url = $"{_supabaseUrl}/rest/v1/{table}?{filter}";
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("charset", "utf-8"));

            var response = await _httpClient.PatchAsync(url, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// DELETE request to remove a record by filter.
        /// </summary>
        public async Task DeleteAsync(string table, string filter)
        {
            var url = $"{_supabaseUrl}/rest/v1/{table}?{filter}";
            var response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Sign in with email and password via Supabase Auth.
        /// </summary>
        public async Task<(string? AccessToken, string? UserId, string? Error)> SignInAsync(string email, string password)
        {
            var url = $"{_supabaseUrl}/auth/v1/token?grant_type=password";
            var payload = new { email, password };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (null, null, responseBody);

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
            var user = root.TryGetProperty("user", out var u) ? u : default;
            var userId = user.TryGetProperty("id", out var id) ? id.GetString() : null;

            return (accessToken, userId, null);
        }

        /// <summary>
        /// Sign up with email and password via Supabase Auth.
        /// </summary>
        public async Task<(string? AccessToken, string? UserId, string? Error)> SignUpAsync(string email, string password)
        {
            var url = $"{_supabaseUrl}/auth/v1/signup";
            var payload = new { email, password };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return (null, null, responseBody);

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            var accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null;
            var user = root.TryGetProperty("user", out var u) ? u : default;
            var userId = user.TryGetProperty("id", out var id) ? id.GetString() : null;

            return (accessToken, userId, null);
        }

        /// <summary>
        /// Sign out the current user.
        /// </summary>
        public async Task<bool> SignOutAsync()
        {
            var url = $"{_supabaseUrl}/auth/v1/logout";
            var response = await _httpClient.PostAsync(url, null);
            ClearAccessToken();
            return response.IsSuccessStatusCode;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
