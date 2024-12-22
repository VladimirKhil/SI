namespace SICore;

/// <summary>
/// Defines well-known game messages.
/// </summary>
public static class Messages
{
    /// <summary>
    /// Согласие на подключение
    /// </summary>
    public const string Accepted = "ACCEPTED";

    /// <summary>
    /// User action error message.
    /// </summary>
    public const string ActionError = "ACTION_ERROR";

    /// <summary>
    /// Реклама
    /// </summary>
    public const string Ads = "ADS";

    /// <summary>
    /// Ответ
    /// </summary>
    public const string Answer = "ANSWER";

    /// <summary>
    /// Contains all players answers.
    /// </summary>
    public const string Answers = "ANSWERS";

    /// <summary>
    /// Answer version. Denotes a preliminary answer printed by player.
    /// </summary>
    public const string AnswerVersion = "ANSWER_VERSION";

    /// <summary>
    /// Требование апелляции
    /// </summary>
    public const string Apellate = "APELLATE";

    /// <summary>
    /// Marks apellation option flag.
    /// </summary>
    [IdempotencyRequired]
    public const string ApellationEnabled = "APELLATION_ENABLES";

    /// <summary>
    /// Asks to select a player.
    /// </summary>
    public const string AskSelectPlayer = "ASK_SELECT_PLAYER";

    /// <summary>
    /// Asks to make a stake.
    /// </summary>
    public const string AskStake = "ASK_STAKE";

    /// <summary>
    /// Asks to validate player's answer.
    /// </summary>
    public const string AskValidate = "ASK_VALIDATE";

    /// <summary>
    /// Notifies the game that the client has completed viewing the media content.
    /// </summary>
    public const string Atom = "ATOM";

    /// <summary>
    /// Small hint fragment. Displayed separately from the main content.
    /// </summary>
    public const string Atom_Hint = "ATOM_HINT";

    /// <summary>
    /// Person's avatar.
    /// </summary>
    [IdempotencyRequired]
    public const string Avatar = "AVATAR";

    /// <summary>
    /// Забанить участника
    /// </summary>
    public const string Ban = "BAN";

    /// <summary>
    /// Person has been banned.
    /// </summary>
    public const string Banned = "BANNED";

    /// <summary>
    /// List of banned persons.
    /// </summary>
    [IdempotencyRequired]
    public const string BannedList = "BANNEDLIST";

    /// <summary>
    /// Время блокировки Игровой кнопки
    /// </summary>
    [IdempotencyRequired]
    public const string ButtonBlockingTime = "BUTTON_BLOCKING_TIME";

    /// <summary>
    /// Отмена ожидания решения
    /// </summary>
    public const string Cancel = "CANCEL";

    /// <summary>
    /// Вопрос с секретом
    /// </summary>
    [Obsolete("Use AskSelectPlayer")]
    public const string Cat = "CAT";

    /// <summary>
    /// Стоимость Вопроса с секретом
    /// </summary>
    [Obsolete("Use AskStake")]
    public const string CatCost = "CATCOST";

    /// <summary>
    /// Изменение суммы участника
    /// </summary>
    public const string Change = "CHANGE";

    /// <summary>
    /// Выбор вопроса
    /// </summary>
    public const string Choice = "CHOICE";

    /// <summary>
    /// Сделанный выбор вопроса
    /// </summary>
    public const string Choose = "CHOOSE";

    /// <summary>
    /// Default computer players.
    /// </summary>
    [IdempotencyRequired]
    public const string ComputerAccounts = "COMPUTERACCOUNTS";

    /// <summary>
    /// Изменение конфигурации игры
    /// </summary>
    public const string Config = "CONFIG";

    /// <summary>
    /// Подключение к серверу
    /// </summary>
    [Obsolete("Remove after switching to SIGame 8")]
    public const string Connect = "CONNECT";

    /// <summary>
    /// К игре подключился новый участник
    /// </summary>
    public const string Connected = "CONNECTED";

    /// <summary>
    /// Table content.
    /// </summary>
    public const string Content = "CONTENT";

    /// <summary>
    /// Appends content to existing table content.
    /// </summary>
    public const string ContentAppend = "CONTENT_APPEND";

