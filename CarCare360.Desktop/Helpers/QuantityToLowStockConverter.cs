using System;
using System.Globalization;
using System.Windows.Data;

namespace CarCare360.Desktop.Helpers;

/// <summary>
/// Возвращает true, если количество товара меньше 5 (порог низкого остатка).
/// </summary>
public class QuantityToLowStockConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int qty) return qty < 5;
        if (value is decimal d)  return d < 5;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
