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
        set { if (_avatarVideoUri != null) { _avatarVideoUri = value; OnPropertyChanged(); } }
    }

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
