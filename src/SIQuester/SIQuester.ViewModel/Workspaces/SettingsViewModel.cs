using SIQuester.Model;
using SIQuester.ViewModel.Properties;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines application settings view model.
/// </summary>
public sealed class SettingsViewModel : WorkspaceViewModel
{
    public ICommand Reset { get; private set; }

    public override string Header => Resources.Options;

    public string[] Fonts => System.Windows.Media.Fonts.SystemFontFamilies.Select(ff => ff.Source).OrderBy(f => f).ToArray();

    public bool SpellCheckingEnabled => Environment.OSVersion.Version > new Version(6, 2);

    public string[] Languages { get; } = new string[] { "ru-RU", "en-US" };

    public AppSettings Model => AppSettings.Default;

    public SettingsViewModel() => Reset = new SimpleCommand(Reset_Executed);

    private void Reset_Executed(object? arg) => AppSettings.Default.Reset();
}
