using Notions;
using SICore;
using SICore.PlatformSpecific;
using SICore.Results;
using SIGame.ViewModel.Properties;
using SIUI.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SIGame.ViewModel
{
    public sealed class BackLink: BackLinkCore
    {
        internal static BackLink Default { get; set; }

        private readonly AppSettingsViewModel _settings;
        private readonly UserSettings _userSettings;

        internal BackLink(AppSettingsViewModel settings, UserSettings userSettings)
        {
            _settings = settings;
            _userSettings = userSettings;
        }

        public override void OnFlash(bool flash = true) => PlatformSpecific.PlatformManager.Instance.Activate();

        public override void PlaySound(string sound = null, double speed = 1.0) => PlatformSpecific.PlatformManager.Instance.PlaySound(sound, speed);

        public override bool MakeLogs => _userSettings.GameSettings.AppSettings.MakeLogs;

        public override bool TranslateGameToChat => _userSettings.GameSettings.AppSettings.TranslateGameToChat;

        public override string LogsFolder => _userSettings.GameSettings.AppSettings.LogsFolder;

        public override string GameButtonKey => PlatformSpecific.PlatformManager.Instance.GetKeyName(_userSettings.GameSettings.AppSettings.GameButtonKey2);

        public override void OnError(Exception exc) => PlatformSpecific.PlatformManager.Instance.ShowMessage(exc.ToString(), PlatformSpecific.MessageType.Error, true);

        public override void LogWarning(string message) => Trace.TraceWarning(message);

        public override void SendError(Exception exc, bool isWarning = false) => PlatformSpecific.PlatformManager.Instance.SendErrorReport(exc);

        /// <summary>
        /// Отправить информацию о результатах игры на сервер
        /// </summary>
        public override void SaveReport(GameResult gameResult)
        {
            
        }

        public override void OnPictureError(string remoteUri)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(string.Format(Resources.Error_UploadingAvatar + ": {0}", remoteUri), PlatformSpecific.MessageType.Warning, true);
        }

        public override string PhotoUri
        {
            get { return Global.PhotoUri; }
        }

        public override string GetPhotoUri(string name)
        {
            return System.IO.Path.Combine(Global.PhotoUri, name.Translit() + ".jpg");
        }

        public override bool SendReport
        {
            get { return _userSettings.SendReport; }
        }

        public override void SaveBestPlayers(IEnumerable<PlayerAccount> players)
        {
            var bestPlayers = CommonSettings.Default.BestPlayers;

            foreach (var player in players)
            {
                int d = bestPlayers.Count - 1;
                while (d > -1 && player.Sum >= bestPlayers[d].Result)
                    d--;

                bestPlayers.Insert(d + 1, new BestPlayer { Name = player.Name, Result = player.Sum });
                if (bestPlayers.Count > BestPlayer.Total)
                    bestPlayers.RemoveAt(bestPlayers.Count - 1);
            }
        }

        public override SettingsViewModel GetSettings()
        {
            return _settings.ThemeSettings.SIUISettings;
        }

        public override void OnGameFinished(string packageId)
        {
            PlatformSpecific.PlatformManager.Instance.ExecuteOnUIThread(() =>
            {
                if (!string.IsNullOrEmpty(packageId) && !_userSettings.PackageHistory.Contains(packageId))
                {
                    _userSettings.PackageHistory.Add(packageId);
                }
            });
        }

        public override bool AreAnswersShown
        {
            get => _userSettings.GameSettings.AppSettings.AreAnswersShown;
            set => _userSettings.GameSettings.AppSettings.AreAnswersShown = value;
        }

        public override bool ShowBorderOnFalseStart => _userSettings.GameSettings.AppSettings.ShowBorderOnFalseStart;

        public override bool LoadExternalMedia => _userSettings.LoadExternalMedia;
    }
}
