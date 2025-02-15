using SIData;

namespace SICore;

/// <summary>
/// Defines player info.
/// </summary>
public sealed class PlayerAccount : PersonAccount
{
    private int _sum = 0;
    private int _stake = 0;
    private bool _pass = false;
    private bool _inGame = true;
    private PlayerState _state = PlayerState.None;

    private bool _canBeSelected = false;

    /// <summary>
    /// Can the player be selected.
    /// </summary>
    public bool CanBeSelected
    {
        get => _canBeSelected;
        set { if (_canBeSelected != value) { _canBeSelected = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Сумма игрока
    /// </summary>
    public int Sum
    {
        get => _sum;
        set { if (_sum != value) { _sum = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Размер ставки
    /// </summary>
    public int Stake
    {
        get => _stake;
        set { if (_stake != value) { _stake = value; OnPropertyChanged(); } }
    }

    private bool _safeStake;

    public bool SafeStake
    {
        get => _safeStake;
        set { if (_safeStake != value) { _safeStake = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Может ли жать на кнопку
    /// </summary>
    public bool Pass
    {
        get => _pass;
        set { if (_pass != value) { _pass = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Находится ли игрок в игре
    /// </summary>
    public bool InGame
    {
        get => _inGame;
        set { if (_inGame != value) { _inGame = value; OnPropertyChanged(); } }
    }

    public PlayerState State
    {
        get => _state;
        set { if (_state != value) { _state = value; OnPropertyChanged(); } }
    }

    private bool _mediaLoaded = false;

    /// <summary>
    /// Has the player loaded media file.
    /// </summary>
    public bool MediaLoaded
    {
        get => _mediaLoaded;
        set { if (_mediaLoaded != value) { _mediaLoaded = value; OnPropertyChanged(); } }
    }

    private string _answer = "";

    /// <summary>
    /// Player's answer.
    /// </summary>
    public string Answer
    {
        get => _answer;
        set
        {
            if (_answer != value)
            {
                _answer = value;
                OnPropertyChanged();
            }
        }
    }

    public PlayerAccount(string name, bool isMale, bool connected, bool gameStarted)
        : base(name, isMale, connected, gameStarted)
    {
    }

    public PlayerAccount(Account account)
        : base(account)
    {
    }

    public void ClearState()
    {
        State = PlayerState.None;
        Pass = false;
        Stake = 0;
        SafeStake = false;
        MediaLoaded = false;
        Answer = "";
    }
}
