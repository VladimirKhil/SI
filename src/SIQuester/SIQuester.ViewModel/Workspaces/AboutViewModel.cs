using SIQuester.ViewModel.Properties;
using System.Reflection;
using System.Text;
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
    /// Перейти на сайт программы
    /// </summary>
    public ICommand OpenSite { get; private set; }

    private string _licenses;

    public string Licenses
    {
        get => _licenses;
        set
        {
            _licenses = value;
            OnPropertyChanged();
        }
    }

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
            var licenseText = new StringBuilder();

            foreach (var file in new DirectoryInfo(licensesFolder).EnumerateFiles())
            {
                licenseText.Append(file.Name).AppendLine(":").AppendLine().AppendLine(File.ReadAllText(file.FullName)).AppendLine();
            }

            Licenses = licenseText.ToString();
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
