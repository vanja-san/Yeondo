namespace Yeondo.Models;

/// <summary>
/// Настройки приложения
/// </summary>
public class AppSettings
{
    public string? LastTargetFolder { get; set; }

    private static readonly string SettingsPath = System.IO.Path.Combine(
        AppContext.BaseDirectory,
        "settings.json");

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static AppSettings Load()
    {
        try
        {
            if (System.IO.File.Exists(SettingsPath))
            {
                var json = System.IO.File.ReadAllText(SettingsPath);
                return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
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
            var json = System.Text.Json.JsonSerializer.Serialize(this, JsonOptions);
            System.IO.File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Игнорируем ошибки сохранения
        }
    }
}
