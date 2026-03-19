using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Yeondo.Models;

/// <summary>
/// Элемент для создания символической ссылки
/// </summary>
public class LinkItem : INotifyPropertyChanged
{
    private string _sourcePath = string.Empty;
    private string _linkPath = string.Empty;
    private bool _isDirectory;
    private LinkStatus _status = LinkStatus.Pending;
    private string? _errorMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public string SourcePath
    {
        get => _sourcePath;
        set => SetField(ref _sourcePath, value);
    }

    public string LinkPath
    {
        get => _linkPath;
        set => SetField(ref _linkPath, value);
    }

    public bool IsDirectory
    {
        get => _isDirectory;
        set => SetField(ref _isDirectory, value);
    }

    public LinkStatus Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public bool IsProcessing => Status == LinkStatus.InProgress;

    public enum LinkStatus
    {
        Pending,
        InProgress,
        Success,
        Error
    }
}
