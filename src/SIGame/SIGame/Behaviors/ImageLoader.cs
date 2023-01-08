using SICore;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SIGame.Behaviors;

// TODO: check code duplication with ImageConverter class

public static class ImageLoader
{
    private static readonly HttpClient HttpClient = new() { DefaultRequestVersion = HttpVersion.Version20 };

    public static PersonAccount GetImageSource(DependencyObject obj) => (PersonAccount)obj.GetValue(ImageSourceProperty);

    public static void SetImageSource(DependencyObject obj, PersonAccount value) => obj.SetValue(ImageSourceProperty, value);

    public static readonly DependencyProperty ImageSourceProperty =
        DependencyProperty.RegisterAttached(
            "ImageSource",
            typeof(PersonAccount),
            typeof(ImageLoader),
            new PropertyMetadata(null, OnImageSourceChanged));

    private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var image = (Image)d;
        var person = (PersonAccount)e.NewValue;

        void handler(object? s, PropertyChangedEventArgs e2)
        {
            if (e2.PropertyName == nameof(PersonAccount.Picture) || e2.PropertyName == nameof(PersonAccount.IsConnected))
            {
                UpdateAvatar(person, image);
            }
        }

        if (e.OldValue != null)
        {
            ((PersonAccount)e.OldValue).PropertyChanged -= handler;
        }

        if (person == null)
        {
            if (e.OldValue != null)
            {
                image.Source = null;
            }
        }
        else
        {
            person.PropertyChanged += handler;
            UpdateAvatar(person, image);
        }
    }

    private static async void UpdateAvatar(PersonAccount person, Image image)
    {
        if (image.Dispatcher != System.Windows.Threading.Dispatcher.CurrentDispatcher)
        {
            await image.Dispatcher.InvokeAsync(() => UpdateAvatar(person, image));
            return;
        }

        if (!person.IsConnected)
        {
            image.Source = null;
            return;
        }

        var value = person.Picture;

        var path = value?.ToString();

        if (string.IsNullOrWhiteSpace(path))
        {
            var avatarCode = person.IsMale ? "m" : "f";

            var defaultImage = new BitmapImage();
            defaultImage.BeginInit();
            defaultImage.UriSource = new Uri($"pack://application:,,,/SIGame;component/Resources/avatar-{avatarCode}.png");
            defaultImage.EndInit();

            image.Source = defaultImage;
            return;
        }

        if (!Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var uri))
        {
            return;
        }

        if (!uri.IsAbsoluteUri)
        {
            return;
        }

        var isLocalFile = uri.Scheme == "file";

        if (isLocalFile && !File.Exists(uri.LocalPath))
        {
            return;
        }

        if (isLocalFile)
        {
            try
            {
                var decoder = BitmapDecoder.Create(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                
                if (decoder.Frames.Count == 0)
                {
                    return;
                }

                var frame = decoder.Frames[0];
                image.Source = frame.CanFreeze ? (BitmapSource)frame.GetAsFrozen() : frame;
            }
            catch
            {

            }

            return;
        }

        try
        {
            using var response = await HttpClient.GetAsync(uri);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(await response.Content.ReadAsStringAsync());
            }

            using var stream = await response.Content.ReadAsStreamAsync();

            // It's better not to touch these options because their incorrect values could lead to images load errors
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.Default);

            if (decoder.Frames.Count == 0)
            {
                return;
            }

            var frame = decoder.Frames[0];
            image.Source = frame.CanFreeze ? (BitmapSource)frame.GetAsFrozen() : frame;
        }
        catch (Exception exc)
        {
            Trace.TraceError($"Image {uri} load error: {exc}");
        }
    }
}
