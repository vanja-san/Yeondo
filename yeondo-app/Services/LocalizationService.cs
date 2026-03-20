using System.Globalization;
using System.IO;
using System.Reflection;

namespace Yeondo.Services;

/// <summary>
/// Модель локализации приложения
/// </summary>
public class LocalizationModel
{
    public string AppTitle { get; set; } = "Yeondo - SymLink Creator";
    public string AddFilesTooltip { get; set; } = "Добавить файлы";
    public string AddFoldersTooltip { get; set; } = "Добавить папки";
    public string CreateButton { get; set; } = "Создать";
    public string OutputPathLabel { get; set; } = "Выходной путь";
    public string SelectPath { get; set; } = "Не выбран";
    public string BrowseButton { get; set; } = "Обзор";
    public string BrowseTooltip { get; set; } = "Выбрать папку";
    public string ClearButton { get; set; } = "Очистить";
    public string LogsButton { get; set; } = "Логи";
    public string ReadyStatus { get; set; } = "Готов к работе";
    public string CreatedCount { get; set; } = "Создано: {0}";
    public string FailedCount { get; set; } = ", Не удалось создать: {0}";
    public string SuccessMessage { get; set; } = "Успешно создано";
    public string RemoveMenuItem { get; set; } = "Удалить из списка";
    public string OpenFolderTooltip { get; set; } = "Нажмите, чтобы открыть папку";
    public string SelectFilesTitle { get; set; } = "Выберите файлы";
    public string SelectFoldersTitle { get; set; } = "Выберите папки";
    public string SelectTargetTitle { get; set; } = "Папка для создания ссылок";
    public string ErrorTitle { get; set; } = "Ошибка";
    public string CreateTargetFolderError { get; set; } = "Не удалось создать целевую папку: {0}";
    public string LinkTypeSymbolic { get; set; } = "Symbolic";
    public string LinkTypeJunction { get; set; } = "Junction";
    public string LinkTypeHardLink { get; set; } = "Hard Link";
    public string LinkTypeUnknown { get; set; } = "Неизвестный тип ссылки";
    public string LogHeader { get; set; } = "=== Создание символических ссылок [{0}] ===";
    public string LogTargetFolder { get; set; } = "Целевая папка: {0}";
    public string LogItemCount { get; set; } = "Элементов: {0}";
    public string LogSuccess { get; set; } = "[OK] {0} -> {1}";
    public string LogError { get; set; } = "[ERROR] {0} -> {1}";
    public string LogSummary { get; set; } = "=== Итог: Успешно {0}, Ошибок {1} ===";
}

/// <summary>
/// Сервис локализации приложения
/// </summary>
public class LocalizationService
{
    private static LocalizationService? _instance;
    private LocalizationModel _resources;
    private LocalizationModel? _fallbackResources;
    private string _currentLanguage;
    private readonly string _i18nPath;
    private readonly List<string> _missingKeys;

    /// <summary>
    /// Константы ключей локализации для предотвращения опечаток
    /// </summary>
    public static class Keys
    {
        public const string AppTitle = nameof(LocalizationModel.AppTitle);
        public const string AddFilesTooltip = nameof(LocalizationModel.AddFilesTooltip);
        public const string AddFoldersTooltip = nameof(LocalizationModel.AddFoldersTooltip);
        public const string CreateButton = nameof(LocalizationModel.CreateButton);
        public const string OutputPathLabel = nameof(LocalizationModel.OutputPathLabel);
        public const string SelectPath = nameof(LocalizationModel.SelectPath);
        public const string BrowseButton = nameof(LocalizationModel.BrowseButton);
        public const string BrowseTooltip = nameof(LocalizationModel.BrowseTooltip);
        public const string ClearButton = nameof(LocalizationModel.ClearButton);
        public const string LogsButton = nameof(LocalizationModel.LogsButton);
        public const string ReadyStatus = nameof(LocalizationModel.ReadyStatus);
        public const string CreatedCount = nameof(LocalizationModel.CreatedCount);
        public const string FailedCount = nameof(LocalizationModel.FailedCount);
        public const string SuccessMessage = nameof(LocalizationModel.SuccessMessage);
        public const string RemoveMenuItem = nameof(LocalizationModel.RemoveMenuItem);
        public const string OpenFolderTooltip = nameof(LocalizationModel.OpenFolderTooltip);
        public const string SelectFilesTitle = nameof(LocalizationModel.SelectFilesTitle);
        public const string SelectFoldersTitle = nameof(LocalizationModel.SelectFoldersTitle);
        public const string SelectTargetTitle = nameof(LocalizationModel.SelectTargetTitle);
        public const string ErrorTitle = nameof(LocalizationModel.ErrorTitle);
        public const string CreateTargetFolderError = nameof(LocalizationModel.CreateTargetFolderError);
        public const string LinkTypeSymbolic = nameof(LocalizationModel.LinkTypeSymbolic);
        public const string LinkTypeJunction = nameof(LocalizationModel.LinkTypeJunction);
        public const string LinkTypeHardLink = nameof(LocalizationModel.LinkTypeHardLink);
        public const string LinkTypeUnknown = nameof(LocalizationModel.LinkTypeUnknown);
        public const string LogHeader = nameof(LocalizationModel.LogHeader);
        public const string LogTargetFolder = nameof(LocalizationModel.LogTargetFolder);
        public const string LogItemCount = nameof(LocalizationModel.LogItemCount);
        public const string LogSuccess = nameof(LocalizationModel.LogSuccess);
        public const string LogError = nameof(LocalizationModel.LogError);
        public const string LogSummary = nameof(LocalizationModel.LogSummary);
    }

