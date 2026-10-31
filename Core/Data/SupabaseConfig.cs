using System;

namespace ScipDashboard.Core.Data
{
    /// <summary>
    /// Supabase connection configuration.
    /// Configure with your Supabase project URL and API key.
    /// Get these from your Supabase dashboard: Project Settings -> API
    /// </summary>
    public class SupabaseConfig
    {
        /// <summary>
        /// Your Supabase project URL (e.g., https://xxx.supabase.co)
        /// </summary>
        public string SupabaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Supabase anon/public API key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Supabase secret/service role key (for server-side operations)
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Supabase JWKS URL for JWT verification
        /// </summary>
        public string JwksUrl { get; set; } = string.Empty;

        /// <summary>
        /// Whether Supabase integration is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        public bool IsConfigured => !string.IsNullOrEmpty(SupabaseUrl) && !string.IsNullOrEmpty(ApiKey);

        public static SupabaseConfig CreateExample()
        {
            return new SupabaseConfig
            {
                SupabaseUrl = "https://your-project.supabase.co",
                ApiKey = "your-anon-key",
                SecretKey = "your-secret-key",
                JwksUrl = "https://your-project.supabase.co/auth/v1/.well-known/jwks.json",
                IsEnabled = false
            };
        }
    }
}
