using SIData;

namespace SICore;

/// <summary>
/// Defines a common game viewer actor.
/// </summary>
public interface IViewerClient : IActor
{
    IConnector? Connector { get; set; }

    /// <summary>
    /// Является ли владельцем сервера
    /// </summary>
    bool IsHost { get; }

    ViewerData MyData { get; }

    IViewerLogic MyLogic { get; }

    string? Avatar { get; set; }

    event Action PersonConnected;

    event Action PersonDisconnected;

    event Action<int, string, string> Timer;

    void GetInfo();

    void Pause();

    void Init();

    event Action<IViewerClient> Switch;

    event Action<GameStage> StageChanged;

    event Action<string?> Ad;

    event Action<bool> IsPausedChanged;

    event Action IsHostChanged;

    void RecreateCommands();

    void Move(object arg);
}
