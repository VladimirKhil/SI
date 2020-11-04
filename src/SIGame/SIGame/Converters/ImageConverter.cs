using System;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SIGame.Converters
{
    [ValueConversion(typeof(string), typeof(ImageSource))]
    public sealed class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

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
                return null;

            if (!Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out Uri uri))
                return null;

            if (!uri.IsAbsoluteUri)
                return null;

            var isLocalFile = uri.Scheme == "file";
            if (isLocalFile && !File.Exists(uri.LocalPath))
                return null;

            try
            {
                return LoadImage(uri, isLocalFile ? BitmapCacheOption.OnLoad : BitmapCacheOption.Default);
            }
            catch
            {
                if (isLocalFile)
                {
                    try
                    {
                        return LoadImage(uri, BitmapCacheOption.Default);
                    }
                    catch
                    {
                        return null;
                    }
                }

                return null;
            }
        }

        private static Freezable LoadImage(Uri uri, BitmapCacheOption bitmapCacheOption)
        {
            var decoder = BitmapDecoder.Create(uri, BitmapCreateOptions.None, bitmapCacheOption); // Лучше эти два параметра не трогать, так как в противном случае в некоторых ситуациях изображения могут перестать отображаться
            if (decoder.Frames.Count == 0)
                return null;

            var frame = decoder.Frames[0];
            return frame.CanFreeze ? frame.GetAsFrozen() : frame;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
