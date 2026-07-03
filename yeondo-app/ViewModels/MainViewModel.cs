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
    private readonly string _logFilePath;

    // Кэшированные команды
    private readonly ICommand _addFilesCommand;
    private readonly ICommand _addFoldersCommand;
    private readonly ICommand _browseTargetCommand;
    private readonly ICommand _createCommand;
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

        // Путь к файлу логов — рядом с исполняемым файлом
        var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
        if (!Directory.Exists(logsDir))
            Directory.CreateDirectory(logsDir);

        _logFilePath = Path.Combine(logsDir, $"symlink_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        // Инициализация команд
        _addFilesCommand = new RelayCommand(AddFiles, () => CanAddItems);
        _addFoldersCommand = new RelayCommand(AddFolders, () => CanAddItems);
        _browseTargetCommand = new RelayCommand(BrowseTarget);
        _createCommand = new AsyncRelayCommand(CreateLinksAsync, () => CanCreate);
        _clearCommand = new RelayCommand(ClearItems, () => !IsBusy && Items.Count > 0);
        _openTargetCommand = new RelayCommand(OpenTarget, () => !string.IsNullOrWhiteSpace(TargetFolder) && Directory.Exists(TargetFolder));
        _openLogsCommand = new RelayCommand(OpenLogs, () => HasErrors && File.Exists(_logFilePath));
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
    public bool CanCreate => !IsBusy && Items.Count > 0 && !string.IsNullOrWhiteSpace(TargetFolder);

    public ICommand AddFilesCommand => _addFilesCommand;
    public ICommand AddFoldersCommand => _addFoldersCommand;
    public ICommand BrowseTargetCommand => _browseTargetCommand;
    public ICommand CreateCommand => _createCommand;
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

    private async Task CreateLinksAsync()
    {
        IsBusy = true;
        _successCount = 0;
        _errorCount = 0;
        HasErrors = false;

        // Создаём директорию для логов
        var logDirectory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
            Directory.CreateDirectory(logDirectory);

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
                IsBusy = false;
                return;
            }
        }

        // Создаём ссылки в фоновом потоке
        await Task.Run(() =>
        {
            var selectedType = _selectedLinkType;

            foreach (var item in items)
            {
                // Обновляем статус в UI-потоке
                App.Current.Dispatcher.Invoke(() => item.Status = LinkItem.LinkStatus.InProgress);

                try
                {
                    var linkName = Path.GetFileName(item.SourcePath);
                    var linkPath = Path.Combine(currentTargetFolder, linkName);

                    (bool result, string? error) = selectedType switch
                    {
                        LinkType.Symbolic => CreateSymbolicLink(item, linkPath),
                        LinkType.Junction => CreateJunctionLink(item, linkPath),
                        LinkType.HardLink => CreateHardLink(item, linkPath),
                        _ => (false, _localization.GetString(LocalizationService.Keys.LinkTypeUnknown))
                    };

                    if (result)
                    {
                        App.Current.Dispatcher.Invoke(() => item.Status = LinkItem.LinkStatus.Success);
                        Interlocked.Increment(ref _successCount);
                        lock (logLines)
                            logLines.Add(string.Format(_localization.GetString(LocalizationService.Keys.LogSuccess), item.SourcePath, linkPath));
                    }
                    else
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            item.Status = LinkItem.LinkStatus.Error;
                            item.ErrorMessage = error;
                        });
                        Interlocked.Increment(ref _errorCount);
                        lock (logLines)
                            logLines.Add(string.Format(_localization.GetString(LocalizationService.Keys.LogError), item.SourcePath, error));
                    }
                }
                catch (Exception ex)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        item.Status = LinkItem.LinkStatus.Error;
                        item.ErrorMessage = ex.Message;
                    });
                    Interlocked.Increment(ref _errorCount);
                    lock (logLines)
                        logLines.Add(string.Format(_localization.GetString(LocalizationService.Keys.LogError), item.SourcePath, ex.Message));
                }
            }
        });

        // Завершение лога
        logLines.Add("");
        logLines.Add(string.Format(_localization.GetString(LocalizationService.Keys.LogSummary), _successCount, _errorCount));

        // Сохраняем лог (в фоне)
        await Task.Run(() =>
        {
            try
            {
                File.WriteAllLines(_logFilePath, logLines);
            }
            catch
            {
                // Игнорируем ошибки записи лога
            }
        });

        UpdateSummaryAfterCreate();
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
        if (File.Exists(_logFilePath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_logFilePath) { UseShellExecute = true });
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
        try
        {
            if (!Directory.Exists(linkPath))
                Directory.CreateDirectory(linkPath);

            return NativeMethods.CreateJunction(linkPath, item.SourcePath);
        }
        catch (Exception ex)
        {
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
