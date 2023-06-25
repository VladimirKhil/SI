using SIData;

namespace SICore;

/// <summary>
/// Defines a game player account.
/// </summary>
public sealed class GamePlayerAccount : GamePersonAccount
{
    private int _sum = 0;
    private bool _pass = false;
    private bool _inGame = true;

    /// <summary>
    /// Player score.
    /// </summary>
    public int Sum
    {
        get => _sum;
        set { _sum = value; OnPropertyChanged(); }
    }

    // TODO: Will be replaced with QuestionPlayState.PossibleAnswerers indicies collection
    /// <summary>
    /// Has the player right to press the button.
    /// </summary>
    internal bool CanPress
    {
        get => _pass;
        set { _pass = value; OnPropertyChanged(); }
    }

    // TODO: Will be replaced with QuestionPlayState.PossibleAnswerers indicies collection and theme deletion strategy
    /// <summary>
    /// Is the player currently being able to play (by game rules, not by connection).
    /// </summary>
    public bool InGame
    {
        get => _inGame;
        set { _inGame = value; OnPropertyChanged(); }
    }

    // TODO: Will be moved to QuestionPlayState
    /// <summary>
    /// Ответ игрока
    /// </summary>
    internal string? Answer { get; set; }

    // TODO: Will be moved to QuestionPlayState
    /// <summary>
    /// Ответ верен
    /// </summary>
    internal bool AnswerIsRight { get; set; }

    // TODO: Will be moved to QuestionPlayState
    /// <summary>
    /// Answer right price factor.
    /// </summary>
    internal double AnswerIsRightFactor { get; set; } = 1.0;

    // TODO: Will be moved to QuestionPlayState
    /// <summary>
    /// Ответ заведомо неверен
    /// </summary>
    internal bool AnswerIsWrong { get; set; }

    // TODO: Will be moved to QuestionPlayState
    /// <summary>
    /// Ставка в финале
    /// </summary>
    internal int FinalStake { get; set; }

    /// <summary>
    /// Вспомогательная переменная
    /// </summary>
    internal bool Flag { get; set; }

    // TODO: Will be moved to Stake strategy implementation
    /// <summary>
    /// Участвует ли конкретный игрок в торгах на аукционе
    /// </summary>
    internal bool StakeMaking { get; set; }

    /// <summary>
    /// Штраф за пинг (для выравнивания шансов)
    /// </summary>
    internal int PingPenalty { get; set; }

    /// <summary>
    /// Time of the last button misfire for the player.
    /// </summary>
    internal DateTime LastBadTryTime { get; set; }

    public GamePlayerAccount(Account account)
        : base(account)
    {
        
    }

    public GamePlayerAccount()
    {

    }
}
