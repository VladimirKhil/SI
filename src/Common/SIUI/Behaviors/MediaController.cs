using SIUI.ViewModel;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace SIUI.Behaviors
{
    public static class MediaController
    {
        public static bool GetIsAttached(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsAttachedProperty);
        }

        public static void SetIsAttached(DependencyObject obj, bool value)
        {
            obj.SetValue(IsAttachedProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsAttached.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsAttachedProperty =
            DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(MediaController), new PropertyMetadata(false, OnIsAttachedChanged));

        [DllImport("winmm.dll")]
        public static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

        [DllImport("winmm.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern uint waveOutSetVolume(IntPtr uDeviceID, uint dwVolume);

        private static readonly DependencyPropertyDescriptor SourceDescriptor = 
            DependencyPropertyDescriptor.FromProperty(MediaElement.SourceProperty, typeof(MediaElement));

        public static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var mediaElement = (MediaElement)d;
            var tableInfo = (TableInfoViewModel)mediaElement?.DataContext;

            if (tableInfo == null)
            {
                return;
            }

            mediaElement.LoadedBehavior = MediaState.Manual;

            if (waveOutGetVolume(IntPtr.Zero, out uint dwVolume) == 0)
            {
                mediaElement.Volume = ((double)Math.Max(dwVolume >> 16, dwVolume & 0x0000FFFF)) / 0xFFFF / 2; // Зададим уровень звука из микшера (делим пополам, т.к. громкость по умолчанию - 0.5)
            }

            void loaded(object sender, EventArgs e2)
            {
                try
                {
                    mediaElement.Play();
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message);
                }
            }

            mediaElement.Loaded += (sender, e2) =>
            {
                try
                {
                    mediaElement.Play();
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message);
                }
            };

            SourceDescriptor.AddValueChanged(mediaElement, loaded);

            mediaElement.MediaFailed += (sender, e2) =>
                {
                    tableInfo.OnMediaLoadError(e2.ErrorException);
                    Trace.TraceError(e2.ErrorException.ToString());
                };

            System.Timers.Timer timer = null;
            if (tableInfo.HasMediaProgress())
            {
                timer = new System.Timers.Timer(1000);
                timer.Elapsed += (sender, e2) =>
                {
                    mediaElement.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        if (mediaElement.NaturalDuration.HasTimeSpan)
                            tableInfo.OnMediaProgress(mediaElement.Position.TotalSeconds / mediaElement.NaturalDuration.TimeSpan.TotalSeconds);
                    }));
                };
            }

            mediaElement.MediaOpened += (sender, e2) =>
            {
                mediaElement.ScrubbingEnabled = mediaElement.HasVideo;
                tableInfo.OnMediaStart();

                if (mediaElement.NaturalDuration.HasTimeSpan && timer != null)
                    timer.Start();
            };

            void seekHandler(int pos)
            {
                if (mediaElement.NaturalDuration.HasTimeSpan)
                    mediaElement.Position = TimeSpan.FromSeconds(mediaElement.NaturalDuration.TimeSpan.TotalSeconds * pos / 100);
            }

            void pauseHandler()
            {
                mediaElement.Dispatcher.BeginInvoke((Action)mediaElement.Pause);
            }

            void resumeHandler()
            {
                mediaElement.Dispatcher.BeginInvoke((Action)mediaElement.Play);
            }

            void volumeChangedHandler(double volume)
            {
                mediaElement.Volume *= volume;
                waveOutSetVolume(IntPtr.Zero, (uint)(0xFFFF * 2 * mediaElement.Volume));
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
    }
}
