namespace SICore;

/// <summary>
/// Defines game states.
/// </summary>
public enum Tasks
{
    /// <summary>
    /// Нет задачи
    /// </summary>
    NoTask,
    /// <summary>
    /// Двигаться дальше
    /// </summary>
    MoveNext,
    /// <summary>
    /// Начало игры
    /// </summary>
    StartGame,
    /// <summary>
    /// Объявление пакета
    /// </summary>
    Package,
    /// <summary>
    /// Объявление раунда
    /// </summary>
    Round,
    /// <summary>
    /// Выяснение того, кто начнёт раунд
    /// </summary>
    AskFirst,
    /// <summary>
    /// Ожидание выяснения того, кто начнёт раунд
    /// </summary>
    WaitFirst,
    /// <summary>
    /// Просьба выбрать вопрос
    /// </summary>
    AskToChoose,
    /// <summary>
    /// Ожидание выбора вопроса
    /// </summary>
    WaitChoose,
    /// <summary>
    /// Объявление темы
    /// </summary>
    Theme,
    /// <summary>
    /// Объявление типа вопроса
    /// </summary>
    QuestionType,
    /// <summary>
    /// Предложение жать на кнопку
    /// </summary>
    AskToTry,
    /// <summary>
    /// Ожидание нажатия на кнопку
    /// </summary>
    WaitTry,
    /// <summary>
    /// Объявление источников и комментариев вопроса
    /// </summary>
    QuestSourComm,
    /// <summary>
    /// Выяснение ответа игрока
    /// </summary>
    AskAnswer,
    AskAnswerDeferred,
    /// <summary>
    /// Ожидание ответа игрока
    /// </summary>
    WaitAnswer,
    /// <summary>
    /// Выяснение правильности ответа у ведущего
    /// </summary>
    AskRight,
    /// <summary>
    /// Ожидание выяснения правильности ответа ведущим
    /// </summary>
    WaitRight,
    /// <summary>
    /// Вывод частичного текста вопроса
    /// </summary>
    PrintPartial,
    /// <summary>
    /// Продолжить отыгрыш вопроса
    /// </summary>
    ContinueQuestion,

    /// <summary>
    /// Ask to select question answerer.
    /// </summary>
    AskToSelectQuestionAnswerer,

    /// <summary>
    /// Waiting for question answerer selection.
    /// </summary>
    WaitQuestionAnswererSelection,

    /// <summary>
    /// Ask player to select question price.
    /// </summary>
    AskToSelectQuestionPrice,

    /// <summary>
    /// Waiting for question price selection.
    /// </summary>
    WaitSelectQuestionPrice,

    /// <summary>
    /// Ожидание решения ведущего о том, кто будет следующим делать ставку
    /// </summary>
    WaitNext,
    /// <summary>
    /// Выяснение ставки следующего игрока
    /// </summary>
    AskStake,
    /// <summary>
    /// Ожидание решения игрока о своей ставке
    /// </summary>
    WaitStake,
    /// <summary>
    /// Объявление игрока, играющего Вопрос со ставкой
    /// </summary>
    PrintAuctPlayer,
    /// <summary>
    /// Объявление финального состава
    /// </summary>
    PrintFinal,
    /// <summary>
    /// Просьба к игроку удалить тему в финале
    /// </summary>
    AskToDelete,
    /// <summary>
    /// Ожидание решения игркоа об удалении темы в финале
    /// </summary>
    WaitDelete,
    /// <summary>
    /// Ожидание решения ведущего о том, кто будет следующим удалять тему в финале
    /// </summary>
    WaitNextToDelete,
    /// <summary>
    /// Объявление темы финального раунда
    /// </summary>
    AnnounceFinalTheme,
    /// <summary>
    /// Ожидание ставки игроков в финале
    /// </summary>
    WaitFinalStake,
    /// <summary>
    /// Announce hidden answer of next player.
    /// </summary>
    Announce,
    /// <summary>
    /// Объявление ставки игрока в финале
    /// </summary>
    AnnounceStake,
    /// <summary>
    /// End current round.
    /// </summary>
    EndRound,
    /// <summary>
    /// Завершение игры
    /// </summary>
    EndGame,
    /// <summary>
    /// Объявление победителя игры
    /// </summary>
    Winner,
    /// <summary>
    /// Прощание с игроками
    /// </summary>
    GoodLuck,
    /// <summary>
    /// Сообщить об апелляции
    /// </summary>
    PrintAppellation,
    /// <summary>
    /// Ожидание решения игроков об апелляции
    /// </summary>
    WaitAppellationDecision,
    /// <summary>
    /// Принять апелляцию
    /// </summary>
    CheckAppellation,
    /// <summary>
    /// Ожидаем, пока игроки напишут отчёт
    /// </summary>
    WaitReport,
    /// <summary>
    /// Автоматическая игра
    /// </summary>
    AutoGame
}

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
/// Dialog modes.
/// </summary>
public enum DialogModes
{
    /// <summary>
    /// No dialog.
    /// </summary>
    None,

    /// <summary>
    /// Answer validation.
    /// </summary>
    AnswerValidation,

    /// <summary>
    /// Person score update.
    /// </summary>
    ChangeSum,

    /// <summary>
    /// Player answer.
    /// </summary>
    Answer,

    /// <summary>
    /// Setting question price.
    /// </summary>
    CatCost,

    /// <summary>
    /// Question stake selection.
    /// </summary>
    Stake,

    /// <summary>
    /// Question final stake selection.
    /// </summary>
    FinalStake,

    /// <summary>
    /// Game report.
    /// </summary>
    Report,

    /// <summary>
    /// Game management.
    /// </summary>
    Manage
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
    /// Удаление темы в финале
    /// </summary>
    FinalThemeDeleting,
    /// <summary>
    /// Выставление ставки в финале
    /// </summary>
    FinalStakeMaking,
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
    /// Выбор игрока, начинающего раунд
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
    /// Решение игроков о правильности ответа
    /// </summary>
    AppellationDecision,

    /// <summary>
    /// Waiting for the players reviews.
    /// </summary>
    Reporting
}

/// <summary>
/// Состояние игрока
/// </summary>
public enum PlayerState
{
    /// <summary>
    /// Обычное
    /// </summary>
    None,
    /// <summary>
    /// Выиграл кнопку
    /// </summary>
    Press,
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
    Pass
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

[Flags]
public enum GamesFilter
{
    NoFilter = 0,
    New = 1,
    Sport = 2,
    Tv = 4,
    NoPassword = 8,
    All = 15
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
