using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.UI.Views
{
    using System;
    using Avalonia.Data.Converters;
    using System.Globalization;
    using Avalonia;
    using Duplicati.BackupExplorer.LocalDatabaseAccess.Model;
    using System.IO;

    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long longValue)
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                while (longValue >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    longValue = longValue / 1024;
                }

                // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
                // show a single decimal place, and no space.
                return String.Format("{0:0.##} {1}", longValue, sizes[order]);
            }

            return AvaloniaProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue;
        }
    }

}
