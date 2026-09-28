using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Yeondo.Commands;
using Yeondo.Models;
using Yeondo.Services;

namespace Yeondo.ViewModels;

public enum LinkType
{
    Symbolic,
    Junction,
    HardLink
}

public class MainViewModel : INotifyPropertyChanged
{
    private readonly AppSettings _settings;
    private readonly LocalizationService _localization;
    private readonly IDialogService _dialogService;
    private string _targetFolder = string.Empty;
    private string _summaryText = string.Empty;
    private string _statusText = string.Empty;
    private bool _isBusy;
    private int _successCount;
    private int _errorCount;
    private bool _hasErrors;
    private LinkType _selectedLinkType = LinkType.Symbolic;

    // null, если папка рядом с исполняемым файлом недоступна для записи — логи просто отключаются
    private readonly string? _logFilePath;

    // Отмена текущего прогона
    private CancellationTokenSource? _cancellation;

    // Кэшированные команды
    private readonly ICommand _addFilesCommand;
    private readonly ICommand _addFoldersCommand;
    private readonly ICommand _browseTargetCommand;
    private readonly ICommand _createCommand;
    private readonly ICommand _cancelCommand;
    private readonly ICommand _clearCommand;
    private readonly ICommand _openTargetCommand;
    private readonly ICommand _openLogsCommand;
    private readonly ICommand _removeItemCommand;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(LocalizationService? localization = null, IDialogService? dialogService = null)
    {
        _localization = localization ?? LocalizationService.Instance;
        _dialogService = dialogService ?? new DialogService();
        _settings = AppSettings.Load();
        if (!string.IsNullOrEmpty(_settings.LastTargetFolder))
            TargetFolder = _settings.LastTargetFolder;

        // Путь к файлу логов — рядом с исполняемым файлом, если туда можно писать
        _logFilePath = TryCreateLogFilePath();

        // Инициализация команд
        _addFilesCommand = new RelayCommand(AddFiles, () => CanAddItems);
        _addFoldersCommand = new RelayCommand(AddFolders, () => CanAddItems);
        _browseTargetCommand = new RelayCommand(BrowseTarget);
        _createCommand = new AsyncRelayCommand(CreateLinksAsync, () => CanCreate);
        _cancelCommand = new RelayCommand(Cancel, () => IsBusy);
        _clearCommand = new RelayCommand(ClearItems, () => !IsBusy && Items.Count > 0);
        _openTargetCommand = new RelayCommand(OpenTarget, () => !string.IsNullOrWhiteSpace(TargetFolder) && Directory.Exists(TargetFolder));
        _openLogsCommand = new RelayCommand(OpenLogs, () => HasErrors && _logFilePath is not null && File.Exists(_logFilePath));
        _removeItemCommand = new RelayCommand<LinkItem>(RemoveItem, _ => !IsBusy);
    }

    public ObservableCollection<LinkItem> Items { get; } = [];

    public string TargetFolder
    {
        get => _targetFolder;
        set
        {
            if (SetField(ref _targetFolder, value))
            {
                _settings.LastTargetFolder = value;
                _settings.Save();
            }
        }
    }