    /// <summary>
    /// Defines content shape without providing real content.
    /// </summary>
    public const string ContentShape = "CONTENT_SHAPE";

    /// <summary>
    /// Updates content state.
    /// </summary>
    public const string ContentState = "CONTENT_STATE";

    /// <summary>
    /// Удаление темы
    /// </summary>
    public const string Delete = "DELETE";

    /// <summary>
    /// Участник отключился от игры
    /// </summary>
    public const string Disconnected = "DISCONNECTED";

    /// <summary>
    /// Нельзя нажимать на кнопку
    /// </summary>
    public const string EndTry = "ENDTRY";

    /// <summary>
    /// Фальстарты
    /// </summary>
    [IdempotencyRequired]
    [Obsolete]
    public const string FalseStart = "FALSESTART";

    /// <summary>
    /// Ставка в финале
    /// </summary>
    [Obsolete("Use AskStake")]
    public const string FinalStake = "FINALSTAKE";

    /// <summary>
    /// Начало размышления над финальным вопросом
    /// </summary>
    public const string FinalThink = "FINALTHINK";

    /// <summary>
    /// Showman should give a move to a player.
    /// </summary>
    [Obsolete("Use AskSelectPlayer")]
    public const string First = "FIRST";

    /// <summary>
    /// Информация о том, кто будет первым удалять тему в финале
    /// </summary>
    [Obsolete("Use AskSelectPlayer")]
    public const string FirstDelete = "FIRSTDELETE";

    /// <summary>
    /// Информация о том, кто будет первым делать ставку
    /// </summary>
    [Obsolete("Use AskSelectPlayer")]
    public const string FirstStake = "FIRSTSTAKE";

    /// <summary>
    /// Передача идентификатора игры для подключения
    /// </summary>
    public const string Game = "GAME";

    /// <summary>
    /// Информация об игре для внешнего наблюдателя
    /// </summary>
    [Obsolete("Remove after switching to SIGame 8")]
    public const string GameInfo = "GAMEINFO";

    /// <summary>
    /// Game metadata: game name, package name, contact uri.
    /// </summary>
    [IdempotencyRequired]
    public const string GameMetadata = "GAMEMETADATA";

    /// <summary>
    /// Темы всей игры
    /// </summary>
    public const string GameThemes = "GAMETHEMES";

    /// <summary>
    /// Подсказка ведущему
    /// </summary>
    public const string Hint = "HINT";

    /// <summary>
    /// Имя хоста
    /// </summary>
    [IdempotencyRequired]
    public const string Hostname = "HOSTNAME";

    /// <summary>
    /// Нажатие на кнопку
    /// </summary>
    public const string I = "I";

    /// <summary>
    /// Информация об игре и её участниках
    /// </summary>
    public const string Info = "INFO";

    /// <summary>
    /// Информация об игре и её участниках (расширенная)
    /// </summary>
    [IdempotencyRequired]
    public const string Info2 = "INFO2";

    /// <summary>
    /// Верен ли ответ участника
    /// </summary>
    public const string IsRight = "ISRIGHT";

    /// <summary>
    /// Выгнать участника
    /// </summary>
    public const string Kick = "KICK";

    /// <summary>
    /// Table layout.
    /// </summary>
    public const string Layout = "LAYOUT";

    /// <summary>
    /// Пометить вопрос
    /// </summary>
    public const string Mark = "MARK";

    /// <summary>
    /// Notifies that the client has loaded the media.
    /// </summary>
    public const string MediaLoaded = "MEDIALOADED";

    /// <summary>
    /// Notifies that the client has preloaded the media.
    /// </summary>
    public const string MediaPreloaded = "MEDIA_PRELOADED";

    /// <summary>
    /// Сменить состояние игры
    /// </summary>
    public const string Move = "MOVE";

    /// <summary>
    /// Denotes that the person could be moved during the game.
    /// </summary>
    public const string Moveable = "MOVEABLE";

    /// <summary>
    /// Выбрать следующего игрока
    /// </summary>
    [Obsolete("Use SelectPlayer")]
    public const string Next = "NEXT";

    /// <summary>
    /// Выбрать следующего игрока, убирающего тему
    /// </summary>
    [Obsolete("Use SelectPlayer")]
    public const string NextDelete = "NEXTDELETE";

