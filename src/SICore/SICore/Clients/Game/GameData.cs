using SICore.Clients.Game;
using SICore.Results;
using SIData;
using SIPackages;
using SIPackages.Core;
using System.Text;
using Utils;

namespace SICore;

/// <summary>
/// Defines a game data.
/// </summary>
public sealed class GameData : Data
{
    /// <summary>
    /// Настройки игры
    /// </summary>
    public IGameSettingsCore<AppSettingsCore> Settings { get; set; }

    /// <summary>
    /// Game package document.
    /// </summary>
    internal SIDocument PackageDoc { get; set; }

    /// <summary>
    /// Game package.
    /// </summary>
    internal Package Package { get; set; }

    /// <summary>
    /// Currently playing round.
    /// </summary>
    public Round? Round { get; set; }

    internal int QLength { get; set; }

    /// <summary>
    /// Currently playing theme.
    /// </summary>
    internal Theme Theme { get; set; }

    /// <summary>
    /// Player currently making a decision.
    /// </summary>
    internal GamePlayerAccount ActivePlayer { get; set; }

    /// <summary>
    /// Current answerer info.
    /// </summary>
    internal GamePlayerAccount Answerer { get; private set; }

    private int _answererIndex;

    /// <summary>
    /// Current answerer index.
    /// </summary>
    internal int AnswererIndex
    {
        get => _answererIndex;
        set
        {
            _answererIndex = value;

            if (value > -1 && value < Players.Count)
            {
                Answerer = Players[value];
            }
            else if (value == -1)
            {
                Answerer = null;
            }
            else
            {
                throw new ArgumentException($"{nameof(value)} {value} must be greater or equal to -1 and less than {Players.Count}!");
            }
        }
    }

    /// <summary>
    /// Index of possible answerer.
    /// </summary>
    public int PendingAnswererIndex { get; set; }

    /// <summary>
    /// Player having a turn.
    /// </summary>
    internal GamePlayerAccount? Chooser { get; private set; }

    private int _chooserIndex = -1;

    /// <summary>
    /// Index of player having a turn.
    /// </summary>
    internal int ChooserIndex
    {
        get => _chooserIndex;
        set
        {
            _chooserIndex = value;

            if (value > -1 && value < Players.Count)
            {
                Chooser = Players[value];
            }
            else if (value == -1)
            {
                Chooser = null;
            }
            else
            {
                throw new ArgumentException($"{nameof(value)} {value} must be greater or equal to -1 and less than {Players.Count}!");
            }
        }
    }

    /// <summary>
    /// Current decision being awaited.
    /// </summary>
    public DecisionType Decision { get; set; }

    /// <summary>
    /// Is any decision being awaited now.
    /// </summary>
    private bool _isWaiting = false;

    /// <summary>
    /// Currently playing question.
    /// </summary>
    internal Question Question { get; set; }

    /// <summary>
    /// Currently playing question type.
    /// </summary>
    internal QuestionType? Type { get; set; }

    private NumberSet? _catInfo = null;

