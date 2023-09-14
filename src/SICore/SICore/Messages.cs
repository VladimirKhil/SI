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
    /// Реклама
    /// </summary>
    public const string Ads = "ADS";

    /// <summary>
    /// Ответ
    /// </summary>
    public const string Answer = "ANSWER";

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
    /// Единица сценария вопроса
    /// </summary>
    public const string Atom = "ATOM";

    /// <summary>
    /// Small hint fragment. Displayed separately from the main content.
    /// </summary>
    public const string Atom_Hint = "ATOM_HINT";

    /// <summary>
    /// Дополнительная единица сценария вопроса
    /// </summary>
    public const string Atom_Second = "ATOM_SECOND";

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
    public const string Cat = "CAT";

    /// <summary>
    /// Стоимость Вопроса с секретом
    /// </summary>
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
    public const string Connect = "CONNECT";

    /// <summary>
    /// К игре подключился новый участник
    /// </summary>
    public const string Connected = "CONNECTED";

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
    public const string FalseStart = "FALSESTART";

    /// <summary>
    /// Финальный раунд
    /// </summary>
    public const string FinalRound = "FINALROUND";

    /// <summary>
    /// Ставка в финале
    /// </summary>
    public const string FinalStake = "FINALSTAKE";

    /// <summary>
    /// Начало размышления над финальным вопросом
    /// </summary>
    public const string FinalThink = "FINALTHINK";

    /// <summary>
    /// Информация о том, кто будет первым выбирать вопрос
    /// </summary>
    public const string First = "FIRST";

    /// <summary>
    /// Информация о том, кто будет первым удалять тему в финале
    /// </summary>
    public const string FirstDelete = "FIRSTDELETE";

    /// <summary>
    /// Информация о том, кто будет первым делать ставку
    /// </summary>
    public const string FirstStake = "FIRSTSTAKE";

    /// <summary>
    /// Передача идентификатора игры для подключения
    /// </summary>
    public const string Game = "GAME";

    /// <summary>
    /// Информация об игре для внешнего наблюдателя
    /// </summary>
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
    /// Пометить вопрос
    /// </summary>
    public const string Mark = "MARK";

    /// <summary>
    /// Notifies that the client has loaded the media.
    /// </summary>
    public const string MediaLoaded = "MEDIALOADED";

    /// <summary>
    /// Сменить состояние игры
    /// </summary>
    public const string Move = "MOVE";

    /// <summary>
    /// Выбрать следующего игрока
    /// </summary>
    public const string Next = "NEXT";

    /// <summary>
    /// Выбрать следующего игрока, убирающего тему
    /// </summary>
    public const string NextDelete = "NEXTDELETE";

    /// <summary>
    /// Игра не существует
    /// </summary>
    public const string NoGame = "NOGAME";

    /// <summary>
    /// Удалена финальная тема
    /// </summary>
    public const string Out = "OUT";

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
    public const string PersonApellated = "PERSONAPELLATED";

    /// <summary>
    /// Игрок ответил в финале
    /// </summary>
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
    public const string Picture = "PICTURE";

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
    /// Тип вопроса
    /// </summary>
    public const string QType = "QTYPE";

    /// <summary>
    /// Вопрос
    /// </summary>
    public const string Question = "QUESTION";

    /// <summary>
    /// Заголовок вопроса
    /// </summary>
    public const string QuestionCaption = "QUESTIONCAPTION";

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
    public const string RoundThemes = "ROUNDTHEMES";

    /// <summary>
    /// Задать игрока, выбирающего вопросы
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
    /// Показать табло
    /// </summary>
    public const string ShowTable = "SHOWTABLO";

    /// <summary>
    /// Изменилась стадия игры
    /// </summary>
    public const string Stage = "STAGE";

    /// <summary>
    /// Ставка
    /// </summary>
    public const string Stake = "STAKE";

    /// <summary>
    /// Начать игру
    /// </summary>
    public const string Start = "START";

    /// <summary>
    /// Остановить раунд
    /// </summary>
    public const string Stop = "STOP";

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
    public const string TextShape = "TEXTSHAPE";

    /// <summary>
    /// Тема
    /// </summary>
    public const string Theme = "THEME";

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
    public const string WrongTry = "WRONGTRY";

    /// <summary>
    /// Можно жать на кнопку
    /// </summary>
    public const string YouTry = "YOUTRY";
}