    /// <summary>
    /// Игра не существует
    /// </summary>
    public const string NoGame = "NOGAME";

    /// <summary>
    /// Game options.
    /// </summary>
    [IdempotencyRequired]
    public const string Options = "OPTIONS";

    /// <summary>
    /// Удалена финальная тема
    /// </summary>
    public const string Out = "OUT";

    /// <summary>
    /// Package info.
    /// </summary>
    public const string Package = "PACKAGE";

    /// <summary>
    /// Идентификатор пакета
    /// </summary>
    public const string PackageId = "PACKAGEID";

    /// <summary>
    /// Логотип пакета
    /// </summary>
    [Obsolete]
    public const string PackageLogo = "PACKAGELOGO";

    /// <summary>
    /// Пас на вопросе
    /// </summary>
    [Obsolete("Use PlayerState instead")]
    public const string Pass = "PASS";

    /// <summary>
    /// Пауза в игре
    /// </summary>
    public const string Pause = "PAUSE";

    /// <summary>
    /// Информация об ответе игрока
    /// </summary>
    public const string Person = "PERSON";

    /// <summary>
    /// Игрок принял решение по апелляции
    /// </summary>
    [Obsolete("Use PlayerState instead")]
    public const string PersonApellated = "PERSONAPELLATED";

    /// <summary>
    /// Игрок ответил в финале
    /// </summary>
    [Obsolete("Use PlayerState instead")]
    public const string PersonFinalAnswer = "PERSONFINALANSWER";

    /// <summary>
    /// Игрок сделал ставку в финале
    /// </summary>
    public const string PersonFinalStake = "PERSONFINALSTAKE";

    /// <summary>
    /// Ставка игрока
    /// </summary>
    public const string PersonStake = "PERSONSTAKE";

    /// <summary>
    /// Картинка участника
    /// </summary>
    [IdempotencyRequired]
    [Obsolete]
    public const string Picture = "PICTURE";

    /// <summary>
    /// Asks or receives game PIN.
    /// </summary>
    public const string Pin = "PIN";

    /// <summary>
    /// Sets player state.
    /// </summary>
    public const string PlayerState = "PLAYER_STATE";

    /// <summary>
    /// Скорость чтения вопроса
    /// </summary>
    [IdempotencyRequired]
    public const string ReadingSpeed = "READINGSPEED";

    /// <summary>
    /// Готовность к игре
    /// </summary>
    public const string Ready = "READY";

    /// <summary>
    /// Отчёт об игре
    /// </summary>
    public const string Report = "REPORT";

    /// <summary>
    /// Продолжить отыгрыш атома сценария
    /// </summary>
    public const string Resume = "RESUME";

    /// <summary>
    /// Правильный ответ (простой)
    /// </summary>
    public const string RightAnswer = "RIGHTANSWER";

    /// <summary>
    /// Notifies about complex answer start.
    /// </summary>
    public const string RightAnswerStart = "RIGHT_ANSWER_START";

    /// <summary>
    /// Тип вопроса
    /// </summary>
    public const string QType = "QTYPE";

    /// <summary>
    /// Вопрос
    /// </summary>
    public const string Question = "QUESTION";

    /// <summary>
    /// Question right and wrong answers.
    /// </summary>
    public const string QuestionAnswers = "QUESTION_ANSWERS";

    /// <summary>
    /// Заголовок вопроса
    /// </summary>
    public const string QuestionCaption = "QUESTIONCAPTION";

    /// <summary>
    /// Question has ended.
    /// </summary>
    public const string QuestionEnd = "QUESTION_END";

    /// <summary>
    /// Реплика игры/участника
    /// </summary>
    public const string Replic = "REPLIC";

    /// <summary>
    /// Round media content links.
    /// </summary>
    public const string RoundContent = "ROUNDCONTENT";

    /// <summary>
    /// Package rounds names. Only rounds with at least one question are taken into account.
    /// </summary>
    [IdempotencyRequired]
    public const string RoundsNames = "ROUNDSNAMES";

    /// <summary>
    /// Round themes names.
    /// </summary>
    [IdempotencyRequired]
    [Obsolete]
    public const string RoundThemes = "ROUNDTHEMES";

