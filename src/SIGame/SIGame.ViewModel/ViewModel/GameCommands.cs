using SICore;
using SIGame.ViewModel.PlatformSpecific;
using SIGame.ViewModel.Properties;
using System;
using System.IO;
using System.Windows.Input;
using Utils;

namespace SIGame.ViewModel
{
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

        private static void OpenLogs_Executed(object arg)
        {
            var logsFolder = UserSettings.Default.GameSettings.AppSettings.LogsFolder;
            if (!Directory.Exists(logsFolder))
            {
                PlatformManager.Instance.ShowMessage(Resources.NoLogsFolder, MessageType.Warning);
                return;
            }

            Browser.Open(
                logsFolder,
                exc => PlatformManager.Instance.ShowMessage(
                    string.Format(Resources.OpenLogsError, exc.Message),
                    MessageType.Error));
        }

        private static void Comment_Executed(object arg)
        {
            var commentUri = Uri.EscapeDataString(Resources.FeedbackLink);
            Browser.Open(
                commentUri,
                exc => PlatformManager.Instance.ShowMessage(
                    string.Format(Resources.CommentSiteError + "\r\n{0}", exc.Message),
                    MessageType.Error));
        }

        private static void Donate_Executed(object arg)
        {
            var donateUri = "https://money.yandex.ru/embed/shop.xml?account=410012283941753&quickpay=shop&payment-type-choice=on&writer=seller&targets=%D0%9F%D0%BE%D0%B4%D0%B4%D0%B5%D1%80%D0%B6%D0%BA%D0%B0+%D0%B0%D0%B2%D1%82%D0%BE%D1%80%D0%B0&targets-hint=&default-sum=100&button-text=03&comment=on&hint=%D0%92%D0%B0%D1%88+%D0%BA%D0%BE%D0%BC%D0%BC%D0%B5%D0%BD%D1%82%D0%B0%D1%80%D0%B8%D0%B9";
            Browser.Open(donateUri,
                exc => PlatformManager.Instance.ShowMessage(
                    string.Format(Resources.LinkError + "\r\n{0}", exc.Message),
                    MessageType.Error));
        }

        private static void Help_Executed(object arg)
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
}
