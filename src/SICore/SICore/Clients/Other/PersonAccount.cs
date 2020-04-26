using SIData;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace SICore
{
    /// <summary>
    /// Основной участник игры
    /// </summary>
    public class PersonAccount : ViewerAccount
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _ready = false;

        /// <summary>
        /// Готов ли участник к игре
        /// </summary>
        [DefaultValue(false)]
        public bool Ready
        {
            get { return _ready; }
            set { if (_ready != value) { _ready = value; OnPropertyChanged(); } }
        }

        private bool _gameStarted = false;

        public bool GameStarted
        {
            get { return _gameStarted; }
            set { if (_gameStarted != value) { _gameStarted = value; OnPropertyChanged(); } }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ICommand _beReadyCommand = null;

        public ICommand BeReadyCommand
        {
            get { return _beReadyCommand; }
            set { if (_beReadyCommand != value) { _beReadyCommand = value; OnPropertyChanged(); } }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ICommand _beUnReadyCommand = null;

        public ICommand BeUnReadyCommand
        {
            get { return _beUnReadyCommand; }
            set { if (_beUnReadyCommand != value) { _beUnReadyCommand = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// Изменить тип
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private CustomCommand _changeType = null;

        public CustomCommand ChangeType
        {
            get { return _changeType; }
            set { if (_changeType != value) { _changeType = value; OnPropertyChanged(); } }
        }

        private bool _isChooser = false;

        public bool IsChooser
        {
            get { return _isChooser; }
            set
            {
                if (_isChooser != value)
                {
                    _isChooser = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Заменить
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private CustomCommand _replace = null;

        public CustomCommand Replace
        {
            get { return _replace; }
            set { if (_replace != value) { _replace = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// Освободить стол (перевести в зрители)
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private CustomCommand _free = null;

        public CustomCommand Free
        {
            get { return _free; }
            set { if (_free != value) { _free = value; OnPropertyChanged(); } }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _replic = "";

        public string Replic
        {
            get { return _replic; }
            set { if (_replic != value) { _replic = value; OnPropertyChanged(); } }
        }

        private Account[] _others;

        public Account[] Others
        {
            get { return _others; }
            set { _others = value; OnPropertyChanged(); }
        }

        public bool IsShowman { get; set; }

        public bool IsMe { get;set; }

        private bool isExtendedMode = false;

        public bool IsExtendedMode
        {
            get { return isExtendedMode; }
            set
            {
                if (isExtendedMode != value)
                {
                    isExtendedMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isDeciding;

        public bool IsDeciding
        {
            get { return _isDeciding; }
            set
            {
                if (_isDeciding != value)
                {
                    _isDeciding = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public PersonAccount(string name, bool sex, bool connected, bool gameStarted)
            : base(name, sex, connected)
        {
            _gameStarted = gameStarted;
        }

        public PersonAccount(Account account)
            : base(account)
        {

        }
    }
}
