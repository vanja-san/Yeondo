using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Yeondo.Models;

namespace Yeondo.Converters;

public class FileNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string path)
            return Path.GetFileName(path);
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            LinkItem.LinkStatus.Pending => "\uE7C8",      // Часы
            LinkItem.LinkStatus.InProgress => "\uE72C",   // Вращение
            LinkItem.LinkStatus.Success => "\uE73E",      // Галочка
            LinkItem.LinkStatus.Error => "\uE711",        // Крестик
            _ => "\uE7C8"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            LinkItem.LinkStatus.Pending => Brushes.Gray,
            LinkItem.LinkStatus.InProgress => Brushes.Orange,
            LinkItem.LinkStatus.Success => Brushes.Green,
            LinkItem.LinkStatus.Error => Brushes.Red,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DetailsVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && str.Contains("[Детали]"))
            return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringHasValueToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return !string.IsNullOrWhiteSpace(str) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SourceIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LinkItem item)
        {
            // Для InProgress возвращаем пустую строку (показываем спиннер)
            if (item.Status == LinkItem.LinkStatus.InProgress)
                return string.Empty;

            // Для Success и Error возвращаем пустую строку (иконка статуса показывается через DataTrigger в XAML)
            if (item.Status == LinkItem.LinkStatus.Success || item.Status == LinkItem.LinkStatus.Error)
                return string.Empty;

            // Для Pending показываем иконку файла/папки
            return item.IsDirectory ? "\uE8B7" : "\uE7C3";
        }
        return "\uE7C3";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatusToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LinkItem.LinkStatus status && parameter is string param)
        {
            if (Enum.TryParse<LinkItem.LinkStatus>(param, out var targetStatus))
            {
                return status == targetStatus ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        if (value != null && int.TryParse(value.ToString(), out var cnt))
        {
            return cnt > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
