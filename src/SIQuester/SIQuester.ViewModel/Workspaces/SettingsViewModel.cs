using SIQuester.Model;
using SIQuester.ViewModel.PlatformSpecific;
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

    public string[] Fonts => PlatformManager.Instance.FontFamilies;

    public bool SpellCheckingEnabled => Environment.OSVersion.Version > new Version(6, 2);

    public string[] Languages { get; } = new string[] { "ru-RU", "en-US" };

    public string[] GPTModels { get; } = new string[] { "gpt-4o-mini-2024-07-18", "gpt-4o-2024-08-06", "gpt-5-2025-08-07" };

    public AppSettings Model => AppSettings.Default;

    public string GPTPrompt => string.IsNullOrEmpty(Model.GPTPrompt)
        ? Resources.DefaultGPTPrompt : Model.GPTPrompt;

    public SettingsViewModel() => Reset = new SimpleCommand(Reset_Executed);

    private void Reset_Executed(object? arg) => AppSettings.Default.Reset();
}
