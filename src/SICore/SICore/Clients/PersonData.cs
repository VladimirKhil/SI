using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SICore
{
    /// <summary>
    /// Общие данные для игрока и ведущего
    /// </summary>
    public sealed class PersonData : INotifyPropertyChanged
    {
        public CustomCommand SendPass { get; set; }
        public CustomCommand SendStake { get; set; }
        public CustomCommand SendVabank { get; set; }
        public CustomCommand SendNominal { get; set; }

        public ICommand SendCatCost { get; set; }
        public ICommand SendFinalStake { get; set; }

        private StakeInfo _stakeInfo = null;

        public StakeInfo StakeInfo
        {
            get { return _stakeInfo; }
            set
            {
                if (_stakeInfo != value)
                {
                    _stakeInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        internal bool[] Var { get; set; } = new bool[4] { false, false, false, false };

        private string[] _right = System.Array.Empty<string>();
        private string[] _wrong = System.Array.Empty<string>();

        /// <summary>
        /// Верные ответы
        /// </summary>
        public string[] Right
        {
            get { return _right; }
            set
            {
                _right = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Неверные ответы
        /// </summary>
        public string[] Wrong
        {
            get => _wrong;
            set
            {
                _wrong = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Имя игрока, чей ответ валидируется
        /// </summary>
        public string ValidatorName { get; set; }

        private string _answer = "";

        /// <summary>
        /// Ответ игрока
        /// </summary>
        public string Answer
        {
            get { return _answer; }
            set { _answer = value; OnPropertyChanged(); }
        }

        private bool _areAnswersShown = true;

        public bool AreAnswersShown
        {
            get { return _areAnswersShown; }
            set
            {
                if (_areAnswersShown != value)
                {
                    _areAnswersShown = value;
                    OnPropertyChanged();
                }
            }
        }

        private ICommand _isRight = null;

        public ICommand IsRight
        {
            get { return _isRight; }
            set { _isRight = value; OnPropertyChanged(); }
        }

        private ICommand _isWrong = null;

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand IsWrong
        {
            get { return _isWrong; }
            set { _isWrong = value; OnPropertyChanged(); }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
