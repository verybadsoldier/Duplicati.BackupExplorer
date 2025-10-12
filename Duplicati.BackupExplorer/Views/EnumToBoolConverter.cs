using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Duplicati.BackupExplorer.Views
{
    public class EnumToBoolConverter : IValueConverter
    {
    /// <summary>
        /// </summary>
        /// <param name="value">The enum property from your ViewModel (e.g., SelectedSortOption).</param>
        /// <param name="parameter">The specific enum value for this MenuItem (e.g., SortMode.Lexical).</param>
        /// <returns>True if the values match, otherwise false.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Check if the ViewModel's value is not null and equals the parameter of this menu item
            return value != null && value.Equals(parameter);
        }

        /// <summary>
        /// </summary>
        /// <param name="value">The IsChecked state from the MenuItem (true or false).</param>
        /// <param name="parameter">The specific enum value for this MenuItem.</param>
        /// <returns>The enum value if the MenuItem was checked (value is true).</returns>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // If the menu item is checked, return its specific enum value to update the ViewModel.
            // Otherwise, do nothing.
            return value is true ? parameter : null;
        }
    }
}
