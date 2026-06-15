using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CarCare360.Desktop.Helpers;

/// <summary>
/// Конвертер: True → Collapsed, False → Visible.
/// Используется для скрытия DataGrid во время загрузки (вместо него — SkeletonLoading).
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is true) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => (value is Visibility.Collapsed);
}
