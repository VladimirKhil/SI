using SIQuester.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SIQuester.Behaviors
{
    public static class MediaController
    {
        public static IMediaOwner GetSource(DependencyObject obj)
        {
            return (IMediaOwner)obj.GetValue(SourceProperty);
        }

        public static void SetSource(DependencyObject obj, IMediaOwner value)
        {
            obj.SetValue(SourceProperty, value);
        }

        // Using a DependencyProperty as the backing store for Source.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.RegisterAttached("Source", typeof(IMediaOwner), typeof(MediaController), new PropertyMetadata(null));

        public static Slider GetProgress(DependencyObject obj)
        {
            return (Slider)obj.GetValue(ProgressProperty);
        }

        public static void SetProgress(DependencyObject obj, Slider value)
        {
            obj.SetValue(ProgressProperty, value);
        }

        // Using a DependencyProperty as the backing store for Progress.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.RegisterAttached("Progress", typeof(Slider), typeof(MediaController), new PropertyMetadata(null, OnProgressChanged));

        public static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var media = (MediaElement)d;
            var slider = (Slider)e.NewValue;

            if (media == null || slider == null)
                return;

            bool blocked = false;

            slider.ValueChanged += (s, e2) =>
                {
                    if (blocked)
                        return;

                    if (media.NaturalDuration.HasTimeSpan)
                        media.Position = TimeSpan.FromSeconds(media.NaturalDuration.TimeSpan.TotalSeconds * slider.Value / 100);
                };

            var timer = new System.Timers.Timer(1000);
            timer.Elapsed += (sender, e2) =>
            {
                media.Dispatcher.BeginInvoke((Action)(() =>
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
                }));
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
        
        public static ToggleButton GetPlayPauseButton(DependencyObject obj)
        {
            return (ToggleButton)obj.GetValue(PlayPauseButtonProperty);
        }

        public static void SetPlayPauseButton(DependencyObject obj, ToggleButton value)
        {
            obj.SetValue(PlayPauseButtonProperty, value);
        }

        // Using a DependencyProperty as the backing store for PlayPauseButton.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlayPauseButtonProperty =
            DependencyProperty.RegisterAttached("PlayPauseButton", typeof(ToggleButton), typeof(MediaController), new PropertyMetadata(null, OnPlayPauseButtonChanged));

        public static void OnPlayPauseButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var media = (MediaElement)d;
            var button = (ToggleButton)e.NewValue;

            if (media == null || button == null)
                return;

            button.Checked += async (s, e2) =>
                {
                    Uri mediaUri;

                    if (media.Source == null)
                    {
                        try
                        {
                            var source = GetSource(media);
                            var mediaUriStr = (await source?.LoadMediaAsync())?.Uri;

                            if (mediaUriStr == null)
                                return;

                            mediaUri = new Uri(mediaUriStr, UriKind.RelativeOrAbsolute);

                            media.Source = mediaUri;
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show(exc.ToString());
                            return;
                        }
                    }
                    else
                    {
                        mediaUri = media.Source;
                    }

                    if (mediaUri.IsAbsoluteUri && mediaUri.Scheme == "https")
                    {
                        MessageBox.Show("Протокол https не поддерживается!", App.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    try
                    {
                        media.Play();
                        SetIsPlaying(media, true);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("Ошибка проигрывания мультимедиа: " + exc.Message, App.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                };
            button.Unchecked += (s, e2) =>
                {
                    try
                    {
                        media.Pause();
                        SetIsPlaying(media, false);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("Ошибка проигрывания мультимедиа: " + exc.Message, App.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                };

            media.MediaEnded += (s, e2) => 
            {
                button.IsChecked = false;
                media.Position = TimeSpan.FromSeconds(0.0);
            };

            media.Unloaded += (s, e2) =>
                {
                    try
                    {
                        if (GetIsPlaying(media))
                            media.Stop();

                        media.SetValue(IsPlayingProperty, DependencyProperty.UnsetValue);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show("Ошибка проигрывания мультимедиа: " + exc.Message, App.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                };
        }

        public static bool GetIsPlaying(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsPlayingProperty);
        }

        public static void SetIsPlaying(DependencyObject obj, bool value)
        {
            obj.SetValue(IsPlayingProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsPlaying.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsPlayingProperty =
            DependencyProperty.RegisterAttached("IsPlaying", typeof(bool), typeof(MediaController), new PropertyMetadata(false));
    }
}
