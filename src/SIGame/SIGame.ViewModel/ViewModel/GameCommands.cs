using SICore;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Properties;
using System.Windows.Input;
using Utils;

namespace SIGame.ViewModel;

/// <summary>
/// Глобальные команды игры
/// </summary>
public static class GameCommands
{
    public static ICommand OpenLogs { get; private set; }
    public static ICommand Comment { get; private set; }
    public static ICommand Donate { get; private set; }
    public static ICommand Help { get; private set; }

    public static ICommand ChangeSound { get; private set; }
    public static ICommand ChangeFullScreen { get; private set; }
    public static ICommand ChangeSearchForUpdates { get; private set; }
    public static ICommand ChangeSendReport { get; private set; }

    static GameCommands()
    {
        OpenLogs = new CustomCommand(OpenLogs_Executed);
        Comment = new CustomCommand(Comment_Executed);
        Donate = new CustomCommand(Donate_Executed);
        Help = new CustomCommand(Help_Executed);

        // Для таскбара Windows 7
        ChangeSound = new CustomCommand(arg => UserSettings.Default.Sound = !UserSettings.Default.Sound);
        ChangeFullScreen = new CustomCommand(arg => UserSettings.Default.FullScreen = !UserSettings.Default.FullScreen);
        ChangeSearchForUpdates = new CustomCommand(arg => UserSettings.Default.SearchForUpdates = !UserSettings.Default.SearchForUpdates);
        ChangeSendReport = new CustomCommand(arg => UserSettings.Default.SendReport = !UserSettings.Default.SendReport);
    }

    private static void OpenLogs_Executed(object? arg)
    {
        var logsFolder = UserSettings.Default.GameSettings.AppSettings.LogsFolder;

        if (!Directory.Exists(logsFolder))
        {
            PlatformManager.Instance.ShowMessage(Resources.NoLogsFolder, MessageType.Warning);
            return;
        }

        try
        {
            Browser.Open(logsFolder);
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage(
                string.Format(Resources.OpenLogsError, exc.Message),
                MessageType.Error);
        }
    }

    private static void Comment_Executed(object? arg)
    {
        var commentUri = Uri.EscapeDataString(Resources.FeedbackLink);

        try
        {
            Browser.Open(commentUri);
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage(
                string.Format(Resources.CommentSiteError + "\r\n{0}", exc.Message),
                MessageType.Error);
        }
    }

    private static void Donate_Executed(object? arg)
    {
        var donateUri = "https://yoomoney.ru/to/410012283941753";

        try
        {
            Browser.Open(donateUri);
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage(
                string.Format(Resources.LinkError + "\r\n{0}", exc.Message),
                MessageType.Error);
        }
    }

    private static void Help_Executed(object? arg)
    {
        try
        {
            PlatformManager.Instance.ShowHelp(arg != null);
        }
        catch (Exception exc)
        {
            PlatformManager.Instance.ShowMessage(exc.Message, MessageType.Error, true);
        }
    }
}
