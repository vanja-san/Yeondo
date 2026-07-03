namespace Yeondo.Services;

/// <summary>
/// Реализация IDialogService для WPF
/// </summary>
public class DialogService : IDialogService
{
  public string[]? ShowOpenFileDialog(string title, bool multiselect)
  {
    var dialog = new Microsoft.Win32.OpenFileDialog
    {
      Multiselect = multiselect,
      Title = title
    };

    if (dialog.ShowDialog() == true)
      return dialog.FileNames;

    return null;
  }

  public string[]? ShowOpenFolderDialog(string title, bool multiselect, string? defaultDirectory = null)
  {
    var dialog = new Microsoft.Win32.OpenFolderDialog
    {
      Title = title,
      Multiselect = multiselect,
      DefaultDirectory = defaultDirectory
    };

    if (dialog.ShowDialog() == true)
      return dialog.FolderNames;

    return null;
  }

  public string? ShowSelectFolderDialog(string title, string? defaultDirectory = null)
  {
    var dialog = new Microsoft.Win32.OpenFolderDialog
    {
      Title = title,
      Multiselect = false,
      DefaultDirectory = defaultDirectory
    };

    if (dialog.ShowDialog() == true)
      return dialog.FolderName;

    return null;
  }

  public void ShowError(string message, string title)
  {
    System.Windows.MessageBox.Show(
        message,
        title,
        System.Windows.MessageBoxButton.OK,
        System.Windows.MessageBoxImage.Error);
  }
}
