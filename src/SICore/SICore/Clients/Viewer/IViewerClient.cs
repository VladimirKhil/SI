using SICore.Network;
using SIData;

namespace SICore;

/// <summary>
/// Defines a client controller.
/// </summary>
public interface IViewerClient : IDisposable
{
    /// <summary>
    /// Host marker.
    /// </summary>
    bool IsHost { get; }

    GameRole Role { get; }

    ViewerData MyData { get; }

    IPersonController MyLogic { get; }

    ViewerActions Actions { get; }

    string? Avatar { get; set; }

    void GetInfo();

    void Pause();

    void Say(string text, string whom = NetworkConstants.Everybody);

    void Move(object arg);
}
