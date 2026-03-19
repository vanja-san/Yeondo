using System.Globalization;
using System.Windows.Data;
using Yeondo.Services;

namespace Yeondo.Converters;

/// <summary>
/// Конвертер для получения строк локализации по ключу
/// </summary>
public class LocConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = parameter as string;
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        var loc = LocalizationService.Instance.Resources;
        var prop = typeof(LocalizationModel).GetProperty(key);
        return prop?.GetValue(loc) as string ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Конвертер для форматирования строк локализации с параметрами
/// </summary>
public class LocFormatConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var key = parameter as string;
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        var loc = LocalizationService.Instance.Resources;
        var prop = typeof(LocalizationModel).GetProperty(key);
        var format = prop?.GetValue(loc) as string;

        if (string.IsNullOrEmpty(format))
            return string.Empty;

        try
        {
            return string.Format(format, values);
        }
        catch
        {
            return format;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
