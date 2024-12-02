using SICore.Network;
using SIData;

namespace SICore;

/// <summary>
/// Defines a common game viewer actor.
/// </summary>
public interface IViewerClient : IActor
{
    /// <summary>
    /// Является ли владельцем сервера
    /// </summary>
    bool IsHost { get; }

    GameRole Role { get; }

    ViewerData MyData { get; }

    IViewerLogic MyLogic { get; }

    ViewerActions Actions { get; }

    string? Avatar { get; set; }

    void GetInfo();

    void Pause();

    void Init();

    event Action<IViewerClient> Switch;

    event Action<GameStage> StageChanged;

    event Action<string?> Ad;

    void RecreateCommands();

    void Say(string text, string whom = NetworkConstants.Everybody, bool isPrivate = false);

    void Move(object arg);
}
