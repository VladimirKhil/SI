namespace SICore;

/// <summary>
/// Describes logging modes.
/// </summary>
public enum LogMode
{
    /// <summary>
    /// System message.
    /// </summary>
    Protocol,
    /// <summary>
    /// Message to log only.
    /// </summary>
    Log,
    /// <summary>
    /// Message to chat only.
    /// </summary>
    Chat
}

/// <summary>
/// Defines game decision types.
/// </summary>
public enum DecisionType
{
    /// <summary>
    /// No decision.
    /// </summary>
    None,

    /// <summary>
    /// Question selection.
    /// </summary>
    QuestionSelection,

    /// <summary>
    /// Player button pressing.
    /// </summary>
    PlayerButtonPressing,

    /// <summary>
    /// Question answerer selection.
    /// </summary>
    QuestionAnswererSelection,

    /// <summary>
    /// Question price selection.
    /// </summary>
    QuestionPriceSelection,

    /// <summary>
    /// Stake making.
    /// </summary>
    StakeMaking,

    /// <summary>
    /// Round theme deleting.
    /// </summary>
    ThemeDeleting,
    
    /// <summary>
    /// Making hidden stake.
    /// </summary>
    HiddenStakeMaking,
    
    /// <summary>
    /// Нажатие на кнопку
    /// </summary>
    Pressing,
    /// <summary>
    /// Выдача ответа
    /// </summary>
    Answering,
    /// <summary>
    /// Проверка правильности ответа
    /// </summary>
    AnswerValidating,

    /// <summary>
    /// Selecting the moving player.
    /// </summary>
    StarterChoosing,

    /// <summary>
    /// Выбор следующего ставящего на Вопросе со ставкой
    /// </summary>
    NextPersonStakeMaking,
    /// <summary>
    /// Выбор следущего игрока, удаляющего тему в финале
    /// </summary>
    NextPersonFinalThemeDeleting,
    
    /// <summary>
    /// Appellating answer.
    /// </summary>
    Appellation,

    /// <summary>
    /// Waiting for the players reviews.
    /// </summary>
    Reporting
}

/// <summary>
/// Defines player states.
/// </summary>
public enum PlayerState
{
    /// <summary>
    /// Обычное
    /// </summary>
    None,

    /// <summary>
    /// Player is answering.
    /// </summary>
    Answering,

    /// <summary>
    /// Проиграл кнопку
    /// </summary>
    Lost,
    /// <summary>
    /// Ответил верно
    /// </summary>
    Right,
    /// <summary>
    /// Ответил неверно
    /// </summary>
    Wrong,
    /// <summary>
    /// Дал ответ в финале
    /// </summary>
    HasAnswered,
    /// <summary>
    /// Спасовал
    /// </summary>
    Pass,
}

/// <summary>
/// Виды ставок
/// </summary>
public enum StakeMode
{
    /// <summary>
    /// Номинал
    /// </summary>
    Nominal,
    /// <summary>
    /// Сумма
    /// </summary>
    Sum,
    /// <summary>
    /// Пас
    /// </summary>
    Pass,
    /// <summary>
    /// Ва-банк
    /// </summary>
    AllIn
}

/// <summary>
/// Describes reasons to stop normal game engine execution.
/// </summary>
public enum StopReason
{
    /// <summary>
    /// No reason. Normal execution.
    /// </summary>
    None,
    /// <summary>
    /// Execution was paused.
    /// </summary>
    Pause,
    /// <summary>
    /// Some required person decision was made.
    /// </summary>
    Decision,
    /// <summary>
    /// Button was hit.
    /// </summary>
    Answer,
    /// <summary>
    /// Appellation was started.
    /// </summary>
    Appellation,
    /// <summary>
    /// Game was moved or round was changed.
    /// </summary>
    Move,
    /// <summary>
    /// Throttling on button hit was started.
    /// </summary>
    Wait
}

public enum MessageTypes
{
    System,
    Special,
    Replic
}

/// <summary>
/// Defines game move directions.
/// </summary>
public enum MoveDirections
{
    /// <summary>
    /// Move one round back.
    /// </summary>
    RoundBack = -2,

    /// <summary>
    /// Move game back.
    /// </summary>
    Back = -1,

    /// <summary>
    /// Move game futher.
    /// </summary>
    Next = 1,

    /// <summary>
    /// Move one round next.
    /// </summary>
    RoundNext = 2,

    /// <summary>
    /// Move to arbitrary round.
    /// </summary>
    Round = 3,
}
