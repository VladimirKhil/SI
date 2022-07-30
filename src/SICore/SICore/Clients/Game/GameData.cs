using SICore.Clients.Game;
using SICore.Network;
using SICore.Results;
using SIData;
using SIPackages;
using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils;

namespace SICore
{
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
        /// Текущий документ пакета
        /// </summary>
        internal SIDocument PackageDoc { get; set; }

        /// <summary>
        /// Текущий пакет
        /// </summary>
        internal Package Package { get; set; }

        /// <summary>
        /// Текущий раунд
        /// </summary>
        public Round Round { get; set; }

        internal int QLength { get; set; }

        /// <summary>
        /// Текущая тема
        /// </summary>
        internal Theme Theme { get; set; }

        /// <summary>
        /// Текущий выбирающий игрок
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
        /// Текущий отвечающий игрок
        /// </summary>
        internal GamePlayerAccount Chooser { get; private set; }

        private int _chooserIndex = -1;

        /// <summary>
        /// Выбирающий вопрос игрок
        /// </summary>
        internal int ChooserIndex
        {
            get { return _chooserIndex; }
            set
            {
                _chooserIndex = value;
                if (value > -1 && value < Players.Count)
                    Chooser = Players[value];
                else if (value == -1)
                    Chooser = null;
                else
                    throw new ArgumentException($"{nameof(value)} {value} must be greater or equal to -1 and less than {Players.Count}!");
            }
        }

        /// <summary>
        /// Ожидаемое решение
        /// </summary>
        public DecisionType Decision { get; set; }

        /// <summary>
        /// Ожидается ли какое-то решение
        /// </summary>
        private bool _isWaiting = false;

        /// <summary>
        /// Текущий вопрос
        /// </summary>
        internal Question Question { get; set; }

        /// <summary>
        /// Тип вопроса
        /// </summary>
        internal QuestionType Type { get; set; }

        private BagCatInfo _catInfo = null;

        public BagCatInfo CatInfo
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
        /// Предыдущий выборщик
        /// </summary>
        internal int ChooserIndexOld = -1;

        private int _appelaerIndex = -1;

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
        /// Итоги игры
        /// </summary>
        internal GameResult GameResultInfo { get; } = new GameResult();

        /// <summary>
        /// Можно ли пометить вопрос
        /// </summary>
        internal bool CanMarkQuestion { get; set; }

        internal int HaveViewedAtom { get; set; }

        /// <summary>
        /// Порядок объявления ставок
        /// </summary>
        public int[] Order { get; set; }

        /// <summary>
        /// Текущий индекс ставящего
        /// </summary>
        public int OrderIndex { get; set; }

        public StringBuilder OrderHistory { get; set; }

        private int _stakerIndex = -1;

        /// <summary>
        /// Делающий ставку
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
        /// История ответов на вопрос (применяется при апелляции)
        /// </summary>
        internal List<AnswerResult> QuestionHistory { get; private set; } = new List<AnswerResult>();

        /// <summary>
        /// Количество полученных ответов на апелляцию
        /// </summary>
        public int AppellationAnswersReceivedCount { get; set; }

        /// <summary>
        /// Количество полученных положительных ответов на апелляцию
        /// </summary>
        public int AppellationAnswersRightReceivedCount { get; set; }

        internal bool IsAnswer { get; set; }

        /// <summary>
        /// Игроки
        /// </summary>
        public List<GamePlayerAccount> Players { get; } = new List<GamePlayerAccount>();

        private GamePersonAccount _showMan = null;

        /// <summary>
        /// Ведущий
        /// </summary>
        internal GamePersonAccount ShowMan
        {
            get => _showMan;
            set
            {
                _showMan = value;
                OnPropertyChanged();

                if (_isUpdating)
                {
                    return;
                }

                BackLink?.LogWarning("Unreachable code? " + Environment.StackTrace);

                OnMainPersonsChanged();
                OnAllPersonsChanged();
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

        public void OnMainPersonsChanged()
        {
            MainPersons = new GamePersonAccount[] { _showMan }.Concat(Players).ToArray();
        }

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
            get
            {
                return _mainPersons;
            }
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

        private bool _isUpdating = false;

        internal void BeginUpdatePersons(string reason)
        {
            _isUpdating = true;

            PersonsUpdateHistory.Append($"Before ({reason}): ").Append(PrintPersons());
        }

        internal void EndUpdatePersons()
        {
            _isUpdating = false;
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

        public ThemeDeletersEnumerator ThemeDeleters { get; internal set; }

        public string Text { get; internal set; }

        /// <summary>
        /// Выводится ли текст вопроса по частям
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

        public string DocumentPath { get; internal set; }

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

        public GameData(IGameManager gameManager) : base(gameManager)
        {

        }
    }
}
