using System.Globalization;
using System.IO;
using System.Reflection;

namespace Yeondo.Services;

/// <summary>
/// Модель локализации приложения. Встроенные значения — английские: это язык по умолчанию
/// и последний запасной вариант при отсутствии или повреждении файла перевода.
/// </summary>
public class LocalizationModel
{
    public string AppTitle { get; set; } = "Yeondo - SymLink Creator";
    public string AddFilesTooltip { get; set; } = "Add files";
    public string AddFoldersTooltip { get; set; } = "Add folders";
    public string CreateButton { get; set; } = "Create";
    public string OutputPathLabel { get; set; } = "Output path";
    public string SelectPath { get; set; } = "Not selected";
    public string BrowseButton { get; set; } = "Browse";
    public string BrowseTooltip { get; set; } = "Select folder";
    public string ClearButton { get; set; } = "Clear";
    public string LogsButton { get; set; } = "Logs";
    public string ReadyStatus { get; set; } = "Ready";
    public string CreatedCount { get; set; } = "Created: {0}";
    public string FailedCount { get; set; } = ", Failed: {0}";
    public string SuccessMessage { get; set; } = "Successfully created";
    public string RemoveMenuItem { get; set; } = "Remove from list";
    public string OpenFolderTooltip { get; set; } = "Click to open folder";
    public string SelectFilesTitle { get; set; } = "Select files";
    public string SelectFoldersTitle { get; set; } = "Select folders";
    public string SelectTargetTitle { get; set; } = "Folder for creating links";
    public string ErrorTitle { get; set; } = "Error";
    public string CreateTargetFolderError { get; set; } = "Failed to create target folder: {0}";
    public string LinkTypeSymbolic { get; set; } = "Symbolic";
    public string LinkTypeJunction { get; set; } = "Junction";
    public string LinkTypeHardLink { get; set; } = "Hard Link";
    public string LinkTypeUnknown { get; set; } = "Unknown link type";
    public string LinkNameUnavailable { get; set; } = "Cannot derive a link name from this path (drive or share root)";
    public string LinkNameConflict { get; set; } = "Another selected item already has this link name";
    public string FailureSummaryTitle { get; set; } = "Created: {0}, Failed: {1}";
    public string FailureReasonLine { get; set; } = "- {0}x: {1}";
    public string FailureSummaryHint { get; set; } = "Full details are in the log; see the README Troubleshooting section.";
    public string LogCancelled { get; set; } = "=== Cancelled by user ===";
    public string StatusCancelled { get; set; } = "Cancelled";
    public string CancelButton { get; set; } = "Cancel";
    public string LogHeader { get; set; } = "=== Symbolic Links Creation [{0}] ===";
    public string LogTargetFolder { get; set; } = "Target folder: {0}";
    public string LogItemCount { get; set; } = "Items: {0}";
    public string LogSuccess { get; set; } = "[OK] {0} -> {1}";
    public string LogError { get; set; } = "[ERROR] {0} -> {1}";
    public string LogSummary { get; set; } = "=== Summary: Success {0}, Failed {1} ===";
    public string JunctionFolderOnly { get; set; } = "Junction works only with folders";
    public string JunctionSourceRequired { get; set; } = "Source must exist for Junction";
    public string HardLinkFilesOnly { get; set; } = "Hard Link works only with files";
    public string HardLinkSourceNotFound { get; set; } = "Source file not found";
    public string ItemsAdded { get; set; } = "Items added: {0}";
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

    // Общие опции для чтения и записи переводов. PropertyNameCaseInsensitive нужен, чтобы
    // пользовательский JSON с ключами в другом регистре не приводил к молчаливому фолбэку.
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

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
        public const string LinkNameUnavailable = nameof(LocalizationModel.LinkNameUnavailable);
        public const string LinkNameConflict = nameof(LocalizationModel.LinkNameConflict);
        public const string FailureSummaryTitle = nameof(LocalizationModel.FailureSummaryTitle);
        public const string FailureReasonLine = nameof(LocalizationModel.FailureReasonLine);
        public const string FailureSummaryHint = nameof(LocalizationModel.FailureSummaryHint);
        public const string LogCancelled = nameof(LocalizationModel.LogCancelled);
        public const string StatusCancelled = nameof(LocalizationModel.StatusCancelled);
        public const string CancelButton = nameof(LocalizationModel.CancelButton);
        public const string LogHeader = nameof(LocalizationModel.LogHeader);
        public const string LogTargetFolder = nameof(LocalizationModel.LogTargetFolder);
        public const string LogItemCount = nameof(LocalizationModel.LogItemCount);
        public const string LogSuccess = nameof(LocalizationModel.LogSuccess);
        public const string LogError = nameof(LocalizationModel.LogError);
        public const string LogSummary = nameof(LocalizationModel.LogSummary);
        public const string JunctionFolderOnly = nameof(LocalizationModel.JunctionFolderOnly);
        public const string JunctionSourceRequired = nameof(LocalizationModel.JunctionSourceRequired);
        public const string HardLinkFilesOnly = nameof(LocalizationModel.HardLinkFilesOnly);
        public const string HardLinkSourceNotFound = nameof(LocalizationModel.HardLinkSourceNotFound);
        public const string ItemsAdded = nameof(LocalizationModel.ItemsAdded);
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

