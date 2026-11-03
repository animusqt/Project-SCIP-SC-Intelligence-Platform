using System;
using System.IO;
using System.Text.Json;

namespace ScipDashboard.WPF.Services
{
    public sealed class LocalSupabaseSettings
    {
        public string ProjectUrl { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
    }

    public static class LocalFeatureStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private static readonly string RootPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SCIP Dashboard");

        public static T Read<T>(string fileName) where T : new()
        {
            var path = GetPath(fileName);
            return File.Exists(path)
                ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ?? new T()
                : new T();
        }

        public static void Write<T>(string fileName, T value)
        {
            Directory.CreateDirectory(RootPath);
            var path = GetPath(fileName);
            var temporaryPath = $"{path}.tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(value, JsonOptions));
            File.Move(temporaryPath, path, true);
        }

        private static string GetPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName)
                throw new ArgumentException("A local data file name is required.", nameof(fileName));

            return Path.Combine(RootPath, fileName);
        }
    }
}
