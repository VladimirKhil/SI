using SICore.Network;
using SIData;

namespace SICore;

/// <summary>
/// Defines a client controller.
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

    void RecreateCommands();

    void Say(string text, string whom = NetworkConstants.Everybody, bool isPrivate = false);

    void Move(object arg);
}
