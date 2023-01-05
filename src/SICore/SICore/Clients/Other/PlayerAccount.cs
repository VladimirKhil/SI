using SIData;
using System.Diagnostics;

namespace SICore;

/// <summary>
/// Информация об игроке во время игры
/// </summary>
public sealed class PlayerAccount : PersonAccount
{
    private int _sum = 0;
    private int _stake = 0;
    private bool _pass = false;
    private bool _inGame = true;
    private PlayerState _state = PlayerState.None;

    private bool _canBeSelected = false;

    public bool CanBeSelected
    {
        get => _canBeSelected;
        set { if (_canBeSelected != value) { _canBeSelected = value; OnPropertyChanged(); Select.CanBeExecuted = value; } }
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
    internal bool Pass
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

    public CustomCommand Select { get; private set; }

    internal Action<PlayerAccount>? SelectionCallback { get; set; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private CustomCommand delete = null;

    /// <summary>
    /// Удалить стол
    /// </summary>
    public CustomCommand Delete
    {
        get => delete;
        set { if (delete != value) { delete = value; OnPropertyChanged(); } }
    }

    public PlayerAccount(string name, bool isMale, bool connected, bool gameStarted)
        : base(name, isMale, connected, gameStarted)
    {
        Init();
    }

    public PlayerAccount(Account account)
        : base(account)
    {
        Init();
    }

    private void Init()
    {
        Select = new CustomCommand(arg => SelectionCallback?.Invoke(this));
    }
}