    private LocalizationService()
    {
        _resources = new LocalizationModel();
        _fallbackResources = null;
        _currentLanguage = "en";
        _missingKeys = [];

        var baseDir = AppContext.BaseDirectory;
        _i18nPath = Path.Combine(baseDir, "i18n");
    }

    public static LocalizationService Instance => _instance ??= new LocalizationService();

    public LocalizationModel Resources => _resources;

    public string CurrentLanguage => _currentLanguage;

    public IReadOnlyList<string> MissingKeys => _missingKeys.AsReadOnly();

    /// <summary>
    /// Инициализация локализации. Загружает JSON файл или создаёт файлы по умолчанию.
    /// </summary>
    public void Initialize()
    {
        var culture = CultureInfo.CurrentUICulture;
        _currentLanguage = culture.TwoLetterISOLanguageName == "ru" ? "ru" : "en";

        if (!Directory.Exists(_i18nPath))
            Directory.CreateDirectory(_i18nPath);

        CreateDefaultLocalizationFiles();
        LoadLocalization(_currentLanguage);
        ValidateLocalization();
    }

    /// <summary>
    /// Загрузка локализации из JSON файла с fallback на английский
    /// </summary>
    private void LoadLocalization(string language)
    {
        var filePath = Path.Combine(_i18nPath, $"{language}.json");
        _missingKeys.Clear();

        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                _resources = System.Text.Json.JsonSerializer.Deserialize<LocalizationModel>(json) ?? new LocalizationModel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Localization] Error loading {language}.json: {ex.Message}");
                _resources = new LocalizationModel();
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[Localization] File {language}.json not found, using defaults");
            _resources = new LocalizationModel();
        }

        // Загружаем английский как fallback если текущий язык не английский
        if (_currentLanguage != "en")
        {
            var enPath = Path.Combine(_i18nPath, "en.json");
            if (File.Exists(enPath))
            {
                try
                {
                    var json = File.ReadAllText(enPath);
                    _fallbackResources = System.Text.Json.JsonSerializer.Deserialize<LocalizationModel>(json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Localization] Error loading en.json fallback: {ex.Message}");
                    _fallbackResources = null;
                }
            }
        }
        else
        {
            _fallbackResources = null;
        }
    }

    /// <summary>
    /// Валидация локализации — проверка что все ключи присутствуют
    /// </summary>
    private void ValidateLocalization()
    {
        _missingKeys.Clear();
        var defaultModel = new LocalizationModel();
        var properties = typeof(LocalizationModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (prop.PropertyType != typeof(string))
                continue;

            var currentValue = prop.GetValue(_resources)?.ToString();
            var defaultValue = prop.GetValue(defaultModel)?.ToString();

            // Если значение пустое или null, и есть fallback — используем fallback
            if (string.IsNullOrEmpty(currentValue))
            {
                if (_fallbackResources != null)
                {
                    var fallbackValue = prop.GetValue(_fallbackResources)?.ToString();
                    if (!string.IsNullOrEmpty(fallbackValue))
                    {
                        prop.SetValue(_resources, fallbackValue);
                        continue;
                    }
                }

                // Если fallback нет или тоже пустой — используем дефолт
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    prop.SetValue(_resources, defaultValue);
                }

                _missingKeys.Add(prop.Name);
                System.Diagnostics.Debug.WriteLine($"[Localization] Missing or empty key: {prop.Name}");
            }
        }

        if (_missingKeys.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[Localization] Warning: {_missingKeys.Count} keys are missing or empty");
        }
    }

    /// <summary>
    /// Создание файлов локализации по умолчанию
    /// </summary>
    private void CreateDefaultLocalizationFiles()
    {
        var ruPath = Path.Combine(_i18nPath, "ru.json");
        if (!File.Exists(ruPath))
        {
            var ru = new LocalizationModel
            {
                AppTitle = "Yeondo - SymLink Creator",
                AddFilesTooltip = "Добавить файлы",
                AddFoldersTooltip = "Добавить папки",
                CreateButton = "Создать",
                OutputPathLabel = "Выходной путь",
                SelectPath = "Не выбран",
                BrowseButton = "Обзор",
                BrowseTooltip = "Выбрать папку",
                ClearButton = "Очистить",
                LogsButton = "Логи",
                ReadyStatus = "Готов к работе",
                CreatedCount = "Создано: {0}",
                FailedCount = ", Не удалось создать: {0}",
                SuccessMessage = "Успешно создано",
                RemoveMenuItem = "Удалить из списка",
                OpenFolderTooltip = "Нажмите, чтобы открыть папку",
                SelectFilesTitle = "Выберите файлы",
                SelectFoldersTitle = "Выберите папки",
                SelectTargetTitle = "Папка для создания ссылок",
                ErrorTitle = "Ошибка",
                CreateTargetFolderError = "Не удалось создать целевую папку: {0}",
                LinkTypeSymbolic = "Symbolic",
                LinkTypeJunction = "Junction",
                LinkTypeHardLink = "Hard Link",
                LinkTypeUnknown = "Неизвестный тип ссылки",
                LogHeader = "=== Создание символических ссылок [{0}] ===",
                LogTargetFolder = "Целевая папка: {0}",
                LogItemCount = "Элементов: {0}",
                LogSuccess = "[OK] {0} -> {1}",
                LogError = "[ERROR] {0} -> {1}",
                LogSummary = "=== Итог: Успешно {0}, Ошибок {1} ==="
            };
            SaveLocalization(ruPath, ru);
        }

        var enPath = Path.Combine(_i18nPath, "en.json");
        if (!File.Exists(enPath))
        {
            var en = new LocalizationModel
            {
                AppTitle = "Yeondo - SymLink Creator",
                AddFilesTooltip = "Add files",
                AddFoldersTooltip = "Add folders",
                CreateButton = "Create",
                OutputPathLabel = "Output path",
                SelectPath = "Not selected",
                BrowseButton = "Browse",
                BrowseTooltip = "Select folder",
                ClearButton = "Clear",
                LogsButton = "Logs",
                ReadyStatus = "Ready",
                CreatedCount = "Created: {0}",
                FailedCount = ", Failed: {0}",
                SuccessMessage = "Successfully created",
                RemoveMenuItem = "Remove from list",
                OpenFolderTooltip = "Click to open folder",
                SelectFilesTitle = "Select files",
                SelectFoldersTitle = "Select folders",
                SelectTargetTitle = "Folder for creating links",
                ErrorTitle = "Error",
                CreateTargetFolderError = "Failed to create target folder: {0}",
                LinkTypeSymbolic = "Symbolic",
                LinkTypeJunction = "Junction",
                LinkTypeHardLink = "Hard Link",
                LinkTypeUnknown = "Unknown link type",
                LogHeader = "=== Symbolic Links Creation [{0}] ===",
                LogTargetFolder = "Target folder: {0}",
                LogItemCount = "Items: {0}",
                LogSuccess = "[OK] {0} -> {1}",
                LogError = "[ERROR] {0} -> {1}",
                LogSummary = "=== Summary: Success {0}, Failed {1} ==="
            };
            SaveLocalization(enPath, en);
        }
    }

    /// <summary>
    /// Сохранение локализации в JSON файл
    /// </summary>
    private void SaveLocalization(string path, LocalizationModel model)
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var json = System.Text.Json.JsonSerializer.Serialize(model, options);
        File.WriteAllText(path, json, System.Text.Encoding.UTF8);
    }

    /// <summary>
    /// Получение строки локализации по ключу с fallback
    /// </summary>
    public string GetString(string key, string? fallback = null)
    {
        var prop = typeof(LocalizationModel).GetProperty(key);
        if (prop != null && prop.PropertyType == typeof(string))
        {
            var value = prop.GetValue(_resources)?.ToString();
            if (!string.IsNullOrEmpty(value))
                return value!;
        }

        // Fallback на английский
        if (_fallbackResources != null)
        {
            var propFallback = typeof(LocalizationModel).GetProperty(key);
            if (propFallback != null && propFallback.PropertyType == typeof(string))
            {
                var value = propFallback.GetValue(_fallbackResources)?.ToString();
                if (!string.IsNullOrEmpty(value))
                    return value!;
            }
        }

        return fallback ?? $"[{key}]";
    }

    /// <summary>
    /// Обновление свойства в текущем объекте локализации
    /// </summary>
    public void UpdateResource(string key, string value)
    {
        var prop = typeof(LocalizationModel).GetProperty(key);
        if (prop != null && prop.PropertyType == typeof(string))
        {
            prop.SetValue(_resources, value);
        }
    }
}
