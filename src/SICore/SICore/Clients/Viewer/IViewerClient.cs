using SICore.Network;
using SIData;

namespace SICore;

// TODO: remove

/// <summary>
/// Defines a client controller.
/// </summary>
public interface IViewerClient : IDisposable
{
    GameRole Role { get; }

    string? Avatar { get; set; }

    void Say(string text, string whom = NetworkConstants.Everybody);
}
