using SIData;
using System.ComponentModel;
using System.Diagnostics;

namespace SICore;

/// <summary>
/// Defines a game person account.
/// </summary>
public class GamePersonAccount : ViewerAccount
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _ready = false;

    /// <summary>
    /// Is the person ready for the game.
    /// </summary>
    [DefaultValue(false)]
    public bool Ready
    {
        get => _ready;
        set { if (_ready != value) { _ready = value; OnPropertyChanged(); } }
    }

    public GamePersonAccount(Account account)
        : base(account) { }

    public GamePersonAccount() { }
}
