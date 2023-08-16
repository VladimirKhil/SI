using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SICore.Clients.Viewer;
using SIGame.Contracts;
using SIGame.Helpers;
using SIGame.Properties;
using SIGame.ViewModel;
using SIGame.ViewModel.PlatformSpecific;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Utils;

namespace SIGame.Implementation;

public sealed class DesktopManager : PlatformManager
{
    private Window? _dialogWindow;

    private readonly System.Windows.Controls.MediaElement _element = new System.Windows.Controls.MediaElement
    {
        LoadedBehavior = System.Windows.Controls.MediaState.Manual,
        UnloadedBehavior = System.Windows.Controls.MediaState.Manual
    };

    private bool _loop;

    private string? _recentAvatarDir = null;

    private string? _recentPackageDir = null;

    private bool _isListenerAttached = false;

    /// <summary>
    /// Папка звуков
    /// </summary>
    internal string SoundsUri => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");

    public override ICommand Close => ApplicationCommands.Close;

    public DesktopManager()
    {
        _element.MediaEnded += Media_Ended;
    }

    public override void ShowHelp(bool asDialog)
    {
        var helpUri = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Resources.HelpFile);
        var document = new System.Windows.Xps.Packaging.XpsDocument(helpUri, FileAccess.Read);

        var helpWindow = new Window
        {
            Icon = Application.Current.MainWindow.Icon,
            Title = CommonSettings.AppName + ": " + Resources.XpsHelp,
            WindowState = WindowState.Maximized
        };

        var docViewer = new System.Windows.Controls.DocumentViewer { Document = document.GetFixedDocumentSequence() };

        var frame = new System.Windows.Controls.Frame
        {
            NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden,
            Content = docViewer
        };

        helpWindow.Content = frame;

        helpWindow.Closed += (sender, e) =>
        {
            document.Close();
        };

        docViewer.AddHandler(
            System.Windows.Documents.Hyperlink.RequestNavigateEvent,
            new System.Windows.Navigation.RequestNavigateEventHandler((sender, e) =>
            {
                if (e.Uri.IsAbsoluteUri && e.Uri.Scheme == "http")
                {
                    try
                    {
                        Browser.Open(e.Uri.ToString());
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(
                            string.Format(Resources.SiteNavigationError + "\r\n{1}", e.Uri, exc.Message),
                            CommonSettings.AppName);
                    }

                    e.Handled = true;
                }
            }
        ));

