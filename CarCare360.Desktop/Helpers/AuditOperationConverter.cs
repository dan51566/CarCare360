using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CarCare360.Desktop.Helpers;

/// <summary>
/// Конвертирует код операции аудита (I/U/D) в читаемое русское название.
/// </summary>
public class AuditOperationTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "I" => "Добавление",
            "U" => "Изменение",
            "D" => "Удаление",
            var s => s ?? string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Конвертирует код операции аудита (I/U/D) в цветную кисть для бейджа.
/// </summary>
public class AuditOperationColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "I" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
            "U" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
            "D" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),
            _   => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#888888"))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
