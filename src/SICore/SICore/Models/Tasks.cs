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
    /// Plays the round theme.
    /// </summary>
    RoundTheme,
    
    /// <summary>
    /// Detect the next moving player.
    /// </summary>
    AskFirst,
    /// <summary>
    /// Ожидание выяснения того, кто начнёт раунд
    /// </summary>
    WaitFirst,

    /// <summary>
    /// Ask to select question on game table.
    /// </summary>
    AskToSelectQuestion,

    /// <summary>
    /// Ожидание выбора вопроса
    /// </summary>
    WaitChoose,

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
    /// Display information after question.
    /// </summary>
    QuestionPostInfo,

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
    /// Announce the stakes winner.
    /// </summary>
    AnnounceStakesWinner,

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
    /// Start appellation process.
    /// </summary>
    StartAppellation,
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
