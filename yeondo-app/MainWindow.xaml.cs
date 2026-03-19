using System.Windows;
using Yeondo.Services;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;

namespace Yeondo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Установка DataContext после инициализации локализации
        DataContext = new ViewModels.MainViewModel(LocalizationService.Instance);

        // Применение локализации к окну
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        var loc = LocalizationService.Instance.Resources;

        // Заголовок окна
        Title = loc.AppTitle;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var viewModel = (ViewModels.MainViewModel)DataContext;
            viewModel.AddItems(files);
        }
    }
}
