using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.Views;
public class CompareResultConverter : Dictionary<string, IDataTemplate>, IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(value));

        if (!(bool)value!)
        {
            return this["shared"];
        }
        else
        {
            return this["disjunct"];

        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