        TryPrepareLocalizationFiles();

        LoadLocalization(_currentLanguage);
        ValidateLocalization();
    }

    /// <summary>
    /// Подготовка файлов локализации рядом с исполняемым файлом.
    /// Папка может быть недоступна для записи (read-only носитель, «Контролируемый доступ к папкам»,
    /// антивирус), поэтому недоступность не считается критичной: приложение продолжает работу
    /// со встроенными значениями LocalizationModel.
    /// </summary>
    private void TryPrepareLocalizationFiles()
    {
        try
        {
            if (!Directory.Exists(_i18nPath))
                Directory.CreateDirectory(_i18nPath);

            CreateDefaultLocalizationFiles();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            System.Diagnostics.Debug.WriteLine($"[Localization] i18n directory is not writable, using built-in defaults: {ex.Message}");
        }
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
                _resources = System.Text.Json.JsonSerializer.Deserialize<LocalizationModel>(json, JsonOptions) ?? new LocalizationModel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Localization] Error loading {language}.json: {ex.Message}, falling back to built-in English");
                _resources = new LocalizationModel();
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[Localization] File {language}.json not found, using built-in English");
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
                    _fallbackResources = System.Text.Json.JsonSerializer.Deserialize<LocalizationModel>(json, JsonOptions);
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
        // Английские значения по умолчанию — последний фолбэк: если ключ отсутствует или пуст
        // в переводе, подставляется английский текст, а не пустая строка.
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
            SaveLocalization(ruPath, CreateRussian());

        var enPath = Path.Combine(_i18nPath, "en.json");
        if (!File.Exists(enPath))
            SaveLocalization(enPath, new LocalizationModel());
    }

    /// <summary>
    /// Русский перевод, создаваемый при первом запуске, если файла i18n\ru.json ещё нет.
    /// Файлы локализации поставляются вместе с приложением; этот метод нужен только для
    /// случая, когда пользователь удалил их или запустил старую сборку без них.
    /// </summary>
    private static LocalizationModel CreateRussian()
    {
        return new LocalizationModel
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
            LinkNameUnavailable = "Невозможно получить имя ссылки из этого пути (корень диска или шары)",
            LinkNameConflict = "Такое имя ссылки уже используется другим выбранным элементом",
            FailureSummaryTitle = "Создано: {0}, с ошибкой: {1}",
            FailureReasonLine = "- {0} шт.: {1}",
            FailureSummaryHint = "Подробности в логе; см. раздел Troubleshooting в README.",
            LogCancelled = "=== Отменено пользователем ===",
            StatusCancelled = "Отменено",
            CancelButton = "Отмена",
            LogHeader = "=== Создание символических ссылок [{0}] ===",
            LogTargetFolder = "Целевая папка: {0}",
            LogItemCount = "Элементов: {0}",
            LogSuccess = "[OK] {0} -> {1}",
            LogError = "[ERROR] {0} -> {1}",
            LogSummary = "=== Итог: Успешно {0}, Ошибок {1} ===",
            JunctionFolderOnly = "Junction работает только с папками",
            JunctionSourceRequired = "Источник должен существовать для Junction",
            HardLinkFilesOnly = "Hard Link работает только с файлами",
            HardLinkSourceNotFound = "Файл источник не найден",
            ItemsAdded = "Добавлено элементов: {0}"
        };
    }

    /// <summary>
    /// Сохранение локализации в JSON файл
    /// </summary>
    private void SaveLocalization(string path, LocalizationModel model)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(model, JsonOptions);
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
}