    public NumberSet? CatInfo
    {
        get => _catInfo;
        set
        {
            if (_catInfo != value)
            {
                _catInfo = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Организатор игры
    /// </summary>
    internal string HostName { get; set; }

    /// <summary>
    /// Timer start time.
    /// </summary>
    internal DateTime[] TimerStartTime { get; set; } = new DateTime[3] { DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow };

    /// <summary>
    /// Время начала паузы
    /// </summary>
    internal DateTime PauseStartTime { get; set; }

    /// <summary>
    /// Index of player that has a turn before.
    /// </summary>
    internal int ChooserIndexOld = -1;

    private int _appelaerIndex = -1;

    /// <summary>
    /// Index of player who's answer is being apellated.
    /// </summary>
    public int AppelaerIndex
    {
        get => _appelaerIndex;
        set
        {
            if (value < -1 && value >= Players.Count)
            {
                throw new ArgumentException($"{nameof(value)} {value} must be greater or equal to -1 and less than {Players.Count}!");
            }

            _appelaerIndex = value;
        }
    }

    public bool IsAppelationForRightAnswer { get; set; }

    public string AppellationSource { get; set; }

    /// <summary>
    /// Писать ли сообщение ожидания
    /// </summary>
    internal bool WaitingMessage = true;

    /// <summary>
    /// Отвеченные неверные версии
    /// </summary>
    internal List<string> UsedWrongVersions = new();

    /// <summary>
    /// Показывает, были ли уже выведены данные о теме
    /// </summary>
    internal bool[] ThemeInfo;

    /// <summary>
    /// Ожидается ли решение
    /// </summary>
    internal bool IsWaiting
    {
        set
        {
            _isWaiting = value;
            if (_isWaiting)
            {
                WaitingMessage = true;
            }
        }
        get
        {
            return _isWaiting;
        }
    }

    /// <summary>
    /// Game results.
    /// </summary>
    internal GameResult GameResultInfo { get; } = new();

    /// <summary>
    /// Can a question be marked (maybe it contains an error).
    /// </summary>
    internal bool CanMarkQuestion { get; set; }

    /// <summary>
    /// Count of players which have viewed media atom already.
    /// </summary>
    internal int HaveViewedAtom { get; set; }

    /// <summary>
    /// Player indicies in the order of making stakes.
    /// </summary>
    public int[] Order { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Index in the <see cref="Order" /> of player currently making a stake.
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// History of making stakes in current question play.
    /// </summary>
    public StringBuilder OrderHistory { get; } = new();

    private int _stakerIndex = -1;

    /// <summary>
    /// Index of player making a stake.
    /// </summary>
    public int StakerIndex
    {
        get => _stakerIndex;
        set
        {
            if (value < -1 && value >= Players.Count)
            {
                throw new ArgumentException($"{nameof(value)} {value} must be greater or equal to -1 and less than {Players.Count}!");
            }

            _stakerIndex = value;
        }
    }

    /// <summary>
    /// Текущая ставка
    /// </summary>
    internal int Stake { get; set; } = -1;

    /// <summary>
    /// Пошёл ли кто-нибудь ва-банк
    /// </summary>
    internal bool AllIn { get; set; } = false;

    /// <summary>
    /// Завершаем ли раунд
    /// </summary>
    internal bool IsRoundEnding { get; set; } = false;

    /// <summary>
    /// Допустимые варианты ставок: 
    /// - номинал
    /// - сумма
    /// - пас
    /// - ва-банк
    /// </summary>
    internal bool[] StakeVariants { get; set; } = new bool[4];

    /// <summary>
    /// Тип ставки
    /// </summary>
    internal StakeMode? StakeType { get; set; }

    /// <summary>
    /// Сумма ставки
    /// </summary>
    internal int StakeSum { get; set; } = -1;

    /// <summary>
    /// Получено ли решение ведущего
    /// </summary>
    public bool ShowmanDecision { get; set; }

    /// <summary>
    /// Играется ли вопрос
    /// </summary>
    internal bool IsQuestionPlaying { get; set; }

    internal int TableInformStage { get; set; }

    internal Lock TableInformStageLock { get; } = new Lock(nameof(TableInformStageLock));

    /// <summary>
    /// Количество ставящих в финале
    /// </summary>
    internal int NumOfStakers { get; set; }

    /// <summary>
    /// Прав ли игрок
    /// </summary>
    internal bool PlayerIsRight { get; set; }

    /// <summary>
    /// History of answers to the current question.
    /// </summary>
    internal List<AnswerResult> QuestionHistory { get; } = new();

    /// <summary>
    /// Number of awaited appellation votes.
    /// </summary>
    public int AppellationAwaitedVoteCount { get; set; }

    /// <summary>
    /// Number of total appellation votes.
    /// </summary>
    public int AppellationTotalVoteCount { get; set; }

    /// <summary>
    /// Number of positive appellation votes.
    /// </summary>
    public int AppellationPositiveVoteCount { get; set; }

    /// <summary>
    /// Number of negative appellation votes.
    /// </summary>
    public int AppellationNegativeVoteCount { get; set; }

    internal bool IsAnswer { get; set; }

    /// <summary>
    /// Game players info.
    /// </summary>
    public List<GamePlayerAccount> Players { get; } = new();

    private GamePersonAccount _showMan;

    /// <summary>
    /// Game showman.
    /// </summary>
    internal GamePersonAccount ShowMan
    {
        get => _showMan;
        set
        {
            _showMan = value;
            OnPropertyChanged();
        }
    }

    private string PrintPersons() => new StringBuilder()
        .Append("Showman: ").Append(PrintAccount(ShowMan)).AppendLine()
        .Append("Players: ").Append(string.Join(", ", Players.Select(PrintAccount))).AppendLine()
        .Append("Viewers: ").Append(string.Join(", ", Viewers.Select(PrintAccount))).AppendLine()
        .ToString();

    public void OnAllPersonsChanged()
    {
        try
        {
            AllPersons = new ViewerAccount[] { _showMan }
                .Concat(Players)
                .Concat(Viewers)
                .Where(a => a.IsConnected)
                .ToDictionary(a => a.Name);
        }
        catch (ArgumentException exc)
        {
            throw new Exception($"OnAllPersonsChanged error: {PersonsUpdateHistory}", exc);
        }

        PersonsUpdateHistory.Append($"Update: ").Append(PrintPersons());
    }

    public void OnMainPersonsChanged() => MainPersons = new GamePersonAccount[] { _showMan }.Concat(Players).ToArray();

    /// <summary>
    /// Зрители
    /// </summary>
    public List<ViewerAccount> Viewers { get; } = new List<ViewerAccount>();

    private GamePersonAccount[] _mainPersons = Array.Empty<GamePersonAccount>();

    /// <summary>
    /// Главные участники
    /// </summary>
    internal GamePersonAccount[] MainPersons
    {
        get => _mainPersons;
        private set
        {
            _mainPersons = value;
            OnPropertyChanged();
        }
    }

    private Dictionary<string, ViewerAccount> _allPersons = new();

    /// <summary>
    /// Все участники
    /// </summary>
    internal Dictionary<string, ViewerAccount> AllPersons
    {
        get => _allPersons;
        private set
        {
            _allPersons = value;
            OnPropertyChanged();
        }
    }

    internal void BeginUpdatePersons(string reason) =>
        PersonsUpdateHistory.AppendLine("===").Append($"Before ({reason}): ").Append(PrintPersons());

    internal void EndUpdatePersons()
    {
        OnMainPersonsChanged();
        OnAllPersonsChanged();
    }

    public int ReportsCount { get; set; }

    public int AcceptedReports { get; set; }

    internal HashSet<string> OpenedFiles { get; set; } = new();

    public bool AnnounceAnswer { get; set; }

    public bool AllowAppellation { get; set; }

    internal Lock TaskLock { get; } = new Lock(nameof(TaskLock));

    public IShare Share { get; set; }

    /// <summary>
    /// Дочитан ли вопрос
    /// </summary>
    public bool IsQuestionFinished { get; set; }

    public int AtomTime { get; set; }

    public string AtomType { get; set; }

    public DateTime AtomStart { get; set; }

    /// <summary>
    /// Game move direction.
    /// </summary>
    public MoveDirections MoveDirection { get; set; }

    /// <summary>
    /// Устная игра
    /// </summary>
    public bool IsOral { get; set; }

    /// <summary>
    /// Может ли ведущий сейчас принять решение за игрока в устной игре
    /// </summary>
    public bool IsOralNow { get; set; }

    /// <summary>
    /// Штраф за хороший пинг
    /// </summary>
    public int Penalty { get; internal set; }

    public DateTime PenaltyStartTime { get; internal set; }

    /// <summary>
    /// A flag indicating that the game waits a little before accepting an answerer.
    /// </summary>
    public bool IsDeferringAnswer { get; internal set; }

    /// <summary>
    /// Enumerates players that delete final themes.
    /// </summary>
    public ThemeDeletersEnumerator? ThemeDeleters { get; internal set; }

    public string Text { get; internal set; }

    /// <summary>
    /// Is question text being printed partially.
    /// </summary>
    public bool IsPartial { get; internal set; }

    public bool MediaOk { get; internal set; }

    public int TextLength { get; internal set; }

    /// <summary>
    /// Marks thinking time when the border around question is shrinking.
    /// </summary>
    public bool IsThinking { get; internal set; }

    public bool IsThinkingPaused { get; internal set; }

    /// <summary>
    /// Accumulates time passed in thinking on question (border shrinking) mode, 0.1 s units.
    /// </summary>
    public double TimeThinking { get; internal set; }

    public bool MoveNextBlocked { get; set; }

    public bool IsPlayingMedia { get; set; }

    public bool IsPlayingMediaPaused { get; set; }

    public string? DocumentPath { get; internal set; }

    public int ThemeIndexToDelete { get; set; } = -1;

    /// <summary>
    /// Round index to move to.
    /// </summary>
    public int TargetRoundIndex { get; internal set; }

    public RoundInfo[] Rounds { get; internal set; } = Array.Empty<RoundInfo>();

    /// <summary>
    /// Counts notifications shown in the game.
    /// Allow to prevent showing an unlimited number of notifications in the game.
    /// </summary>
    internal int OversizedMediaNotificationsCount { get; set; }

    public IMedia PackageLogo { get; internal set; }

    /// <summary>
    /// Index of player called for negative appellation.
    /// </summary>
    public int AppellationCallerIndex { get; internal set; } = -1;

    public GameData(IGameManager gameManager, GamePersonAccount showman) : base(gameManager)
    {
        _showMan = showman;
    }
}
