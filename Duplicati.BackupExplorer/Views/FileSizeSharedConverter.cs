using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Duplicati.BackupExplorer.Views
{


    public class FileSizeSharedConverter : IMultiValueConverter
    {
        object? IMultiValueConverter.Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Any(x => x is UnsetValueType)) return false;

            // Ensure all bindings are provided and attached to correct target type
            if (values.Count != 2 || !targetType.IsAssignableFrom(typeof(string)))
                throw new NotSupportedException();

            var val1 = values[0];
            var val2 = values[1];
            if (val1 == null || val2 == null)
                throw new InvalidOperationException("One of the values is null");

            var part2 = FileSizeConverter.CalculateNumeric((long)val2);
            var part1 = FileSizeConverter.CalculateNumeric((long)val1, part2.Item2);
            return String.Format("{0:0.##}/{1:0.##} {2}", part1.Item1, part2.Item1, part2.Item2);
        }
    }

}
