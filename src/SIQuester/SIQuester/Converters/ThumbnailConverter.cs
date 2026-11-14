using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SIQuester.Converters;

/// <summary>
/// Small previews for the media side panel: the picture is decoded straight to preview size
/// (much faster and lighter than the full picture) and kept in a small cache, so opening the tab again is instant.
/// </summary>
[ValueConversion(typeof(string), typeof(ImageSource))]
public sealed class ThumbnailConverter : IValueConverter
{
    /// <summary>
    /// Preview width in pixels: the panel shows pictures about 230 px wide, with room for display scaling.
    /// </summary>
    private const int DecodeWidth = 360;

    /// <summary>
    /// How many previews to keep (about 0.5 MB each at most).
    /// </summary>
    private const int CacheSize = 200;

    private static readonly Dictionary<string, LinkedListNode<(string Key, ImageSource Image)>> Index = new();
    private static readonly LinkedList<(string Key, ImageSource Image)> Recent = new();

    private static readonly ImageConverter FullConverter = new();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path) || !Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            return FullConverter.Convert(value!, targetType, parameter, culture);
        }

        if (!uri.IsFile)
        {
            return FullConverter.Convert(value, targetType, parameter, culture); // web link: keep the usual way
        }

        var file = new FileInfo(uri.LocalPath);

        if (!file.Exists || file.Length == 0)
        {
            return null;
        }

        // A replaced or compressed file gets a new preview
        var key = $"{file.FullName}|{file.Length}|{file.LastWriteTimeUtc.Ticks}";

        if (Index.TryGetValue(key, out var node))
        {
            Recent.Remove(node);
            Recent.AddFirst(node);
            return node.Value.Image;
        }

        BitmapImage image;

        try
        {
            image = new BitmapImage();
            image.BeginInit();
            image.UriSource = uri;
            image.DecodePixelWidth = DecodeWidth;
            image.CacheOption = BitmapCacheOption.OnLoad; // the file is not kept open
            image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.IgnoreImageCache;
            image.EndInit();
            image.Freeze();
        }
        catch
        {
            // Not a picture the Windows codecs can read: show it the usual way (or nothing)
            return FullConverter.Convert(value, targetType, parameter, culture);
        }

        Index[key] = Recent.AddFirst((key, image));

        while (Recent.Count > CacheSize && Recent.Last != null)
        {
            Index.Remove(Recent.Last.Value.Key);
            Recent.RemoveLast();
        }

        return image;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
