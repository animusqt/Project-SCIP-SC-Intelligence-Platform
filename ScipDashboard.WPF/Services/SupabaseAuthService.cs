using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ScipDashboard.Core.Data;

namespace ScipDashboard.WPF.Services
{
    /// <summary>
    /// Supabase authentication service - handles sign in, sign up, sign out.
    /// </summary>
    public class SupabaseAuthService
    {
        private readonly SupabaseClient _client;
        private string? _currentUserId;
        private string? _accessToken;

        public event Action<string?>? UserChanged; // Fires when user logs in/out (passes userId or null)

        public string? CurrentUserId => _currentUserId;
        public string? AccessToken => _accessToken;
        public bool IsAuthenticated => !string.IsNullOrEmpty(_currentUserId);

        public SupabaseAuthService(SupabaseClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Sign in with email and password.
        /// </summary>
        public async Task<(bool Success, string? UserId, string? ErrorMessage)> SignInAsync(string email, string password)
        {
            var (accessToken, userId, error) = await _client.SignInAsync(email, password);
            if (!string.IsNullOrEmpty(error))
                return (false, null, error);

            _accessToken = accessToken;
            _currentUserId = userId;
            _client.SetAccessToken(accessToken);

            UserChanged?.Invoke(_currentUserId);
            return (true, userId, null);
        }

        public Task<(bool Success, string? UserId, string? ErrorMessage)> SignInWithDiscordAsync() =>
            SignInWithOAuthAsync("discord");

        public async Task<(bool Success, string? UserId, string? ErrorMessage)> SignInWithGoogleAsync() =>
            await SignInWithOAuthAsync("google");

        public async Task<(bool Success, string? UserId, string? ErrorMessage)> SignInWithOAuthAsync(string provider)
        {
            if (!string.Equals(provider, "discord", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(provider, "google", StringComparison.OrdinalIgnoreCase))
                return (false, null, "Choose Discord or Google to connect your account.");

            var providerName = string.Equals(provider, "google", StringComparison.OrdinalIgnoreCase)
                ? "Google"
                : "Discord";
            using var portProbe = new TcpListener(IPAddress.Loopback, 0);
            portProbe.Start();
            var port = ((IPEndPoint)portProbe.LocalEndpoint).Port;
            portProbe.Stop();

            var redirectUri = $"http://localhost:{port}/oauth2/callback/";
            var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
            var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
            using var listener = new HttpListener();
            listener.Prefixes.Add(redirectUri);
            listener.Start();

            var authorizationUri = _client.CreateOAuthAuthorizationUri(providerName, redirectUri, challenge);
            Process.Start(new ProcessStartInfo(authorizationUri.AbsoluteUri) { UseShellExecute = true });

            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromMinutes(3));
            }
            catch (TimeoutException)
            {
                return (false, null, $"{providerName} sign-in timed out. Check that the browser completed OAuth and that the local callback URL is allow-listed in Supabase.");
            }

            var callbackError = context.Request.QueryString["error_description"] ??
                                context.Request.QueryString["error"];
            var authorizationCode = context.Request.QueryString["code"];
            var responseMessage = string.IsNullOrWhiteSpace(callbackError)
                ? "Account connection received. You can close this browser tab and return to SCIP Dashboard."
                : $"{providerName} sign-in did not complete. Return to SCIP Dashboard for details.";
            context.Response.ContentType = "text/html; charset=utf-8";
            var html = $"<!doctype html><html><body><h2>{WebUtility.HtmlEncode(responseMessage)}</h2></body></html>";
            var responseBytes = Encoding.UTF8.GetBytes(html);
            context.Response.ContentLength64 = responseBytes.Length;
            await context.Response.OutputStream.WriteAsync(responseBytes);
            context.Response.Close();

            if (!string.IsNullOrWhiteSpace(callbackError))
                return (false, null, callbackError);
            if (string.IsNullOrWhiteSpace(authorizationCode))
                return (false, null, $"Supabase returned without an authorization code. Confirm the {providerName} provider and redirect URL are configured.");

            var (accessToken, userId, error) = await _client.ExchangePkceCodeAsync(authorizationCode, verifier);
            if (!string.IsNullOrWhiteSpace(error))
                return (false, null, error);
            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
                return (false, null, $"Supabase did not return a usable {providerName} session.");

            _accessToken = accessToken;
            _currentUserId = userId;
            _client.SetAccessToken(accessToken);
            UserChanged?.Invoke(_currentUserId);
            return (true, userId, null);
        }

        /// <summary>
        /// Sign up with email and password.
        /// </summary>
        public async Task<(bool Success, string? UserId, string? ErrorMessage)> SignUpAsync(string email, string password)
        {
            var (accessToken, userId, error) = await _client.SignUpAsync(email, password);
            if (!string.IsNullOrEmpty(error))
                return (false, null, error);

            _accessToken = accessToken;
            _currentUserId = userId;
            _client.SetAccessToken(accessToken);

            UserChanged?.Invoke(_currentUserId);
            return (true, userId, null);
        }

        /// <summary>
        /// Sign out the current user.
        /// </summary>
        public async Task<bool> SignOutAsync()
        {
            var success = await _client.SignOutAsync();
            _accessToken = null;
            _currentUserId = null;
            _client.ClearAccessToken();
            UserChanged?.Invoke(null);
            return success;
        }

        /// <summary>
        /// Set the access token manually (e.g., from stored session).
        /// </summary>
        public void SetSession(string accessToken, string userId)
        {
            _accessToken = accessToken;
            _currentUserId = userId;
            _client.SetAccessToken(accessToken);
        }

        /// <summary>
        /// Clear the session.
        /// </summary>
        public void ClearSession()
        {
            _accessToken = null;
            _currentUserId = null;
            _client.ClearAccessToken();
            UserChanged?.Invoke(null);
        }

        private static string Base64Url(byte[] value) =>
            Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
