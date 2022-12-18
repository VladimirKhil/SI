using SIQuester.ViewModel.Contracts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SIQuester.Behaviors;

/// <summary>
/// Provides methods for manipulating media play.
/// </summary>
public static class MediaController
{
    public static IMediaOwner? GetSource(DependencyObject obj) => (IMediaOwner?)obj.GetValue(SourceProperty);

    public static void SetSource(DependencyObject obj, IMediaOwner value) => obj.SetValue(SourceProperty, value);

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.RegisterAttached("Source", typeof(IMediaOwner), typeof(MediaController), new PropertyMetadata(null));

    public static Slider GetProgress(DependencyObject obj) => (Slider)obj.GetValue(ProgressProperty);

    public static void SetProgress(DependencyObject obj, Slider value) => obj.SetValue(ProgressProperty, value);

    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.RegisterAttached("Progress", typeof(Slider), typeof(MediaController), new PropertyMetadata(null, OnProgressChanged));

    public static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var media = (MediaElement)d;
        var slider = (Slider)e.NewValue;

        if (media == null || slider == null)
        {
            return;
        }

        var blocked = false;

        slider.ValueChanged += (s, e2) =>
        {
            if (blocked)
            {
                return;
            }

            if (media.NaturalDuration.HasTimeSpan)
            {
                media.Position = TimeSpan.FromSeconds(media.NaturalDuration.TimeSpan.TotalSeconds * slider.Value / 100);
            }
        };

        var timer = new System.Timers.Timer(1000);

        timer.Elapsed += (sender, e2) =>
        {
            media.Dispatcher.BeginInvoke(() =>
            {
                if (media.NaturalDuration.HasTimeSpan)
                {
                    blocked = true;
                    try
                    {
                        slider.Value = 100.0 * media.Position.TotalSeconds / media.NaturalDuration.TimeSpan.TotalSeconds;
                    }
                    finally
                    {
                        blocked = false;
                    }
                }
            });
        };

        media.MediaOpened += (sender, e2) =>
        {
            if (media.NaturalDuration.HasTimeSpan)
            {
                try
                {
                    timer.Start();
                }
                catch (ObjectDisposedException) { }
            }
        };

        void ended(object sender, RoutedEventArgs e2)
        {
            if (timer.Enabled)
            {
                timer.Stop();
            }

            timer.Dispose();
        }

        media.Unloaded += ended;
    }

    public static ToggleButton GetPlayPauseButton(DependencyObject obj) => (ToggleButton)obj.GetValue(PlayPauseButtonProperty);

    public static void SetPlayPauseButton(DependencyObject obj, ToggleButton value) => obj.SetValue(PlayPauseButtonProperty, value);

    public static readonly DependencyProperty PlayPauseButtonProperty =
        DependencyProperty.RegisterAttached(
            "PlayPauseButton",
            typeof(ToggleButton),
            typeof(MediaController),
            new PropertyMetadata(null, OnPlayPauseButtonChanged));

    public static void OnPlayPauseButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var mediaElement = (MediaElement)d;
        var playPauseButton = (ToggleButton)e.NewValue;

        if (mediaElement == null || playPauseButton == null)
        {
            return;
        }

        playPauseButton.Checked += async (s, e2) =>
        {
            Uri mediaUri;

            if (mediaElement.Source == null)
            {
                try
                {
                    var mediaOwner = GetSource(mediaElement);

                    if (mediaOwner == null)
                    {
                        return;
                    }

                    var mediaUriStr = (await mediaOwner.LoadMediaAsync())?.Uri;

                    if (mediaUriStr == null)
                    {
                        return;
                    }

                    mediaUri = new Uri(mediaUriStr, UriKind.RelativeOrAbsolute);

                    mediaElement.Source = mediaUri;
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Media play error: {exc}", App.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }
            else
            {
                mediaUri = mediaElement.Source;
            }

            try
            {
                mediaElement.Play();
                SetIsPlaying(mediaElement, true);
            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    "Ошибка проигрывания мультимедиа: " + exc.Message,
                    App.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            }
        };

        playPauseButton.Unchecked += (s, e2) =>
        {
            try
            {
                mediaElement.Pause();
                SetIsPlaying(mediaElement, false);
            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    "Ошибка проигрывания мультимедиа: " + exc.Message,
                    App.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            }
        };

        mediaElement.MediaEnded += (s, e2) => 
        {
            playPauseButton.IsChecked = false;
            mediaElement.Position = TimeSpan.FromSeconds(0.0);
        };

        mediaElement.Unloaded += (s, e2) =>
        {
            try
            {
                if (GetIsPlaying(mediaElement))
                {
                    mediaElement.Stop();
                }

                mediaElement.SetValue(IsPlayingProperty, DependencyProperty.UnsetValue);
            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    "Ошибка проигрывания мультимедиа: " + exc.Message,
                    App.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            }
        };
    }

    public static bool GetIsPlaying(DependencyObject obj) => (bool)obj.GetValue(IsPlayingProperty);

    public static void SetIsPlaying(DependencyObject obj, bool value) => obj.SetValue(IsPlayingProperty, value);

    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.RegisterAttached("IsPlaying", typeof(bool), typeof(MediaController), new PropertyMetadata(false));
}
