using SIUI.ViewModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace SIUI.Behaviors;

public static class MediaController
{
    /// <summary>
    /// Current volume level.
    /// </summary>
    public static double Volume { get; private set; } = 0.5;

    static MediaController()
    {
        if (waveOutGetVolume(IntPtr.Zero, out uint dwVolume) == 0)
        {
            // Setting default volume level from mixer (dividing it by half because default volume is 0.5)
            Volume = ((double)Math.Max(dwVolume >> 16, dwVolume & 0x0000FFFF)) / 0xFFFF / 2;
        }
    }

    public static void UpdateVolume(double factor)
    {
        Volume = Math.Min(1.0, Math.Max(0.0, Volume * factor));
        _ = waveOutSetVolume(IntPtr.Zero, (uint)(0xFFFF * 2 * Volume));
    }

    private static readonly DependencyPropertyDescriptor SourceDescriptor =
        DependencyPropertyDescriptor.FromProperty(MediaElement.SourceProperty, typeof(MediaElement));

    public static TableInfoViewModel? GetLoadHandler(DependencyObject obj) => (TableInfoViewModel?)obj.GetValue(LoadHandlerProperty);

    public static void SetLoadHandler(DependencyObject obj, TableInfoViewModel? value) => obj.SetValue(LoadHandlerProperty, value);

    public static readonly DependencyProperty LoadHandlerProperty =
        DependencyProperty.RegisterAttached("LoadHandler", typeof(TableInfoViewModel), typeof(MediaController), new PropertyMetadata(null, OnLoadHandlerChanged));

    public static void OnLoadHandlerChanged(DependencyObject? d, DependencyPropertyChangedEventArgs e)
    {
        var mediaElement = (MediaElement?)d;
        var tableInfo = (TableInfoViewModel?)e.NewValue;

        if (mediaElement == null || tableInfo == null)
        {
            return;
        }

        mediaElement.LoadedBehavior = MediaState.Manual;
        mediaElement.Volume = Volume;

        void loaded(object? sender, EventArgs e2)
        {
            if (mediaElement.Source == null)
            {
                return;
            }

            try
            {
                mediaElement.Play();
                tableInfo.OnMediaLoad();
            }
            catch (Exception exc)
            {
                tableInfo.OnMediaLoadError(new MediaLoadException(mediaElement.Source?.ToString(), exc));
            }
        }

        mediaElement.Loaded += loaded;

        SourceDescriptor.AddValueChanged(mediaElement, loaded);

        mediaElement.MediaFailed += (sender, e2) =>
        {
            tableInfo.OnMediaLoadError(new MediaLoadException(((MediaElement?)sender)?.Source?.ToString() ?? "", e2.ErrorException));
        };

        System.Timers.Timer? timer = null;

        if (tableInfo.HasMediaProgress())
        {
            timer = new System.Timers.Timer(1000);

            timer.Elapsed += (sender, e2) =>
            {
                mediaElement.Dispatcher.BeginInvoke(() =>
                {
                    if (mediaElement.NaturalDuration.HasTimeSpan)
                    {
                        tableInfo.OnMediaProgress(mediaElement.Position.TotalSeconds / mediaElement.NaturalDuration.TimeSpan.TotalSeconds);
                    }
                });
            };
        }

        mediaElement.MediaOpened += (sender, e2) =>
        {
            mediaElement.ScrubbingEnabled = mediaElement.HasVideo;
            tableInfo.OnMediaStart();

            if (mediaElement.NaturalDuration.HasTimeSpan && timer != null)
            {
                timer.Start();
            }
        };

        void seekHandler(int pos)
        {
            if (mediaElement.NaturalDuration.HasTimeSpan)
            {
                mediaElement.Position = TimeSpan.FromSeconds(mediaElement.NaturalDuration.TimeSpan.TotalSeconds * pos / 100);
            }
        }

        void pauseHandler()
        {
            mediaElement.Dispatcher.BeginInvoke(mediaElement.Pause);
        }

        void resumeHandler()
        {
            mediaElement.Dispatcher.BeginInvoke(mediaElement.Play);
        }

        void volumeChangedHandler(double factor)
        {
            mediaElement.Volume = Math.Min(1.0, Math.Max(0.0, mediaElement.Volume * factor));
        }

        tableInfo.MediaSeek += seekHandler;
        tableInfo.MediaPause += pauseHandler;
        tableInfo.MediaResume += resumeHandler;
        tableInfo.VolumeChanged += volumeChangedHandler;

        void ended(object sender, RoutedEventArgs e2)
        {
            if (timer != null)
            {
                if (timer.Enabled)
                {
                    timer.Stop();
                }

                timer.Dispose();
                timer = null;
            }

            ((MediaElement)sender).Loaded -= loaded;

            tableInfo.MediaSeek -= seekHandler;
            tableInfo.MediaPause -= pauseHandler;
            tableInfo.MediaResume -= resumeHandler;
            tableInfo.VolumeChanged -= volumeChangedHandler;

            SourceDescriptor.RemoveValueChanged(mediaElement, loaded);
        }

        mediaElement.Unloaded += ended;

        mediaElement.MediaEnded += (sender, e2) =>
        {
            tableInfo.OnMediaEnd();
        };
    }

    [DllImport("winmm.dll")]
    private static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

    [DllImport("winmm.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
    private static extern uint waveOutSetVolume(IntPtr uDeviceID, uint dwVolume);
}
