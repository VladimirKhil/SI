using SIQuester.ViewModel.Properties;
using System.Reflection;
using System.Windows.Input;
using Utils;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines application info view model.
/// </summary>
public sealed class AboutViewModel : WorkspaceViewModel
{
    private const string _LicensesFolder = "Licenses";

    public override string Header => Resources.About;

    /// <summary>
    /// Application version.
    /// </summary>
    public string Version
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        }
    }

    /// <summary>
    /// Opens application website.
    /// </summary>
    public ICommand OpenSite { get; private set; }

    private readonly Dictionary<string, string> _licenses = new();

    public Dictionary<string, string> Licenses => _licenses;

    public AboutViewModel()
    {
        OpenSite = new SimpleCommand(arg => GoToUrl("https://vladimirkhil.com/si/siquester"));

        LoadLicenses();
    }

    private void LoadLicenses()
    {
        try
        {
            var licensesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _LicensesFolder);

            foreach (var file in new DirectoryInfo(licensesFolder).EnumerateFiles())
            {
                _licenses[file.Name] = File.ReadAllText(file.FullName);
            }
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private void GoToUrl(string url)
    {
        try
        {
            Browser.Open(url);
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }
}
