using SImulator.ViewModel.Model;
using SImulator.ViewModel.PlatformSpecific;

namespace SImulator.ViewModel.Contracts;

/// <summary>
/// Defines platform-specific logic.
/// </summary>
public interface IPlatformService
{
    ButtonManagers.ButtonManagerFactory ButtonManagerFactory { get; }

    Task<string?> AskSelectFileAsync(string header);

    string? AskSelectLogsFolder();

    string? AskSelectColor();

    Task<IPackageSource?> AskSelectPackageAsync(string arg);

    IGameLogger CreateGameLogger(string? folder);

    IDisplayDescriptor[] GetScreens();

    void ShowMessage(string text, bool error = true);

    void NavigateToSite();

    void InitSettings(AppSettings defaultSettings);

    void PlaySound(string name, Action? onFinish = null);

    void ClearMedia();

    string[] GetComPorts();

    string[] GetFonts();

    IPresentationController CreatePresentationController(
        IDisplayDescriptor displayDescriptor,
        IPresentationListener presentationListener,
        SoundsSettings soundsSettings,
        bool sendCommonMessages);
}