    public string SummaryText
    {
        get => _summaryText;
        set => SetField(ref _summaryText, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetField(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public bool HasErrors
    {
        get => _hasErrors;
        set => SetField(ref _hasErrors, value);
    }

    public LinkType SelectedLinkType
    {
        get => _selectedLinkType;
        set => SetField(ref _selectedLinkType, value);
    }

    public bool CanAddItems => !IsBusy;

    public bool IsCancelling => _cancellation is { IsCancellationRequested: true };
    public bool CanCreate => !IsBusy && Items.Count > 0 && !string.IsNullOrWhiteSpace(TargetFolder);

    public ICommand AddFilesCommand => _addFilesCommand;
    public ICommand AddFoldersCommand => _addFoldersCommand;
    public ICommand BrowseTargetCommand => _browseTargetCommand;
    public ICommand CreateCommand => _createCommand;
    public ICommand CancelCommand => _cancelCommand;
    public ICommand ClearCommand => _clearCommand;
    public ICommand OpenTargetCommand => _openTargetCommand;
    public ICommand OpenLogsCommand => _openLogsCommand;
    public ICommand RemoveItemCommand => _removeItemCommand;

    /// <summary>
    /// Проверка, существует ли уже элемент с таким путём
    /// </summary>
    private bool ItemExists(string path)
    {
        return Items.Any(item => string.Equals(item.SourcePath, path, StringComparison.OrdinalIgnoreCase));
    }

    private void AddFiles()
    {
        var files = _dialogService.ShowOpenFileDialog(
            _localization.GetString(LocalizationService.Keys.SelectFilesTitle), true);

        if (files != null)
        {
            foreach (var file in files)
            {
                if (!ItemExists(file))
                {
                    Items.Add(new LinkItem
                    {
                        SourcePath = file,
                        IsDirectory = false
                    });
                }
            }
            UpdateSummary();
        }
    }

    private void AddFolders()
    {
        var folders = _dialogService.ShowOpenFolderDialog(
            _localization.GetString(LocalizationService.Keys.SelectFoldersTitle), true);

        if (folders != null)
        {
            foreach (var folder in folders)
            {
                if (!ItemExists(folder))
                {
                    Items.Add(new LinkItem
                    {
                        SourcePath = folder,
                        IsDirectory = true
                    });
                }
            }
            UpdateSummary();
        }
    }

    private void BrowseTarget()
    {
        var folder = _dialogService.ShowSelectFolderDialog(
            _localization.GetString(LocalizationService.Keys.SelectTargetTitle),
            TargetFolder);

        if (folder != null)
        {
            TargetFolder = folder;
        }
    }

    private void UpdateSummary()
    {
        SummaryText = Items.Count > 0
            ? string.Format(_localization.GetString(LocalizationService.Keys.ItemsAdded), Items.Count)
            : string.Empty;
    }

    public void AddItems(IEnumerable<string> paths)
    {
        // Пока идёт создание ссылок, список заморожен снимком в CreateLinksAsync:
        // добавленные здесь элементы не попали бы в текущий прогон.
        if (IsBusy)
            return;

        foreach (var path in paths)
        {
            if (!ItemExists(path))
            {
                Items.Add(new LinkItem
                {
                    SourcePath = path,
                    IsDirectory = Directory.Exists(path)
                });
            }
        }
        UpdateSummary();
    }

    private void Cancel()
    {
        if (_cancellation is not null && !_cancellation.IsCancellationRequested)
        {
            _cancellation.Cancel();
            OnPropertyChanged(nameof(IsCancelling));
        }
    }

    private async Task CreateLinksAsync()
    {
        IsBusy = true;
        _successCount = 0;
        _errorCount = 0;
        HasErrors = false;
        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();
        OnPropertyChanged(nameof(IsCancelling));
        var token = _cancellation.Token;

        // Создаём директорию для логов
        var logDirectory = _logFilePath is not null ? Path.GetDirectoryName(_logFilePath) : null;
        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
        {
            try
            {
                Directory.CreateDirectory(logDirectory);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                // Логи недоступны, продолжаем работу без них
            }
        }

        // Начинаем лог
        var currentTargetFolder = TargetFolder; // копируем для фонового потока
        var items = Items.ToList(); // копируем список
        var logLines = new List<string>
        {
            string.Format(_localization.GetString(LocalizationService.Keys.LogHeader), DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")),
            string.Format(_localization.GetString(LocalizationService.Keys.LogTargetFolder), currentTargetFolder),
            string.Format(_localization.GetString(LocalizationService.Keys.LogItemCount), items.Count),
            ""
        };

        // Очищаем предыдущие статусы
        foreach (var item in items)
        {
            item.Status = LinkItem.LinkStatus.Pending;
            item.ErrorMessage = null;
        }

        // Создаём целевую папку если нет (в UI-потоке для MessageBox)
        if (!Directory.Exists(currentTargetFolder))
        {
            try
            {
                Directory.CreateDirectory(currentTargetFolder);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(
                    string.Format(_localization.GetString(LocalizationService.Keys.CreateTargetFolderError), ex.Message),
                    _localization.GetString(LocalizationService.Keys.ErrorTitle));
                _cancellation?.Dispose();
                _cancellation = null;
                IsBusy = false;
                return;
            }
        }

        // Создаём ссылки в фоновом потоке
        var cancelled = false;
        await Task.Run(() =>
        {
            var selectedType = _selectedLinkType;

            // LinkItem - обычный INotifyPropertyChanged без потоковой привязки, поэтому
            // свойства меняются прямо из фонового потока: WPF сам переносит обновление
            // биндинга в UI-поток. Обращаться к App.Current.Dispatcher.Invoke здесь
            // не нужно - это блокирующий переход между потоками на каждый элемент
            // (замерено 322 мс против 1,0 мс на 5000 элементов, ~314x).
            var usedLinkPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                if (token.IsCancellationRequested)
                {
                    cancelled = true;
                    break;
                }

                item.Status = LinkItem.LinkStatus.InProgress;

                try
                {
                    var linkName = GetLinkName(item.SourcePath);
                    if (linkName is null)
                    {
                        item.Status = LinkItem.LinkStatus.Error;
                        item.ErrorMessage = _localization.GetString(LocalizationService.Keys.LinkNameUnavailable);
                        Interlocked.Increment(ref _errorCount);
                        lock (logLines)
                            logLines.Add(string.Format(_localization.GetString(LocalizationService.Keys.LogError), item.SourcePath, item.ErrorMessage));
                        continue;
                    }

                    var linkPath = Path.Combine(currentTargetFolder, linkName);

                    // Защита: linkPath не должен совпадать с целевой папкой. Раньше имя
                    // бралось из Path.GetFileName, который для пути с завершающим
                    // разделителем (например "C:\dir\folder\") возвращает пустую строку,
                    // из-за чего linkPath схлопывался в сам целевой каталог, а Junction
                    // превращал выбранную папку назначения в reparse point.
                    if (PathsEqual(linkPath, currentTargetFolder))
                    {
                        item.Status = LinkItem.LinkStatus.Error;
                        item.ErrorMessage = _localization.GetString(LocalizationService.Keys.LinkNameUnavailable);
                        Interlocked.Increment(ref _errorCount);
                        lock (logLines)
                            logLines.Add(string.Format(_localization.GetString(LocalizationService.Keys.LogError), item.SourcePath, item.ErrorMessage));
                        continue;
                    }

                    // Два источника с одинаковым именем дали бы один и тот же linkPath:
                    // второй перезаписал бы результат первого. Сообщаем об этом явно.
                    if (!usedLinkPaths.Add(linkPath))
                    {
                        item.Status = LinkItem.LinkStatus.Error;
                        item.ErrorMessage = _localization.GetString(LocalizationService.Keys.LinkNameConflict);
                        Interlocked.Increment(ref _errorCount);
                        lock (logLines)
                            logLines.Add(string.Format(_localization.GetString(LocalizationService.Keys.LogError), item.SourcePath, item.ErrorMessage));
                        continue;
                    }

                    (bool result, string? error) = selectedType switch
                    {
                        LinkType.Symbolic => CreateSymbolicLink(item, linkPath),
                        LinkType.Junction => CreateJunctionLink(item, linkPath),
                        LinkType.HardLink => CreateHardLink(item, linkPath),
                        _ => (false, _localization.GetString(LocalizationService.Keys.LinkTypeUnknown))
                    };

                    if (result)
                    {
                        item.Status = LinkItem.LinkStatus.Success;
                        Interlocked.Increment(ref _successCount);
                        lock (logLines)
                            logLines.Add(string.Format(_localization.GetString(LocalizationService.Keys.LogSuccess), item.SourcePath, linkPath));
                    }
                    else
                    {
                        item.Status = LinkItem.LinkStatus.Error;
                        item.ErrorMessage = error;
                        Interlocked.Increment(ref _errorCount);
                        lock (logLines)
                            logLines.Add(string.Format(_localization.GetString(LocalizationService.Keys.LogError), item.SourcePath, error));
                    }
                }
                catch (Exception ex)
                {
                    item.Status = LinkItem.LinkStatus.Error;
                    item.ErrorMessage = ex.Message;
                    Interlocked.Increment(ref _errorCount);
                    lock (logLines)
                        logLines.Add(string.Format(_localization.GetString(LocalizationService.Keys.LogError), item.SourcePath, ex.Message));
                }
            }
        });

        // Завершение лога
        logLines.Add("");
        if (cancelled)
            logLines.Add(_localization.GetString(LocalizationService.Keys.LogCancelled));
        logLines.Add(string.Format(_localization.GetString(LocalizationService.Keys.LogSummary), _successCount, _errorCount));

        // Сохраняем лог (в фоне)
        var logFilePath = _logFilePath;
        if (logFilePath is not null)
        {
            await Task.Run(() =>
            {
                try
                {
                    File.WriteAllLines(logFilePath, logLines);
                }
                catch
                {
                    // Игнорируем ошибки записи лога
                }
            });
        }

        UpdateSummaryAfterCreate();
        if (cancelled)
            StatusText = _localization.GetString(LocalizationService.Keys.StatusCancelled) + ": " + StatusText;
        if (_errorCount > 0)
            ReportFailureReasons(items);

        _cancellation?.Dispose();
        _cancellation = null;
        OnPropertyChanged(nameof(IsCancelling));
        IsBusy = false;
        CommandManager.InvalidateRequerySuggested();
    }

    private void UpdateSummaryAfterCreate()
    {
        if (_errorCount == 0)
        {
            StatusText = string.Format(_localization.GetString(LocalizationService.Keys.CreatedCount), _successCount);
            HasErrors = false;
        }
        else
        {
            StatusText = string.Format(_localization.GetString(LocalizationService.Keys.CreatedCount), _successCount) +
                        string.Format(_localization.GetString(LocalizationService.Keys.FailedCount), _errorCount);
            HasErrors = true;
        }
        SummaryText = string.Empty;
    }

    /// <summary>
    /// Показывает причины неудач сгруппированными по тексту: одинаковые Win32-ошибки
    /// не должны превращать итог в тысячу одинаковых строк.
    /// </summary>
    private void ReportFailureReasons(IReadOnlyList<LinkItem> items)
    {
        var groups = items
            .Where(i => i.Status == LinkItem.LinkStatus.Error)
            .Select(i => i.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .GroupBy(m => m!.Trim(), StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Take(5)
            .Select(g => string.Format(
                _localization.GetString(LocalizationService.Keys.FailureReasonLine),
                g.Count(),
                g.Key))
            .ToList();

        if (groups.Count == 0)
            return;

        var message = string.Format(
            _localization.GetString(LocalizationService.Keys.FailureSummaryTitle),
            _successCount,
            _errorCount)
            + Environment.NewLine
            + string.Join(Environment.NewLine, groups)
            + Environment.NewLine
            + _localization.GetString(LocalizationService.Keys.FailureSummaryHint);

        _dialogService.ShowError(
            message,
            _localization.GetString(LocalizationService.Keys.ErrorTitle));
    }

    private void ClearItems()
    {
        Items.Clear();
        SummaryText = string.Empty;
        StatusText = string.Empty;
        HasErrors = false;
        _successCount = 0;
        _errorCount = 0;
    }

    private void RemoveItem(LinkItem? item)
    {
        if (item != null && Items.Contains(item))
        {
            Items.Remove(item);
            UpdateSummary();
        }
    }

    private void OpenTarget()
    {
        if (Directory.Exists(TargetFolder))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(TargetFolder) { UseShellExecute = true });
        }
    }

    private void OpenLogs()
    {
        if (_logFilePath is not null && File.Exists(_logFilePath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_logFilePath) { UseShellExecute = true });
        }
    }

    /// <summary>
    /// Создаёт папку логов рядом с исполняемым файлом и возвращает путь файла лога.
    /// Возвращает null, если запись невозможна — приложение продолжает работу без логов.
    /// </summary>
    private static string? TryCreateLogFilePath()
    {
        try
        {
            var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
            if (!Directory.Exists(logsDir))
                Directory.CreateDirectory(logsDir);

            return Path.Combine(logsDir, $"symlink_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            System.Diagnostics.Debug.WriteLine($"[Logs] log directory is not writable, logging disabled: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Возвращает имя будущей ссылки для исходного пути или null, если имя получить
    /// невозможно (например, путь указывает на корень диска: "C:\" или "\\server\").
    /// </summary>
    internal static string? GetLinkName(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            return null;

        // Завершающие разделители убираем, иначе Path.GetFileName вернёт пустую строку.
        var trimmed = sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (trimmed.Length == 0)
            return null;

        var name = Path.GetFileName(trimmed);
        if (!string.IsNullOrEmpty(name))
            return name;

        // У корня диска или сетевой шары нет компонента имени.
        return null;
    }

    internal static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(left)),
                Path.TrimEndingDirectorySeparator(Path.GetFullPath(right)),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static (bool success, string? error) CreateSymbolicLink(LinkItem item, string linkPath)
    {
        int flags = item.IsDirectory
            ? NativeMethods.SYMBOLIC_LINK_FLAG_DIRECTORY | NativeMethods.SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE
            : NativeMethods.SYMBOLIC_LINK_FLAG_FILE | NativeMethods.SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE;

        return NativeMethods.CreateSymbolicLink(linkPath, item.SourcePath, flags);
    }

    private (bool success, string? error) CreateJunctionLink(LinkItem item, string linkPath)
    {
        if (!item.IsDirectory)
            return (false, _localization.GetString(LocalizationService.Keys.JunctionFolderOnly));

        if (!Directory.Exists(item.SourcePath))
            return (false, _localization.GetString(LocalizationService.Keys.JunctionSourceRequired));

        // Для Junction нужно сначала создать пустую директорию, затем установить reparse point
        var createdLinkDirectory = false;
        try
        {
            if (!Directory.Exists(linkPath))
            {
                Directory.CreateDirectory(linkPath);
                createdLinkDirectory = true;
            }

            var (success, error) = NativeMethods.CreateJunction(linkPath, item.SourcePath);
            if (!success && createdLinkDirectory)
            {
                // Не оставляем пустую папку, если reparse point установить не удалось
                try
                {
                    Directory.Delete(linkPath);
                }
                catch (Exception cleanupEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Link] failed to remove empty junction directory '{linkPath}': {cleanupEx.Message}");
                }
            }

            return (success, error);
        }
        catch (Exception ex)
        {
            if (createdLinkDirectory)
            {
                try
                {
                    Directory.Delete(linkPath);
                }
                catch (Exception cleanupEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Link] failed to remove empty junction directory '{linkPath}': {cleanupEx.Message}");
                }
            }

            return (false, ex.Message);
        }
    }

    private (bool success, string? error) CreateHardLink(LinkItem item, string linkPath)
    {
        if (item.IsDirectory)
            return (false, _localization.GetString(LocalizationService.Keys.HardLinkFilesOnly));

        if (!File.Exists(item.SourcePath))
            return (false, _localization.GetString(LocalizationService.Keys.HardLinkSourceNotFound));

        return NativeMethods.CreateHardLink(linkPath, item.SourcePath);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
