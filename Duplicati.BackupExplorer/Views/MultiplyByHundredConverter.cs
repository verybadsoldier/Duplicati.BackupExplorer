using Avalonia;
using Avalonia.Data.Converters;
using System;
using System.Globalization;


namespace Duplicati.BackupExplorer.Views
{

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
