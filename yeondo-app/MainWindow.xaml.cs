using System.Windows;
using System.Windows.Input;
using DragEventArgs = System.Windows.DragEventArgs;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using Yeondo.Services;

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
