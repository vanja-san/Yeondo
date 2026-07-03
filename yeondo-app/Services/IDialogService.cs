namespace Yeondo.Services;

/// <summary>
/// Абстракция для диалоговых окон (MVVM-friendly)
/// </summary>
public interface IDialogService
{
  /// <summary>
  /// Показывает диалог открытия файлов
  /// </summary>
  string[]? ShowOpenFileDialog(string title, bool multiselect);

  /// <summary>
  /// Показывает диалог открытия папок
  /// </summary>
  string[]? ShowOpenFolderDialog(string title, bool multiselect, string? defaultDirectory = null);

  /// <summary>
  /// Показывает диалог выбора одной папки
  /// </summary>
  string? ShowSelectFolderDialog(string title, string? defaultDirectory = null);

  /// <summary>
  /// Показывает MessageBox с ошибкой
  /// </summary>
  void ShowError(string message, string title);
}
