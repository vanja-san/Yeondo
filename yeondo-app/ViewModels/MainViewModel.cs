using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
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
    private string _targetFolder = string.Empty;
    private string _summaryText = string.Empty;
    private string _statusText = string.Empty;
    private bool _isBusy;
    private int _successCount;
    private int _errorCount;
    private bool _hasErrors;
    private LinkType _selectedLinkType = LinkType.Symbolic;
    private readonly string _logFilePath;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(LocalizationService? localization = null)
    {
        _localization = localization ?? LocalizationService.Instance;
        _settings = AppSettings.Load();
        if (!string.IsNullOrEmpty(_settings.LastTargetFolder))
            TargetFolder = _settings.LastTargetFolder;

        // Путь к файлу логов — рядом с исполняемым файлом
        var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
        if (!Directory.Exists(logsDir))
            Directory.CreateDirectory(logsDir);
        
        _logFilePath = Path.Combine(logsDir, $"symlink_{DateTime.Now:yyyyMMdd_HHmmss}.log");
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

    public ICommand AddFilesCommand => new RelayCommand(AddFiles, () => CanAddItems);
    public ICommand AddFoldersCommand => new RelayCommand(AddFolders, () => CanAddItems);
    public ICommand BrowseTargetCommand => new RelayCommand(BrowseTarget);
    public ICommand CreateCommand => new RelayCommand(CreateLinks, () => CanCreate);
    public ICommand ClearCommand => new RelayCommand(ClearItems, () => !IsBusy && Items.Count > 0);
    public ICommand OpenTargetCommand => new RelayCommand(OpenTarget, () => !string.IsNullOrWhiteSpace(TargetFolder) && Directory.Exists(TargetFolder));
    public ICommand OpenLogsCommand => new RelayCommand(OpenLogs, () => HasErrors && File.Exists(_logFilePath));
    public ICommand RemoveItemCommand => new RelayCommand<LinkItem>(RemoveItem, _ => !IsBusy);

    /// <summary>
    /// Проверка, существует ли уже элемент с таким путём
    /// </summary>
    private bool ItemExists(string path)
    {
        return Items.Any(item => string.Equals(item.SourcePath, path, StringComparison.OrdinalIgnoreCase));
    }

    private void AddFiles()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Title = _localization.Resources.SelectFilesTitle
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
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
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = _localization.Resources.SelectFoldersTitle,
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var folder in dialog.FolderNames)
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
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = _localization.Resources.SelectTargetTitle,
            DefaultDirectory = TargetFolder
        };

        if (dialog.ShowDialog() == true)
        {
            TargetFolder = dialog.FolderName;
        }
    }

    private void UpdateSummary()
    {
        SummaryText = Items.Count > 0 ? $"Добавлено элементов: {Items.Count}" : string.Empty;
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

    private async void CreateLinks()
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
        var logLines = new List<string>
        {
            $"=== Создание символических ссылок [{DateTime.Now:dd.MM.yyyy HH:mm:ss}] ===",
            $"Целевая папка: {TargetFolder}",
            $"Элементов: {Items.Count}",
            ""
        };

        // Очищаем предыдущие статусы
        foreach (var item in Items)
        {
            item.Status = LinkItem.LinkStatus.Pending;
            item.ErrorMessage = null;
        }

        // Создаём целевую папку если нет
        if (!Directory.Exists(TargetFolder))
        {
            try
            {
                Directory.CreateDirectory(TargetFolder);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    string.Format(_localization.Resources.CreateTargetFolderError, ex.Message),
                    _localization.Resources.ErrorTitle,
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                IsBusy = false;
                return;
            }
        }

        // Создаём ссылки по очереди
        foreach (var item in Items)
        {
            item.Status = LinkItem.LinkStatus.InProgress;

            await Task.Delay(50);

            try
            {
                var linkName = Path.GetFileName(item.SourcePath);
                var linkPath = Path.Combine(TargetFolder, linkName);

                (bool result, string? error) = SelectedLinkType switch
                {
                    LinkType.Symbolic => CreateSymbolicLink(item, linkPath),
                    LinkType.Junction => CreateJunctionLink(item, linkPath),
                    LinkType.HardLink => CreateHardLink(item, linkPath),
                    _ => (false, "Неизвестный тип ссылки")
                };

                if (result)
                {
                    item.Status = LinkItem.LinkStatus.Success;
                    _successCount++;
                    logLines.Add($"[OK] {item.SourcePath} -> {linkPath}");
                }
                else
                {
                    item.Status = LinkItem.LinkStatus.Error;
                    item.ErrorMessage = error;
                    _errorCount++;
                    logLines.Add($"[ERROR] {item.SourcePath} -> {error}");
                }
            }
            catch (Exception ex)
            {
                item.Status = LinkItem.LinkStatus.Error;
                item.ErrorMessage = ex.Message;
                _errorCount++;
                logLines.Add($"[ERROR] {item.SourcePath} -> {ex.Message}");
            }
        }

        // Завершение лога
        logLines.Add("");
        logLines.Add($"=== Итог: Успешно {_successCount}, Ошибок {_errorCount} ===");

        // Сохраняем лог
        try
        {
            File.WriteAllLines(_logFilePath, logLines);
        }
        catch
        {
            // Игнорируем ошибки записи лога
        }

        UpdateSummaryAfterCreate();
        IsBusy = false;
        CommandManager.InvalidateRequerySuggested();
    }

    private void UpdateSummaryAfterCreate()
    {
        if (_errorCount == 0)
        {
            StatusText = string.Format(_localization.Resources.CreatedCount, _successCount);
            HasErrors = false;
        }
        else
        {
            StatusText = string.Format(_localization.Resources.CreatedCount, _successCount) +
                        string.Format(_localization.Resources.FailedCount, _errorCount);
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
            System.Diagnostics.Process.Start("explorer.exe", TargetFolder);
        }
    }

    private void OpenLogs()
    {
        if (File.Exists(_logFilePath))
        {
            System.Diagnostics.Process.Start("notepad.exe", _logFilePath);
        }
    }

    private static (bool success, string? error) CreateSymbolicLink(LinkItem item, string linkPath)
    {
        int flags = item.IsDirectory
            ? NativeMethods.SYMBOLIC_LINK_FLAG_DIRECTORY | NativeMethods.SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE
            : NativeMethods.SYMBOLIC_LINK_FLAG_FILE | NativeMethods.SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE;

        return NativeMethods.CreateSymbolicLink(linkPath, item.SourcePath, flags);
    }

    private static (bool success, string? error) CreateJunctionLink(LinkItem item, string linkPath)
    {
        if (!item.IsDirectory)
            return (false, "Junction работает только с папками");

        if (!Directory.Exists(item.SourcePath))
            return (false, "Источник должен существовать для Junction");

        int flags = NativeMethods.SYMBOLIC_LINK_FLAG_DIRECTORY | NativeMethods.SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE;
        return NativeMethods.CreateSymbolicLink(linkPath, item.SourcePath, flags);
    }

    private static (bool success, string? error) CreateHardLink(LinkItem item, string linkPath)
    {
        if (item.IsDirectory)
            return (false, "Hard Link работает только с файлами");

        if (!File.Exists(item.SourcePath))
            return (false, "Файл источник не найден");

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

public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    private readonly Action _execute = execute;
    private readonly Func<bool>? _canExecute = canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}

