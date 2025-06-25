using SICore.Network;
using SIData;

namespace SICore;

/// <summary>
/// Defines a client controller.
/// </summary>
public interface IViewerClient : IDisposable
{
    GameRole Role { get; }

    ViewerData ClientData { get; }

    IPersonController MyLogic { get; }

    ViewerActions Actions { get; }

    string? Avatar { get; set; }

    void GetInfo();

    void Pause();

    void Say(string text, string whom = NetworkConstants.Everybody);

    void Move(object arg);
}
