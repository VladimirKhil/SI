using SImulator.ViewModel.Model;
using System.Diagnostics;
using System.Text.Json;

namespace SImulator.Implementation;

/// <summary>
/// Loads and saves user settings (same format and location as the Windows version).
/// </summary>
internal static class SettingsStorage
{
    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Khil-soft",
        "SImulator",
        "Settings");

    private static string SettingsFile => Path.Combine(SettingsFolder, "user.config");

    internal static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                using var stream = File.OpenRead(SettingsFile);
                return JsonSerializer.Deserialize<AppSettings>(stream) ?? new AppSettings();
            }
        }
        catch (Exception exc)
        {
            Trace.TraceError(exc.ToString());
        }

        return new AppSettings();
    }

    /// <summary>
    /// Saves settings.
    /// </summary>
    /// <returns>Error message or null on success.</returns>
    internal static string? Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);
            using var stream = File.Create(SettingsFile);
            JsonSerializer.Serialize(stream, settings);
            return null;
        }
        catch (Exception exc)
        {
            return exc.Message;
        }
    }
}
