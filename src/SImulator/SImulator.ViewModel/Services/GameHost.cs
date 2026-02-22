using SICore.Contracts;
using SICore.Models;
using SICore.Results;

namespace SImulator.ViewModel.Services;

internal sealed class GameHost : IGameHost
{
    private readonly HostOptions _options = new();

    public HostOptions Options => _options;

    public int MaxImageSizeKb => int.MaxValue;

    public int MaxAudioSizeKb => int.MaxValue;

    public int MaxVideoSizeKb => int.MaxValue;

    public bool AreCustomAvatarsSupported => throw new NotImplementedException();

    public string? GetAd(string localization, out int adId)
    {
        adId = -1;
        return null;
    }

    public void LogWarning(string message)
    {
        throw new NotImplementedException();
    }

    public void SaveReport(GameResult result)
    {
        throw new NotImplementedException();
    }

    public void SendError(Exception exc, bool isWarning = false)
    {
        throw new NotImplementedException();
    }
}
