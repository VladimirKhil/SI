using SICore.PlatformSpecific;
using SIUI.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SICore
{
    public interface IGameManager : IPlatformManager
    {
        void OnFlash(bool flash = true);

        void OnError(Exception exc);

        void PlaySound(string sound = null, double speed = 1.0);

        bool MakeLogs { get; }

        string LogsFolder { get; }

        bool TranslateGameToChat { get; }

        string GameButtonKey { get; }

        bool SendReport { get; }

        bool AreAnswersShown { get; set; }

        string PhotoUri { get; }

        bool ShowBorderOnFalseStart { get; }

        bool LoadExternalMedia { get; }

        /// <summary>
        /// Maximum recommended image size.
        /// </summary>
        int MaxImageSizeKb { get; }

        /// <summary>
        /// Maximum recommended audio size.
        /// </summary>
        int MaxAudioSizeKb { get; }

        /// <summary>
        /// Maximum recommended video size.
        /// </summary>
        int MaxVideoSizeKb { get; }

        string GetPhotoUri(string name);

        void SendError(Exception exc, bool isWarning = false);

        Task SaveReportAsync(Results.GameResult result, CancellationToken cancellationToken = default);

        void OnPictureError(string remoteUri);

        void SaveBestPlayers(IEnumerable<PlayerAccount> players);

        SettingsViewModel GetSettings();

        void OnGameFinished(string packageId);

        /// <summary>
        /// Получить рекламное сообщение
        /// </summary>
        string GetAd(string localization, out int adId);

        void LogWarning(string message);
    }
}
