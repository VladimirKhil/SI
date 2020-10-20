using SIData;
using SIUI.Model;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace SICore
{
    /// <summary>
    /// Данные клиента
    /// </summary>
    public abstract class Data : ITimeProvider, INotifyPropertyChanged
    {
        public IGameManager BackLink { get; set; }

        /// <summary>
        /// Глобальный генератор случайных чисел
        /// </summary>
        public static readonly Random Rand = new Random();

        /// <summary>
        /// Информация, используемая табло
        /// </summary>
        public TableInfo TInfo { get; } = new TableInfo();
        public object TInfoLock { get; } = new object();

        public int PrevoiusTheme = -1, PreviousQuest = -1;
        /// <summary>
        /// Выбор игрока
        /// </summary>
        public int ThemeIndex = -1, QuestionIndex = -1;

        /// <summary>
        /// Объект синхронизации для choiceTheme и choiceQuest
        /// </summary>
        public object ChoiceLock { get; } = new object();

        private GameStage _stage = GameStage.Before;

        /// <summary>
        /// Состояние игры
        /// </summary>
        public GameStage Stage
        {
            get { return _stage; }
            set { _stage = value; OnPropertyChanged(); }
        }

        private int _roundTime = 0;
        /// <summary>
        /// Время раунда
        /// </summary>
        public int RoundTime
        {
            get { return _roundTime; }
            set { if (_roundTime != value) { _roundTime = value; OnPropertyChanged(); } }
        }

        private int _pressingTime = 0;
        /// <summary>
        /// Время на нажатие на кнопку
        /// </summary>
        public int PressingTime
        {
            get { return _pressingTime; }
            set { if (_pressingTime != value) { _pressingTime = value; OnPropertyChanged(); } }
        }

        private int _thinkingTime = 0;
        /// <summary>
        /// Время для принятия решения
        /// </summary>
        public int ThinkingTime
        {
            get { return _thinkingTime; }
            set { if (_thinkingTime != value) { _thinkingTime = value; OnPropertyChanged(); } }
        }

        internal int CurPriceRight, CurPriceWrong = 0;

        /// <summary>
        /// Информация о системных ошибках в игре, которые неплохо бы отправлять автору, но которые не приводят к краху системы
        /// </summary>
        public StringBuilder SystemLog { get; set; } = new StringBuilder();

        public virtual void OnAddString(string person, string text, LogMode mode)
        {
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
