using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Globalization;

namespace Duplicati.BackupExplorer.Views
{
    public class FileTreeIconTypeConverter : IValueConverter
    {
        // 1. Cache the loaded images in static fields for performance.
        private static readonly Bitmap? FileIcon = LoadBitmapFromAssets("avares://Duplicati.BackupExplorer/Assets/mnauliady_book.png");
        private static readonly Bitmap? FolderIcon = LoadBitmapFromAssets("avares://Duplicati.BackupExplorer/Assets/mnauliady_folder.png");

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 2. Return the appropriate pre-loaded Bitmap object.
            if (value is true)
            {
                return FileIcon;
            }

            return FolderIcon;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return AvaloniaProperty.UnsetValue;
        }

        // Helper method to load a Bitmap from the application's assets.
        private static Bitmap? LoadBitmapFromAssets(string uri)
        {
            try
            {
                return new Bitmap(AssetLoader.Open(new Uri(uri)));
            }
            catch (Exception)
            {
                // Optionally handle the error if an icon is not found.
                return null;
            }
        }
    }

}