public class RelayCommand<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
{
    private readonly Action<T?> _execute = execute;
    private readonly Func<T?, bool>? _canExecute = canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

    public void Execute(object? parameter) => _execute((T?)parameter);
}

public static partial class NativeMethods
{
    public const int SYMBOLIC_LINK_FLAG_FILE = 0;
    public const int SYMBOLIC_LINK_FLAG_DIRECTORY = 1;
    public const int SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 2;

    [System.Runtime.InteropServices.LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = System.Runtime.InteropServices.StringMarshalling.Utf16, EntryPoint = "CreateSymbolicLinkW")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool CreateSymbolicLinkNative(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

    [System.Runtime.InteropServices.LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = System.Runtime.InteropServices.StringMarshalling.Utf16, EntryPoint = "CreateHardLinkW")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool CreateHardLinkNative(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    public static (bool success, string? error) CreateSymbolicLink(string link, string target, int flags)
    {
        bool result = CreateSymbolicLinkNative(link, target, flags);
        if (!result)
        {
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            return (false, new System.ComponentModel.Win32Exception(error).Message);
        }
        return (true, null);
    }

    public static (bool success, string? error) CreateHardLink(string link, string target)
    {
        bool result = CreateHardLinkNative(link, target, IntPtr.Zero);
        if (!result)
        {
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            return (false, new System.ComponentModel.Win32Exception(error).Message);
        }
        return (true, null);
    }
}
