using SIData;
using System.Diagnostics;

namespace SICore;

/// <summary>
/// Common viewer account.
/// </summary>
public class ViewerAccount : Account
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _isConnected = false;

    /// <summary>
    /// Is connected to game.
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        set { if (_isConnected != value) { _isConnected = value; OnPropertyChanged(); } }
    }

    private string? _avatarVideoUri;

    /// <summary>
    /// Account video avatar uri.
    /// </summary>
    public string? AvatarVideoUri
    {
        get => _avatarVideoUri;
        set
        {
            if (_avatarVideoUri != value)
            {
                _avatarVideoUri = value;

                try
                {
                    OnPropertyChanged();
                }
                catch (NotImplementedException exc) when (exc.Message.Contains("The Source property cannot be set to null"))
                {
                    // https://github.com/MicrosoftEdge/WebView2Feedback/issues/1136
                }

                try
                {
                    OnPropertyChanged(nameof(HasVideoAvatar));
                }
                catch (NotImplementedException exc) when (exc.Message.Contains("The Source property cannot be set to null"))
                {
                    // https://github.com/MicrosoftEdge/WebView2Feedback/issues/1136
                }
            }
        }
    }

    /// <summary>
    /// Video avatar marker.
    /// </summary>
    /// <remarks>
    /// Cannot bind directly to <see cref="AvatarVideoUri" /> because of the WebView2 bug.
    /// </remarks>
    public bool HasVideoAvatar => _avatarVideoUri != null;

    /// <summary>
    /// Can the account be moved.
    /// </summary>
    public bool IsMoveable { get; set; }

    public ViewerAccount(string name, bool isMale, bool isConnected)
        : base(name, isMale)
    {
        _isConnected = isConnected;
    }

    public ViewerAccount(Account account)
        : base(account)
    {

    }

    public ViewerAccount()
    {

    }
}