        if (asDialog)
        {
            helpWindow.ShowDialog();
        }
        else
        {
            helpWindow.Show();
        }
    }

    public override string? SelectColor()
    {
        // TODO: Remove Windows Forms dependency

        var diag = new System.Windows.Forms.ColorDialog();

        if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var color = diag.Color;
            var convertedColor = Color.FromRgb(color.R, color.G, color.B);
            return convertedColor.ToString();
        }

        return null;
    }

    public override string? SelectLogsFolder(string initialFolder)
    {
        var dialog = new FolderBrowser { Description = Resources.SelectLogsFolder, InitialFolder = initialFolder };

        if (dialog.ShowDialog() == true)
        {
            return dialog.SelectedPath;
        }

        return null;
    }

    public override string? SelectHumanAvatar()
    {
        var openDialog = new OpenFileDialog { Title = Resources.SelectAvatar, Filter = Resources.Images + " (*.bmp, *.jpg, *.png, *.gif, *.tiff)|*.bmp;*.jpg;*.png;*.gif;*.tiff" };
        
        if (_recentAvatarDir != null)
        {
            openDialog.InitialDirectory = _recentAvatarDir;
        }

        if (openDialog.ShowDialog().Value)
        {
            try
            {
                _recentAvatarDir = Path.GetDirectoryName(openDialog.FileName);
            }
            catch (ArgumentException)
            {
                // Это наши проблемы, а не проблемы пользователя
            }

            if (new FileInfo(openDialog.FileName).Length > 1000000)
            {
                MessageBox.Show(Resources.FileLarger1Mb, AppConstants.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return null;
            }

            return openDialog.FileName;
        }

        return null;
    }

    public override string? SelectLocalPackage(long? maxPackageSize)
    {
        try
        {
            var openDialog = new OpenFileDialog { Title = Resources.SelectGamePackage, Filter = Resources.SIQuestions + "|*.siq" };

            if (_recentPackageDir != null)
            {
                openDialog.InitialDirectory = _recentPackageDir;
            }

            if (Directory.Exists(Global.PackagesUri))
            {
                openDialog.CustomPlaces.Add(new FileDialogCustomPlace(Global.PackagesUri));
            }

            var openResult = openDialog.ShowDialog();

            if (openResult.HasValue && openResult.Value)
            {
                if (maxPackageSize.HasValue && new FileInfo(openDialog.FileName).Length > maxPackageSize.Value * 1024 * 1024)
                {
                    MessageBox.Show(
                        $"{ViewModel.Properties.Resources.FileTooLarge} {string.Format(ViewModel.Properties.Resources.MaximumFileSize, maxPackageSize.Value)}",
                        AppConstants.ProductName,
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);

                    return null;
                }

                _recentPackageDir = Path.GetDirectoryName(openDialog.FileName);
                return openDialog.FileName;
            }
        }
        catch (Exception exc)
        {
            ShowMessage(exc.Message, MessageType.Warning, true);
        }

        return null;
    }

    public override string SelectStudiaBackground()
    {
        var dialog = new OpenFileDialog
        {
            Title = Resources.SelectStudiaBackgroundFileName,
            Filter = Resources.Images + " (*.bmp, *.jpg, *.png, *.gif, *.tiff)|*.bmp;*.jpg;*.png;*.gif;*.tiff"
        };

        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        return dialog.FileName;
    }

    public override string SelectMainBackground()
    {
        var dialog = new OpenFileDialog
        {
            Title = Resources.SelectMainBackgroundFIleName,
            Filter = Resources.Images + " (*.bmp, *.jpg, *.png, *.gif, *.tiff)|*.bmp;*.jpg;*.png;*.gif;*.tiff"
        };
        
        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        return dialog.FileName;
    }

    public override string SelectLogo()
    {
        var dialog = new OpenFileDialog
        {
            Title = Resources.SelectGameLogoFileName,
            Filter = Resources.Images + " (*.bmp, *.jpg, *.png, *.gif, *.tiff)|*.bmp;*.jpg;*.png;*.gif;*.tiff"
        };
        
        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        return dialog.FileName;
    }

    public override string SelectSound()
    {
        var dialog = new OpenFileDialog { Title = Resources.SelectSoundFile, Filter = Resources.Sounds + " (*.mp3)|*.mp3" };
        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        return dialog.FileName;
    }

    public override string SelectSettingsForExport()
    {
        var dialog = new SaveFileDialog
        {
            DefaultExt = ".sisettings",
            Title = Resources.SelectExportFileName,
            Filter = Resources.SISettings + "|*.sisettings"
        };
        
        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        return dialog.FileName;
    }

    public override string SelectSettingsForImport()
    {
        var dialog = new OpenFileDialog
        {
            DefaultExt = ".sisettings",
            Title = Resources.SelectImportFileName,
            Filter = Resources.SISettings + "|*.sisettings"
        };
        
        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        return dialog.FileName;
    }

    public override void Activate() => Application.Current.Dispatcher.BeginInvoke(
        () =>
        {
            var main = (MainWindow)Application.Current.MainWindow;
            main?.FlashIfNeeded(true);
        });

    public override void PlaySound(string? sound = null, double speed = 1, bool loop = false)
    {
        if (string.IsNullOrEmpty(sound))
        {
            PlaySoundInternal();
            return;
        }

        if (!UserSettings.Default.Sound)
        {
            return;
        }

        var themeSettings = UserSettings.Default.GameSettings.AppSettings.ThemeSettings;
        var source = GetSoundUri(themeSettings, sound);
        if (source != null && File.Exists(source))
        {
            PlaySoundInternal(source, speed, loop);
            return;
        }

        source = Path.Combine(SoundsUri, sound);

        if (Path.GetExtension(source).Length == 0)
        {
            if (File.Exists(source + ".wav"))
            {
                source += ".wav";
            }
            else if (File.Exists(source + ".mp3"))
            {
                source += ".mp3";
            }
        }
        
        if (!File.Exists(source))
        {
            return;
        }

        PlaySoundInternal(source, speed, loop);
    }

    private static string? GetSoundUri(ThemeSettings themeSettings, string source) => source switch
    {
        MainViewModel.MainMenuSound => themeSettings.SoundMainMenuUri,
        Sounds.RoundBegin => themeSettings.SoundBeginRoundUri,
        Sounds.RoundThemes => themeSettings.SoundRoundThemesUri,
        Sounds.QuestionSecret => themeSettings.SoundQuestionGiveUri,
        Sounds.QuestionStake => themeSettings.SoundQuestionStakeUri,
        Sounds.QuestionNoRisk => themeSettings.SoundQuestionNoRiskUri,
        Sounds.QuestionNoAnswers => themeSettings.SoundNoAnswerUri,
        Sounds.FinalThink => themeSettings.SoundFinalThinkUri,
        Sounds.RoundTimeout => themeSettings.SoundTimeoutUri,
        _ => null,
    };

    internal void PlaySoundInternal(string? source = null, double speed = 1.0, bool loop = false)
    {
        if (System.Windows.Threading.Dispatcher.CurrentDispatcher != _element.Dispatcher)
        {
            _element.Dispatcher.BeginInvoke((Action<string?, double, bool>)PlaySoundInternal, source, speed, loop);
            return;
        }

        try
        {
            if (source == null)
            {
                _element.Stop();
                _element.Source = null;
                return;
            }

            _element.Volume = UserSettings.Default.Volume / 100;
            _element.SpeedRatio = speed;
            _element.Source = new Uri(source, UriKind.RelativeOrAbsolute);

            _loop = loop;

            _element.Play();

            if (!_isListenerAttached)
            {
                AttachListener();
                _isListenerAttached = true;
            }
        }
        catch (Exception exc)
        {
            MessageBox.Show(exc.Message, CommonSettings.AppName);
        }
    }

    private void AttachListener()
    {
        UserSettings.Default.VolumeChanged += volumeRate =>
        {
            _element.Volume *= volumeRate;
        };
    }

    private void Media_Ended(object sender, EventArgs e)
    {
        if (!_loop)
        {
            return;
        }

        _element.Position = TimeSpan.Zero;
        _element.Play();
    }

    public override void ShowMessage(string text, MessageType messageType, bool uiThread = false)
    {
        try
        {
            var image = messageType switch
            {
                MessageType.Warning => MessageBoxImage.Warning,
                MessageType.Error => MessageBoxImage.Error,
                _ => MessageBoxImage.Information,
            };

            if (uiThread)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(text, CommonSettings.AppName, MessageBoxButton.OK, image);
                });
            }
            else
            {
                MessageBox.Show(text, CommonSettings.AppName, MessageBoxButton.OK, image);
            }
        }
        catch (MissingMethodException)
        {
            MessageBox.Show(Resources.NETFrameworkCorrupted, CommonSettings.AppName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            Application.Current.Dispatcher.BeginInvoke((Action)Application.Current.Shutdown);
        }
    }

    public override bool Ask(string text) =>
        MessageBox.Show(text, CommonSettings.AppName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public override void SendErrorReport(Exception exc, bool isWarning = false)
    {
        if (isWarning)
        {
            MessageBox.Show(exc.Message, AppConstants.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
        else
        {
            var errorManager = ServiceProvider.GetRequiredService<IErrorManager>();
            errorManager.SendErrorReport(exc);
        }
    }

    public override string GetKeyName(int key) => ((Key)key).ToString();

    public override IAnimatableTimer GetAnimatableTimer() => new AnimatableTimer();

    public override void ExecuteOnUIThread(Action action) => Application.Current?.Dispatcher.Invoke(action);

    public override void ShowDialogWindow(object dataContext, Action onClose)
    {
        _dialogWindow?.Close();
        _dialogWindow = new DialogWindow { DataContext = dataContext };

        void closed(object? sender, EventArgs eventArgs)
        {
            _dialogWindow.Closed -= closed;
            onClose();
        }

        _dialogWindow.Closed += closed;
        _dialogWindow.Show();
    }

    public override void CloseDialogWindow()
    {
        _dialogWindow?.Close();
        _dialogWindow = null;
    }
}
