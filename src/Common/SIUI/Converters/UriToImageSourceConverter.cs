using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SIUI.Converters;

public sealed class UriToImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path)
        {
            return DependencyProperty.UnsetValue;
        }

        if (!Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            return DependencyProperty.UnsetValue;
        }

        if (uri.Scheme == "file" && !File.Exists(path))
        {
            return DependencyProperty.UnsetValue;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();

            image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = uri;

            image.EndInit();

            return image;
        }
        catch
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
