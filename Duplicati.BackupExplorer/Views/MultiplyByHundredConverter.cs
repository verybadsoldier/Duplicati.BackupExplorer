using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.Views
{
    using System;
    using Avalonia.Data.Converters;
    using System.Globalization;
    using Avalonia;

    public class MultiplyByHundredConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is float doubleValue)
            {
                return doubleValue * 100;
            }

            return AvaloniaProperty.UnsetValue;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is float doubleValue)
            {
                return doubleValue / 100;
            }

            return AvaloniaProperty.UnsetValue;
        }
    }

}
