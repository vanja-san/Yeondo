using System.IO;
using System.Text.Json;

namespace Yeondo.Models;

/// <summary>
/// Настройки приложения
/// </summary>
public class AppSettings
{
    public string? LastTargetFolder { get; set; }
    public bool CreateForFiles { get; set; } = true;
    public bool CreateForDirectories { get; set; } = true;

    private static readonly string SettingsPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "yeondo-app",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (System.IO.File.Exists(SettingsPath))
            {
                var json = System.IO.File.ReadAllText(SettingsPath);
                return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Игнорируем ошибки загрузки
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var directory = System.IO.Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            var json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Игнорируем ошибки сохранения
        }
    }
}
