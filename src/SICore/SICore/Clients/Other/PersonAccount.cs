using SIData;
using System.ComponentModel;
using System.Diagnostics;

namespace SICore;

/// <summary>
/// Defines main game person (player or showman).
/// </summary>
public class PersonAccount : ViewerAccount
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _ready = false;

    /// <summary>
    /// Is the person ready to start the game.
    /// </summary>
    [DefaultValue(false)]
    public bool Ready
    {
        get { return _ready; }
        set { if (_ready != value) { _ready = value; OnPropertyChanged(); } }
    }

    private bool _gameStarted = false;

    public bool GameStarted
    {
        get { return _gameStarted; }
        set { if (_gameStarted != value) { _gameStarted = value; OnPropertyChanged(); } }
    }

    private bool _isChooser = false;

    public bool IsChooser
    {
        get { return _isChooser; }
        set
        {
            if (_isChooser != value)
            {
                _isChooser = value;
                OnPropertyChanged();
            }
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _replic = "";

    public string Replic
    {
        get { return _replic; }
        set { if (_replic != value) { _replic = value; OnPropertyChanged(); } }
    }

    public bool IsShowman { get; set; }

    private bool _isDeciding;

    public bool IsDeciding
    {
        get => _isDeciding;
        set
        {
            if (_isDeciding != value)
            {
                _isDeciding = value;
                OnPropertyChanged();
            }
        }
    }

    public PersonAccount(string name, bool isMale, bool connected, bool gameStarted)
        : base(name, isMale, connected)
    {
        _gameStarted = gameStarted;
    }

    public PersonAccount(Account account)
        : base(account)
    {

    }
}
