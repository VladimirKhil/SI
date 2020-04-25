using SIData;
using SIPackages;
using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using SICore.Clients.Game;
using System.Text;
using SICore.Results;

namespace SICore
{
    /// <summary>
    /// Данные игры
    /// </summary>
    public sealed class GameData : Data
    {
        /// <summary>
        /// Настройки игры
        /// </summary>
        public IGameSettingsCore<AppSettingsCore> Settings;

        /// <summary>
        /// Текущий документ пакета
        /// </summary>
        internal SIDocument PackageDoc;

        /// <summary>
        /// Текущий пакет
        /// </summary>
        internal Package Package;

        /// <summary>
        /// Текущий раунд
        /// </summary>
        public Round Round { get; set; }

        internal int QLength;

        /// <summary>
        /// Текущая тема
        /// </summary>
        internal Theme Theme;

        /// <summary>
        /// Текущий выбирающий игрок
        /// </summary>
        internal GamePlayerAccount ActivePlayer = null;

        /// <summary>
        /// Текущий отвечающий игрок
        /// </summary>
        internal GamePlayerAccount Answerer { get; private set; }

        private int _answererIndex;

        internal int AnswererIndex
        {
            get { return _answererIndex; }
            set
            {
                _answererIndex = value;
                if (value > -1 && value < Players.Count)
                    Answerer = Players[value];
                else if (value == -1)
                    Answerer = null;
                else
                    throw new ArgumentException($"{nameof(value)} {value} must be greater or equal to -1 and less than {Players.Count}!");
            }
        }

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
        internal QuestionType Type;

        private BagCatInfo _catInfo = null;

        public BagCatInfo CatInfo
        {
            get { return _catInfo; }
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
        /// Время начала раунда
        /// </summary>
        internal DateTime[] TimerStartTime { get; set; } = new DateTime[3] { DateTime.Now, DateTime.Now, DateTime.Now };

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
            get { return _appelaerIndex; }
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
        internal List<string> UsedWrongVersions = new List<string>();

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
                    WaitingMessage = true;
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
            get { return _stakerIndex; }
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
        internal int Stake = -1;

        /// <summary>
        /// Пошёл ли кто-нибудь ва-банк
        /// </summary>
        internal bool AllIn = false;

        /// <summary>
        /// Завершаем ли раунд
        /// </summary>
        internal bool IsRoundEnding = false;

        /// <summary>
        /// Допустимые варианты ставок: 
        /// - номинал
        /// - сумма
        /// - пас
        /// - ва-банк
        /// </summary>
        internal bool[] StakeVariants = new bool[4];

        /// <summary>
        /// Тип ставки
        /// </summary>
        internal StakeMode? StakeType;

        /// <summary>
        /// Сумма ставки
        /// </summary>
        internal int StakeSum = -1;

        /// <summary>
        /// Получено ли решение ведущего
        /// </summary>
        public bool ShowmanDecision { get; set; }

        /// <summary>
        /// Играется ли вопрос
        /// </summary>
        internal bool isQuestionPlaying = false;

        internal int TabloInformStage = 0;
        internal object TabloInformStageLock = new object();

        /// <summary>
        /// Количество ставящих в финале
        /// </summary>
        internal int NumOfStakers = 0;

        /// <summary>
        /// Прав ли игрок
        /// </summary>
        internal bool PlayerIsRight = false;

        /// <summary>
        /// История ответов на вопрос (применяется при апелляции)
        /// </summary>
        internal List<AnswerResult> QuestionHistory { get; private set; } = new List<AnswerResult>();

		/// <summary>
		/// Количество полученных ответов на апелляцию
		/// </summary>
        public int ApelAnswersReceivedCount { get; set; }
        /// <summary>
		/// Количество полученных положительных ответов на апелляцию
		/// </summary>
        public int ApelAnswersRightReceivedCount { get; set; }

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
            get { return _showMan; }
            set
            {
                _showMan = value;
                OnPropertyChanged();
                if (_isUpdating)
                    return;

                OnMainPersonsChanged();
                OnAllPersonsChanged();
            }
        }

        public void OnAllPersonsChanged()
        {
            AllPersons = new Account[] { _showMan }.Concat(Players).Concat(Viewers).ToArray();
        }

        public void OnMainPersonsChanged()
        {
            MainPersons = new GamePersonAccount[] { _showMan }.Concat(Players).ToArray();
        }

        /// <summary>
        /// Зрители
        /// </summary>
        public List<Account> Viewers { get; } = new List<Account>();

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

        private Account[] _allPersons = Array.Empty<Account>();

        /// <summary>
        /// Все участники
        /// </summary>
        internal Account[] AllPersons
        {
            get => _allPersons;
            private set
            {
                _allPersons = value;
                OnPropertyChanged();
            }
        }

        private bool _isUpdating = false;

        internal void BeginUpdatePersons()
        {
            _isUpdating = true;
        }

        internal void EndUpdatePersons()
        {
            _isUpdating = false;
            OnMainPersonsChanged();
            OnAllPersonsChanged();
        }

        public int ReportsCount { get; set; }
        public int AcceptedReports { get; set; }

        internal List<string> OpenedFiles = new List<string>();
        public bool AnnounceAnswer;
        public bool AllowApellation;

        public object TaskLock { get; } = new object();

        public IShare Share { get; set; }

		/// <summary>
		/// Дочитан ли вопрос
		/// </summary>
        public bool IsQuestionFinished { get; set; }

        public int AtomTime { get; set; }

		public string AtomType { get; set; }

        public DateTime AtomStart { get; set; }

        public int MoveDirection { get; set; }
        /// <summary>
        /// Устная игра
        /// </summary>
        public bool IsOral { get; set; }

        public bool IsOralNow { get; set; }
		public int Penalty { get; internal set; }
		public DateTime PenaltyStartTime { get; internal set; }

		public bool IsDeferringAnswer { get; internal set; }

		public ThemeDeletersEnumerator ThemeDeleters { get; internal set; }
		public string Text { get; internal set; }
		/// <summary>
		/// Выводится ли текст вопроса по частям
		/// </summary>
		public bool IsPartial { get; internal set; }
		public int TextLength { get; internal set; }

		public bool IsThinking { get; internal set; }
		public double TimeThinking { get; internal set; }
        [Obsolete]
		public DateTime StartTryTime { get; internal set; }

		public bool MoveNextBlocked { get; set; }
	}
}
