using SICore.Connections;
using SIData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace SICore
{
    /// <summary>
    /// Данные зрителя
    /// </summary>
    public sealed class ViewerData : Data
    {
        public string ServerAddress { get; set; }
        public string ServerPublicUrl { get; set; }
        public string[] ContentPublicUrls { get; set; }

        private DialogModes _dialogMode = DialogModes.None;

        public DialogModes DialogMode
        {
            get { return _dialogMode; }
            set { _dialogMode = value; OnPropertyChanged(); }
        }

        private ICommand _atomViewed;

        public ICommand AtomViewed
        {
            get { return _atomViewed; }
            set
            {
                if (_atomViewed != value)
                {
                    _atomViewed = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ProtocolPath { get; set; }

        private ICommand _kick;

        public ICommand Kick
        {
            get { return _kick; }
            set
            {
                if (_kick != value)
                {
                    _kick = value;
                    OnPropertyChanged();
                }
            }
        }

        private ICommand _ban;

        public ICommand Ban
        {
            get { return _ban; }
            set
            {
                if (_ban != value)
                {
                    _ban = value;
                    OnPropertyChanged();
                }
            }
        }

        private CustomCommand _forceStart;

        public CustomCommand ForceStart
        {
            get { return _forceStart; }
            set
            {
                if (_forceStart != value)
                {
                    _forceStart = value;
                    OnPropertyChanged();
                }
            }
        }

        private CustomCommand _addTable;

        public CustomCommand AddTable
        {
            get { return _addTable; }
            set
            {
                if (_addTable != value)
                {
                    _addTable = value;
                    OnPropertyChanged();
                }
            }
        }

        private CustomCommand _deleteTable;

        public CustomCommand DeleteTable
        {
            get { return _deleteTable; }
            set
            {
                if (_deleteTable != value)
                {
                    _deleteTable = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _studia;

        public string Studia
        {
            get { return _studia; }
            set
            {
                _studia = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Файл протокола (логов)
        /// </summary>
        public StreamWriter ProtocolWriter;

        public PersonAccount Speaker { get; set; }

        private string _printedText = "";

        public string PrintedText
        {
            get { return _printedText; }
            set
            {
                if (_printedText != value)
                {
                    _printedText = value;
                    OnPropertyChanged();

                    SendMessageCommand.CanBeExecuted = value.Length > 0;
                }
            }
        }

        private string _hint = "";

        public string Hint
        {
            get { return _hint; }
            set { _hint = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Тип вопроса
        /// </summary>
        public string QuestionType { get; set; }

        public bool IsPartial { get; set; }

        public string AtomType { get; set; } = "";
        /// <summary>
        /// Номер текущего атома сценария вопроса
        /// </summary>
        public int AtomIndex { get; set; }

        internal int Winner { get; set; }

        internal int LastStakerIndex { get; set; } = -1;

        public string Name { get; internal set; }

        public ViewerAccount Me
        {
            get
            {
                AllPersons.TryGetValue(Name, out var me);
                return me;
            }
        }

        public bool IsChatOpened { get; set; } = true;

        /// <summary>
        /// Адрес изображения участника
        /// </summary>
        internal string Picture { get; set; }

        private bool _isPlayer;

        public bool IsPlayer
        {
            get { return _isPlayer; }
            set
            {
                if (_isPlayer != value)
                {
                    _isPlayer = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _iReady = false;

        /// <summary>
        /// Готов ли участник к игре
        /// </summary>
        public bool IReady
        {
            get { return _iReady; }
            set { _iReady = value; OnPropertyChanged(); }
        }

        internal string Sound
        {
            set { BackLink.PlaySound(value); }
        }

        internal bool FalseStart { get; set; } = true;

        public CustomCommand SendMessageCommand { get; set; }

        public PersonData PersonDataExtensions { get; private set; } = new PersonData();
        public PlayerData PlayerDataExtensions { get; private set; } = new PlayerData();
        public ShowmanData ShowmanDataExtensions { get; private set; } = new ShowmanData();

        /// <summary>
        /// Делегат, организующий отправку сообщения
        /// </summary>
        public Action<string> MessageSending { get; set; }
        public event Action<string, string, LogMode> StringAdding;

        private List<PlayerAccount> _players = new List<PlayerAccount>();

        /// <summary>
        /// Игроки
        /// </summary>
        public List<PlayerAccount> Players
        {
            get { return _players; }
            internal set
            {
                if (_players != value)
                {
                    _players = value;
                    OnPropertyChanged();
                }
            }
        }

        private PersonAccount _showMan = null;

        /// <summary>
        /// Ведущий
        /// </summary>
        public PersonAccount ShowMan
        {
            get { return _showMan; }
            set
            {
                _showMan = value;
                OnPropertyChanged();
                if (_isUpdating)
                {
                    return;
                }

                OnMainPersonsChanged();
                OnAllPersonsChanged();
            }
        }

        private bool _showMainTimer;

        public bool ShowMainTimer
        {
            get { return _showMainTimer; }
            set
            {
                if (_showMainTimer != value)
                {
                    _showMainTimer = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public void OnAllPersonsChanged()
        {
            PersonsUpdateHistory.Append("Update: ").Append(PrintPersons()).AppendLine();

            var accounts = new List<ViewerAccount>();
            if (_showMan != null)
            {
                accounts.Add(_showMan);
            }

            try
            {
                AllPersons = accounts.Concat(_players).Concat(_viewers)
                    .Where(account => account.IsConnected)
                    .ToDictionary(account => account.Name);
            }
            catch (ArgumentException exc)
            {
                throw new Exception($"OnAllPersonsChanged error: {PersonsUpdateHistory}", exc);
            }

            if (!AllPersons.ContainsKey(Name))
            {
                throw new Exception($"!AllPersons.ContainsKey({Name})! {string.Join(",", AllPersons.Keys)} {PersonsUpdateHistory}");
            }
        }

        public void OnMainPersonsChanged()
        {
            var accounts = new List<PersonAccount>();
            if (_showMan != null)
            {
                accounts.Add(_showMan);
            }

            MainPersons = accounts.Concat(_players).ToArray();
        }

        private List<ViewerAccount> _viewers = new List<ViewerAccount>();

        /// <summary>
        /// Зрители
        /// </summary>
        public List<ViewerAccount> Viewers
        {
            get { return _viewers; }
            internal set
            {
                if (_viewers != value)
                {
                    _viewers = value;
                    OnPropertyChanged();
                }
            }
        }

        internal void UpdatePlayers() => OnPropertyChanged(nameof(Players));

        internal void UpdateViewers() => OnPropertyChanged(nameof(Viewers));

        private PersonAccount[] _mainPersons = Array.Empty<PersonAccount>();

        /// <summary>
        /// Главные участники
        /// </summary>
        internal PersonAccount[] MainPersons
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

        private Dictionary<string, ViewerAccount> _allPersons = new Dictionary<string, ViewerAccount>();

        /// <summary>
        /// Все участники
        /// </summary>
        public Dictionary<string, ViewerAccount> AllPersons
        {
            get => _allPersons;
            private set
            {
                _allPersons = value;
                OnPropertyChanged();
            }
        }

        private bool _isUpdating = false;

        internal void BeginUpdatePersons(string reason = null)
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

        private string PrintPersons() => new StringBuilder()
            .Append("Showman: ").Append(PrintAccount(ShowMan)).AppendLine()
            .Append("Players: ").Append(string.Join(", ", Players.Select(PrintAccount))).AppendLine()
            .Append("Viewers: ").Append(string.Join(", ", Viewers.Select(PrintAccount))).AppendLine()
            .ToString();

        public ViewerData()
        {
            Winner = -1;

            SendMessageCommand = new CustomCommand(item =>
            {
                MessageSending?.Invoke(PrintedText);
                PrintedText = "";
            }) { CanBeExecuted = false };
        }

        private readonly List<string> _chatTable = new List<string>();

        /// <summary>
        /// Add mesage to the game chat
        /// </summary>
        /// <param name="message"></param>
        internal void AddToChat(Message message)
        {
            var index = _chatTable.IndexOf(message.Sender);
            // if user is not present in user list, add him
            if (index == -1)
            {
                _chatTable.Add(message.Sender);
                index = _chatTable.Count - 1;
            }

            OnAddString(message.Sender, message.Text, LogMode.Chat + index);
        }

        public override void OnAddString(string person, string text, LogMode mode)
        {
            StringAdding?.Invoke(person, text, mode);
        }

        private bool _autoReady = false;

        public bool AutoReady
        {
            get { return _autoReady; }
            set
            {
                _autoReady = value;
                OnAutoReadyChanged();
            }
        }

        public string PackageId { get; internal set; }
        public int ButtonBlockingTime { get; internal set; } = 3;

        internal event Action AutoReadyChanged;

        private void OnAutoReadyChanged()
        {
            AutoReadyChanged?.Invoke();
        }
    }
}
