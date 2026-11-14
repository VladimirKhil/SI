using SIPackages;
using SIQuester.Model;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SIQuester.Helpers;

/// <summary>
/// Makes pictures fit the SIGame size limit (1 MB) with the built-in WPF codecs. Audio and video are not compressed.
/// </summary>
internal static class MediaShrinker
{
    /// <summary>
    /// Returns a compressed copy that fits <paramref name="maxBytes" />, or null when it can't be done.
    /// </summary>
    public static string? Shrink(string path, string collectionName, long maxBytes) => collectionName switch
    {
        CollectionNames.ImagesStorageName => ShrinkImage(path, maxBytes),
        _ => null
    };

    // ----- Pictures -----

    /// <summary>
    /// Re-encodes the picture: first lowers JPEG quality, then reduces the size step by step until it fits.
    /// Pictures with transparency stay PNG; others become JPEG. Animated GIF and AVIF are left as is.
    /// </summary>
    private static string? ShrinkImage(string path, long maxBytes)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension is ".gif" or ".avif")
        {
            return null; // would lose animation / no encoder
        }

        BitmapSource source;

        try
        {
            var decoder = BitmapDecoder.Create(new Uri(path), BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];

            // Phone photos are often stored sideways with an EXIF "rotate me" tag; the new file has no tags,
            // so turn the pixels themselves
            source = ApplyExifOrientation(frame, ReadExifOrientation(frame));
        }
        catch
        {
            return null; // format not supported by Windows codecs
        }

        var keepTransparency = HasTransparency(source);

        // Larger than a screen makes no sense in the game
        var longSide = Math.Max(source.PixelWidth, source.PixelHeight);
        var scale = Math.Min(1.0, 2560.0 / longSide);

        for (var attempt = 0; attempt < 14; attempt++)
        {
            BitmapSource frame = scale < 0.999
                ? new TransformedBitmap(source, new ScaleTransform(scale, scale))
                : source;

            byte[] data;

            if (keepTransparency)
            {
                data = Encode(new PngBitmapEncoder(), frame);
            }
            else
            {
                var opaque = new FormatConvertedBitmap(frame, PixelFormats.Bgr24, null, 0);
                data = Array.Empty<byte>();

                foreach (var quality in new[] { 88, 80, 72, 64 })
                {
                    data = Encode(new JpegBitmapEncoder { QualityLevel = quality }, opaque);

                    if (data.Length <= maxBytes)
                    {
                        break;
                    }
                }
            }

            if (data.Length <= maxBytes)
            {
                var output = CreateOutputPath(path, keepTransparency ? ".png" : ".jpg");
                File.WriteAllBytes(output, data);
                return output;
            }

            scale *= 0.8;
        }

        return null;
    }

    /// <summary>
    /// EXIF orientation (1 = as stored; 2…8 = mirrored and/or rotated), 1 when there is no tag.
    /// </summary>
    private static int ReadExifOrientation(BitmapFrame frame)
    {
        try
        {
            if (frame.Metadata is BitmapMetadata metadata && metadata.GetQuery("System.Photo.Orientation") is ushort value)
            {
                return value;
            }
        }
        catch
        {
            // No readable metadata
        }

        return 1;
    }

    /// <summary>
    /// Turns the picture so that it looks the way the EXIF orientation says it should.
    /// </summary>
    private static BitmapSource ApplyExifOrientation(BitmapSource source, int orientation)
    {
        Transform? transform = orientation switch
        {
            2 => new ScaleTransform(-1, 1), // mirrored
            3 => new RotateTransform(180),
            4 => new ScaleTransform(1, -1), // mirrored upside down
            5 => new TransformGroup { Children = { new RotateTransform(90), new ScaleTransform(-1, 1) } },
            6 => new RotateTransform(90),
            7 => new TransformGroup { Children = { new RotateTransform(90), new ScaleTransform(1, -1) } },
            8 => new RotateTransform(270),
            _ => null,
        };

        return transform == null ? source : new TransformedBitmap(source, transform);
    }

    private static byte[] Encode(BitmapEncoder encoder, BitmapSource frame)
    {
        encoder.Frames.Add(BitmapFrame.Create(frame));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Whether the picture really uses transparency (an alpha channel alone is not enough).
    /// </summary>
    private static bool HasTransparency(BitmapSource source)
    {
        var format = source.Format;

        if (format != PixelFormats.Bgra32 && format != PixelFormats.Pbgra32 && format != PixelFormats.Prgba64
            && format != PixelFormats.Rgba64 && format != PixelFormats.Prgba128Float && format != PixelFormats.Rgba128Float
            && format != PixelFormats.Indexed1 && format != PixelFormats.Indexed2 && format != PixelFormats.Indexed4 && format != PixelFormats.Indexed8)
        {
            return false;
        }

        var bgra = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        var stride = bgra.PixelWidth * 4;
        var pixels = new byte[stride * bgra.PixelHeight];
        bgra.CopyPixels(pixels, stride, 0);

        for (var i = 3; i < pixels.Length; i += 4)
        {
            if (pixels[i] < 250)
            {
                return true;
            }
        }

        return false;
    }

    private static string CreateOutputPath(string source, string extension)
    {
        var dir = Path.Combine(Path.GetTempPath(), AppSettings.ProductName, AppSettings.MediaFolderName, Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, Path.GetFileNameWithoutExtension(source) + extension);
    }
}
