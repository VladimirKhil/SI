using SICore.Connections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace SICore
{
    /// <summary>
    /// Данные зрителя
    /// </summary>
    public sealed class ViewerData : Data
    {
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
        /// Файл протокола
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
        public string _qtype;

		public bool IsPartial { get; set; }

        public string _atomType = "";
        /// <summary>
        /// Номер текущего атома сценария вопроса
        /// </summary>
        public int AtomIndex { get; set; }

        internal int Winner { get; set; }

        internal int _lastStakerNum = -1;

		private ViewerAccount _me = null;

        public ViewerAccount Me
		{
			get { return _me; }
			internal set
			{
				if (_me != value)
				{
					if (_me is PersonAccount oldPersonAccount)
					{
						oldPersonAccount.IsMe = false;
					}

					_me = value;

					if (_me is PersonAccount personAccount)
					{
						personAccount.IsMe = true;
					}

					OnPropertyChanged();
				}
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
                if (isUpdating)
                    return;

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
            var accounts = new List<ViewerAccount>();
            if (_showMan != null)
                accounts.Add(_showMan);

            AllPersons = accounts.Concat(_players).Concat(_viewers).ToArray();
        }

        public void OnMainPersonsChanged()
        {
            var accounts = new List<PersonAccount>();
            if (_showMan != null)
                accounts.Add(_showMan);

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

        internal void UpdatePlayers()
        {
            OnPropertyChanged(nameof(Players));
        }

        internal void UpdateViewers()
        {
            OnPropertyChanged(nameof(Viewers));
        }

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

        private ViewerAccount[] _allPersons = Array.Empty<ViewerAccount>();

        /// <summary>
        /// Все участники
        /// </summary>
        public ViewerAccount[] AllPersons
        {
            get
            {
                return _allPersons;
            }
            private set
            {
                _allPersons = value;
                OnPropertyChanged();
            }
        }

        private bool isUpdating = false;

        internal void BeginUpdatePersons()
        {
            isUpdating = true;
        }

        internal void EndUpdatePersons()
        {
            isUpdating = false;
            OnMainPersonsChanged();
            OnAllPersonsChanged();
        }

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

        internal void AddToChat(Message message)
        {
            var index = _chatTable.IndexOf(message.Sender);
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

		internal event Action AutoReadyChanged;

        private void OnAutoReadyChanged()
        {
			AutoReadyChanged?.Invoke();
		}
    }
}
