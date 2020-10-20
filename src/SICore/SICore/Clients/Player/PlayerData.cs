using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SICore
{
    /// <summary>
    /// Данные игрока
    /// </summary>
    public sealed class PlayerData: INotifyPropertyChanged
    {
        private CustomCommand _pressGameButton;

        public CustomCommand PressGameButton
        {
            get { return _pressGameButton; }
            set
            {
                if (_pressGameButton != value)
                {
                    _pressGameButton = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SendAnswer { get; set; }

        private CustomCommand _apellate;

        public CustomCommand Apellate
        {
            get { return _apellate; }
            set
            {
                if (_apellate != value)
                {
                    _apellate = value;
                    OnPropertyChanged();
                }
            }
        }

        private CustomCommand _pass;

        public CustomCommand Pass
        {
            get { return _pass; }
            set
            {
                if (_pass != value)
                {
                    _pass = value;
                    OnPropertyChanged();
                }
            }
        }

        internal int Difficulty { get; set; }
        /// <summary>
        /// Можно ли думать над вопросом до истечения времени
        /// </summary>
        internal bool LongThink { get; set; }

        /// <summary>
        /// Знает ли ответ
        /// </summary>
        internal bool KnowsAnswer { get; set; } = false;

        /// <summary>
        /// Уверен ли в ответе
        /// </summary>
        internal bool IsSure { get; set; } = false;

        /// <summary>
        /// Готов ли жать на кнопку
        /// </summary>
        internal bool ReadyToPress { get; set; } = false;

        /// <summary>
        /// Текущая величина смелости
        /// </summary>
        internal int RealBrave { get; set; } = 0;

        internal int DeltaBrave { get; set; } = 0;

        /// <summary>
        /// Текущая скорость реакции
        /// </summary>
        internal int RealSpeed { get; set; } = 0;

        internal int BestBrave { get; set; } = 0;

        /// <summary>
        /// Продолжается ли чтение вопроса
        /// </summary>
        internal bool IsQuestionInProgress { get; set; }

        /// <summary>
        /// Отчёт об игре
        /// </summary>
        public SIReport Report { get; set; }

        private int _numApps;

        public int NumApps
        {
            get { return _numApps; }
            set { _numApps = value; OnPropertyChanged(); }
        }

        private bool myTry;

        /// <summary>
        /// Можно жать на кнопку (чтобы при игре без фальстартов компьютерные игроки соображали помедленнее)
        /// </summary>
        public bool MyTry
        {
            get { return myTry; }
            set { myTry = value; OnPropertyChanged(); }
        }

        public PlayerData()
        {
            _numApps = int.MaxValue;
            Report = new SIReport();
        }

        public event Action PressButton;
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPressButton()
        {
            PressButton?.Invoke();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
