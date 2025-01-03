﻿namespace SICore;

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
    /// Detect the next moving player.
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
    /// Theme information.
    /// </summary>
    Theme,
    
    /// <summary>
    /// Theme information without question play.
    /// </summary>
    ThemeInfo,
    
    /// <summary>
    /// Question start information.
    /// </summary>
    QuestionStartInfo,

    /// <summary>
    /// Show next answer option.
    /// </summary>
    ShowNextAnswerOption,

    /// <summary>
    /// Asking the players to press the button.
    /// </summary>
    AskToTry,
    
    /// <summary>
    /// Waiting for the players to press the button.
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
    /// Просьба к игроку удалить тему в финале
    /// </summary>
    AskToDelete,
    /// <summary>
    /// Ожидание решения игркоа об удалении темы в финале
    /// </summary>
    WaitDelete,
    /// <summary>
    /// Waiting for the players to make hidden stakes.
    /// </summary>
    WaitHiddenStake,
    /// <summary>
    /// Announce hidden answer of next player.
    /// </summary>
    Announce,
    /// <summary>
    /// Объявление ставки игрока в финале
    /// </summary>
    AnnounceStake,

    /// <summary>
    /// Announce stake after right answer.
    /// </summary>
    AnnouncePostStakeWithAnswerOptions,
    
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
    /// Решение игроков о правильности ответа
    /// </summary>
    AppellationDecision,

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
