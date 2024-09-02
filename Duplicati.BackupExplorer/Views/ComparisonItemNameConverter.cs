using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Duplicati.BackupExplorer.Views;
public class ComparisonItemNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(value));

        if (value is null)
        {
            return "Not Selected";
        }
        else
        {
            return value.ToString();
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