    /// <summary>
    /// Round themes names.
    /// </summary>
    [IdempotencyRequired]
    public const string RoundThemes2 = "ROUND_THEMES2";

    /// <summary>
    /// Select player.
    /// </summary>
    public const string SelectPlayer = "SELECT_PLAYER";

    /// <summary>
    /// Gives move to player.
    /// </summary>
    public const string SetChooser = "SETCHOOSER";

    /// <summary>
    /// Set person as host.
    /// </summary>
    public const string SetHost = "SETHOST";

    /// <summary>
    /// Set game join mode.
    /// </summary>
    public const string SetJoinMode = "SETJOINMODE";

    /// <summary>
    /// Sets stake.
    /// </summary>
    public const string SetStake = "SET_STAKE";

    /// <summary>
    /// Показать табло
    /// </summary>
    public const string ShowTable = "SHOWTABLO";

    /// <summary>
    /// Изменилась стадия игры
    /// </summary>
    public const string Stage = "STAGE";

    /// <summary>
    /// Lightweight version of stage info (does not require to be announced).
    /// </summary>
    public const string StageInfo = "STAGE_INFO";

    /// <summary>
    /// Ставка
    /// </summary>
    [Obsolete("Use AskStake, SetStake")]
    public const string Stake = "STAKE";

    /// <summary>
    /// Stake info.
    /// </summary>
    [Obsolete("Use AskStake")]
    public const string Stake2 = "STAKE2";

    /// <summary>
    /// Начать игру
    /// </summary>
    public const string Start = "START";

    /// <summary>
    /// Остановить раунд
    /// </summary>
    public const string Stop = "STOP";

    /// <summary>
    /// Stops question play.
    /// </summary>
    public const string StopPlay = "STOP_PLAY";

    /// <summary>
    /// Фоновая картинка студии
    /// </summary>
    public const string Studia = "STUDIA";

    /// <summary>
    /// Информация о суммах участников
    /// </summary>
    public const string Sums = "SUMS";

    /// <summary>
    /// Contains information about round table cells.
    /// </summary>
    [IdempotencyRequired]
    public const string Table = "TABLO2";

    /// <summary>
    /// Форма текста
    /// </summary>
    [Obsolete("Use ContentShape")]
    public const string TextShape = "TEXTSHAPE";

    /// <summary>
    /// Current theme.
    /// </summary>
    public const string Theme = "THEME";

    /// <summary>
    /// Theme comments.
    /// </summary>
    public const string ThemeComments = "THEME_COMMENTS";

    /// <summary>
    /// Изменения таймера
    /// </summary>
    [IdempotencyRequired]
    public const string Timer = "TIMER";

    /// <summary>
    /// Время вышло
    /// </summary>
    public const string Timeout = "TIMEOUT";

    /// <summary>
    /// Toggles (removes or restores a question).
    /// </summary>
    public const string Toggle = "TOGGLE";

    /// <summary>
    /// Можно нажимать на кнопку
    /// </summary>
    public const string Try = "TRY";

    /// <summary>
    /// Unban a person.
    /// </summary>
    public const string Unban = "UNBAN";

    /// <summary>
    /// Person has been unbanned.
    /// </summary>
    public const string Unbanned = "UNBANNED";

    /// <summary>
    /// Defines a user-level error.
    /// </summary>
    public const string UserError = "USER_ERROR";

    /// <summary>
    /// Answer validation decision.
    /// </summary>
    public const string Validate = "VALIDATE";

    /// <summary>
    /// Необходимо провалидировать ответ игрока
    /// </summary>
    public const string Validation = "VALIDATION";

    /// <summary>
    /// Answer validation.
    /// </summary>
    public const string Validation2 = "VALIDATION2";

    /// <summary>
    /// Победитель
    /// </summary>
    public const string Winner = "WINNER";

    /// <summary>
    /// Список неправильных ответов на вопрос
    /// </summary>
    public const string Wrong = "WRONG";

    /// <summary>
    /// Проиграл кнопку
    /// </summary>
    [Obsolete("Use PlayerState instead")]
    public const string WrongTry = "WRONGTRY";

    /// <summary>
    /// Можно жать на кнопку
    /// </summary>
    public const string YouTry = "YOUTRY";
}
