using Microsoft.Extensions.DependencyInjection;
using Notions;
using SICore;
using SICore.PlatformSpecific;
using SICore.Results;
using SIGame.ViewModel.Properties;
using SIUI.ViewModel;
using System.Diagnostics;

namespace SIGame.ViewModel;

public sealed class BackLink : BackLinkCore
{
    internal static BackLink Default { get; set; }

    private readonly AppSettingsViewModel _settings;

    private readonly UserSettings _userSettings;

    private readonly IServiceProvider _serviceProvider;

    internal BackLink(AppSettingsViewModel settings, UserSettings userSettings, IServiceProvider serviceProvider)
    {
        _settings = settings;
        _userSettings = userSettings;
        _serviceProvider = serviceProvider;
    }

    public override void OnFlash(bool flash = true) => PlatformSpecific.PlatformManager.Instance.Activate();

    public override void PlaySound(string sound = null, double speed = 1.0) => PlatformSpecific.PlatformManager.Instance.PlaySound(sound, speed);

    public override bool MakeLogs => _userSettings.GameSettings.AppSettings.MakeLogs;

    public override bool TranslateGameToChat => _userSettings.GameSettings.AppSettings.TranslateGameToChat;

    public override string LogsFolder => _userSettings.GameSettings.AppSettings.LogsFolder;

    public override string GameButtonKey => PlatformSpecific.PlatformManager.Instance.GetKeyName(_userSettings.GameSettings.AppSettings.GameButtonKey2);

    public override int MaximumTableTextLength => _userSettings.GameSettings.AppSettings.ThemeSettings.MaximumTableTextLength;

    public override int MaximumReplicTextLength => _userSettings.GameSettings.AppSettings.ThemeSettings.MaximumReplicTextLength;

    public override void OnError(Exception exc) => PlatformSpecific.PlatformManager.Instance.ShowMessage(exc.ToString(), PlatformSpecific.MessageType.Error, true);

    public override void LogWarning(string message) => Trace.TraceWarning(message);

    public override void SendError(Exception exc, bool isWarning = false) => PlatformSpecific.PlatformManager.Instance.SendErrorReport(exc);

    /// <summary>
    /// Sends game results info to server.
    /// </summary>
    public override async Task SaveReportAsync(GameResult gameResult, CancellationToken cancellationToken = default)
    {
        SI.GameResultService.Client.AnswerInfo answerConverter(AnswerInfo q) =>
            new() { Answer = q.Answer, Question = q.Question, Theme = q.Theme, Round = q.Round };

        SI.GameResultService.Client.PersonResult personResultConverter(PersonResult p) =>
            new() { Name = p.Name, Sum = p.Sum };

        var result = new SI.GameResultService.Client.GameResult
        {
            ApellatedQuestions = new List<SI.GameResultService.Client.AnswerInfo>(gameResult.ApellatedQuestions.Select(answerConverter)),
            Comments = gameResult.Comments,
            ErrorLog = gameResult.ErrorLog,
            MarkedQuestions = new List<SI.GameResultService.Client.AnswerInfo>(gameResult.MarkedQuestions.Select(answerConverter)),
            PackageID = gameResult.PackageID,
            PackageName = gameResult.PackageName,
            Results = new List<SI.GameResultService.Client.PersonResult>(gameResult.Results.Select(personResultConverter)),
            WrongVersions = new List<SI.GameResultService.Client.AnswerInfo>(gameResult.WrongVersions.Select(answerConverter))
        };

        var gameResultService = _serviceProvider.GetRequiredService<SI.GameResultService.Client.IGameResultServiceClient>();
        
        try
        {
            await gameResultService.SendGameReportAsync(result, cancellationToken);
        }
        catch (Exception)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(Resources.Error_SendingReport, PlatformSpecific.MessageType.Warning, true);

            if (CommonSettings.Default.DelayedResultsNew.Count < 10)
            {
                CommonSettings.Default.DelayedResultsNew.Add(result);
            }
        }
    }

    public override void OnPictureError(string remoteUri) =>
        PlatformSpecific.PlatformManager.Instance.ShowMessage(
            string.Format(Resources.Error_UploadingAvatar + ": {0}", remoteUri),
            PlatformSpecific.MessageType.Warning,
            true);

    public override string PhotoUri => Global.PhotoUri;

    public override string GetPhotoUri(string name) => System.IO.Path.Combine(Global.PhotoUri, name.Translit() + ".jpg");

    public override bool SendReport => _userSettings.SendReport;

    public override void SaveBestPlayers(IEnumerable<PlayerAccount> players)
    {
        var bestPlayers = CommonSettings.Default.BestPlayers;

        foreach (var player in players)
        {
            var d = bestPlayers.Count - 1;

            while (d > -1 && player.Sum >= bestPlayers[d].Result)
            {
                d--;
            }

            bestPlayers.Insert(d + 1, new BestPlayer { Name = player.Name, Result = player.Sum });

            if (bestPlayers.Count > BestPlayer.Total)
            {
                bestPlayers.RemoveAt(bestPlayers.Count - 1);
            }
        }
    }

    public override SettingsViewModel GetSettings() => _settings.ThemeSettings.SIUISettings;

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

    public override int MaxImageSizeKb => int.MaxValue;

    public override int MaxAudioSizeKb => int.MaxValue;

    public override int MaxVideoSizeKb => int.MaxValue;

    public override bool AreCustomAvatarsSupported => true;
}
