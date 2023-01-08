using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SIGame.Converters;

[ValueConversion(typeof(string), typeof(ImageSource))]
public sealed class ImageConverter : IValueConverter
{
    public ImageSource? FallbackImage { get; set; }

    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return FallbackImage;
        }

        if (value is byte[] data)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            image.StreamSource = new MemoryStream(data);
            image.EndInit();

            return image;
        }

        var path = value.ToString();

        if (string.IsNullOrWhiteSpace(path))
        {
            return FallbackImage;
        }

        if (!Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var uri))
        {
            return FallbackImage;
        }

        if (!uri.IsAbsoluteUri)
        {
            return FallbackImage;
        }

        var isLocalFile = uri.Scheme == "file";

        if (isLocalFile && !File.Exists(uri.LocalPath))
        {
            return FallbackImage;
        }

        try
        {
            return LoadImage(uri, isLocalFile ? BitmapCacheOption.OnLoad : BitmapCacheOption.Default);
        }
        catch (Exception exc)
        {
            if (isLocalFile)
            {
                try
                {
                    return LoadImage(uri, BitmapCacheOption.Default);
                }
                catch (Exception exc2)
                {
                    Trace.TraceWarning("Image {0} load error: {1}", uri, exc2.Message);
                    return FallbackImage;
                }
            }
            else
            {
                Trace.TraceWarning("Image {0} load error: {1}", uri, exc.Message);
            }

            return FallbackImage;
        }
    }

    private Freezable? LoadImage(Uri uri, BitmapCacheOption bitmapCacheOption)
    {
        // Don't touch decoder options without serious reasons. It could break image display in some situations
        var decoder = BitmapDecoder.Create(uri, BitmapCreateOptions.None, bitmapCacheOption);

        if (decoder.Frames.Count == 0)
        {
            return FallbackImage;
        }

        var frame = decoder.Frames[0];
        return frame.CanFreeze ? frame.GetAsFrozen() : frame;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
