using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Duplicati.BackupExplorer.Views
{


    public class FileSizeConverter : IValueConverter
    {

        static public Tuple<float, string> CalculateNumeric(long longValue, string? unit = null)
        {
            string[] sizes = ["B", "KB", "MB", "GB", "TB"];
            int order = 0;

            float floatValue = longValue;
            if (unit == null)
            {
                while (floatValue >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    floatValue /= 1024;
                }
            }
            else
            {
                order = Array.FindIndex(sizes, x => x == unit);
                floatValue /= (float)Math.Pow(1024, order);
            }

            return Tuple.Create(floatValue, sizes[order]);
        }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long longValue)
            {
                var result = CalculateNumeric(longValue);
                // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
                // show a single decimal place, and no space.
                return String.Format("{0:0.##} {1}", result.Item1, result.Item2);
            }

            return AvaloniaProperty.UnsetValue;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue;
        }
    }

}
