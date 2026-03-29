using SImulator.ViewModel.PlatformSpecific;

namespace SImulator.ViewModel.Contracts;

/// <summary>
/// Defines platform-specific logic.
/// </summary>
public interface IPlatformService
{
    Task<string?> AskSelectFileAsync(string header);

    string? AskSelectLogsFolder();

    Task<IPackageSource?> AskSelectPackageAsync(string arg);

    IGameLogger CreateGameLogger(string? folder);

    IDisplayDescriptor[] GetScreens();

    void ShowMessage(string text, bool error = true);
}
