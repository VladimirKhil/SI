using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SIQuester.Converters;

[ValueConversion(typeof(string), typeof(ImageSource))]
public sealed class ImageConverter : IValueConverter
{
    private BitmapImage _defaultImage;

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            if (_defaultImage == null)
            {
                var logo = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/logo.jpg")).Stream;

                _defaultImage = new BitmapImage();
                _defaultImage.BeginInit();
                _defaultImage.StreamSource = logo;
                _defaultImage.EndInit();
                _defaultImage.Freeze();
            }

            return _defaultImage;
        }

        var path = value.ToString();

        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (!Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri uri))
        {
            return null;
        }

        if (!uri.IsAbsoluteUri)
        {
            return null;
        }

        var isLocalFile = uri.Scheme == "file";
        
        if (isLocalFile && (!File.Exists(uri.LocalPath) || new FileInfo(uri.LocalPath).Length == 0))
        {
            return null;
        }

        try
        {
            // Do not touch these params otherwise images may suddenly disappear

            var decoder = BitmapDecoder.Create(
                uri,
                BitmapCreateOptions.IgnoreColorProfile,
                isLocalFile ? BitmapCacheOption.OnLoad : BitmapCacheOption.Default);
            
            if (decoder.Frames.Count == 0)
            {
                return null;
            }

            var frame = decoder.Frames[0];

            return frame.CanFreeze ? frame.GetAsFrozen() : frame;
        }
        catch (Exception)
        {
            // TODO: log
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
