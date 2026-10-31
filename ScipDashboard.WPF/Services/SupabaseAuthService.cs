using System;
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
    }
}
