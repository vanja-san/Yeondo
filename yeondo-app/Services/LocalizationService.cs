using System.Globalization;
using System.IO;

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
}

/// <summary>
/// Сервис локализации приложения
/// </summary>
public class LocalizationService
{
    private static LocalizationService? _instance;
    private LocalizationModel _resources;
    private string _currentLanguage;
    private readonly string _i18nPath;
    private string _currentLangPath;

    private LocalizationService()
    {
        _resources = new LocalizationModel();
        _currentLanguage = "en"; // Язык по умолчанию

        // Путь к папке i18n рядом с исполняемым файлом
        var baseDir = AppContext.BaseDirectory;
        _i18nPath = Path.Combine(baseDir, "i18n");
        _currentLangPath = Path.Combine(_i18nPath, $"{_currentLanguage}.json");
    }

    public static LocalizationService Instance => _instance ??= new LocalizationService();

    public LocalizationModel Resources => _resources;

    public string CurrentLanguage => _currentLanguage;

    /// <summary>
    /// Инициализация локализации. Загружает JSON файл или создаёт файлы по умолчанию.
    /// </summary>
    public void Initialize()
    {
        // Определяем язык системы
        var culture = CultureInfo.CurrentUICulture;
        _currentLanguage = culture.TwoLetterISOLanguageName == "ru" ? "ru" : "en";
        _currentLangPath = Path.Combine(_i18nPath, $"{_currentLanguage}.json");

        // Создаём папку i18n если нет
        if (!Directory.Exists(_i18nPath))
        {
            Directory.CreateDirectory(_i18nPath);
        }

        // Создаём файлы локализации по умолчанию
        CreateDefaultLocalizationFiles();

        // Загружаем текущий язык
        LoadLocalization(_currentLanguage);
    }

    /// <summary>
    /// Загрузка локализации из JSON файла
    /// </summary>
    private void LoadLocalization(string language)
    {
        var filePath = Path.Combine(_i18nPath, $"{language}.json");

        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                _resources = System.Text.Json.JsonSerializer.Deserialize<LocalizationModel>(json) ?? new LocalizationModel();
            }
            catch
            {
                // При ошибке используем значения по умолчанию
                _resources = new LocalizationModel();
            }
        }
    }

    /// <summary>
    /// Создание файлов локализации по умолчанию
    /// </summary>
    private void CreateDefaultLocalizationFiles()
    {
        // Русский язык
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
                LinkTypeHardLink = "Hard Link"
            };
            SaveLocalization(ruPath, ru);
        }

        // Английский язык
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
                LinkTypeHardLink = "Hard Link"
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
