using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Duplicati.BackupExplorer.Views;
public class BackupListItemBackgroundConverter : Dictionary<string, IDataTemplate>, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(value));

        if (value is null)
        {
            return "LightBlue";
        }
        else
        {
            return "#ADD8D7";

        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
