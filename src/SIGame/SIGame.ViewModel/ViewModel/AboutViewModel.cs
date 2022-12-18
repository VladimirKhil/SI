using SICore;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Properties;
using System.Reflection;
using System.Windows.Input;
using Utils;

namespace SIGame.ViewModel;

public sealed class AboutViewModel: ViewModel<object>
{
    public bool IsProgress => false;

    /// <summary>
    /// Версия приложения
    /// </summary>
    public string AppVersion => $"{Resources.About_Version} {Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3)}";

    public ICommand NavigateHome { get; }

    public ICommand NavigateComposer { get; }

    public ICommand OpenLicenses { get; }

    public ICommand OpenPublicDomain { get; }

    public AboutViewModel()
    {
        NavigateHome = new CustomCommand(NavigateHome_Executed);
        NavigateComposer = new CustomCommand(NavigateComposer_Executed);
        OpenLicenses = new CustomCommand(OpenLicenses_Executed);
        OpenPublicDomain = new CustomCommand(OpenPublicDomain_Executed);
    }

    private void NavigateHome_Executed(object arg) => OpenSite("https://vladimirkhil.com/si/game");

    private void NavigateComposer_Executed(object arg) => OpenSite("https://soundcloud.com/vladislav-hoshenko");

    private void OpenLicenses_Executed(object arg)
    {
        var licensesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "licenses");

        if (!Directory.Exists(licensesFolder))
        {
            PlatformManager.Instance.ShowMessage(Resources.NoLicensesFolder, MessageType.Warning);
            return;
        }

        try
        {
            Browser.Open(licensesFolder);
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage(
                string.Format(Resources.OpenLicensesError, exc.Message),
                MessageType.Error);
        }
    }

    private void OpenPublicDomain_Executed(object arg) => OpenSite("https://en.wikipedia.org/wiki/Wikipedia:Public_domain");

    private static void OpenSite(string url)
    {
        try
        {
            Browser.Open(url);
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage(
                $"{string.Format(Resources.ErrorMovingToSite, url)}{Environment.NewLine}{exc.Message}",
                MessageType.Error);
        }
    }
}
