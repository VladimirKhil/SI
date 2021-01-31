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
        /// Глобальный генератор случайных чисел. Он создаётся на каждого клиента отдельно, поскольку не потокобезопасен
        /// </summary>
        public Random Rand { get; } = new Random();

        /// <summary>
        /// Информация, используемая табло
        /// </summary>
        public TableInfo TInfo { get; } = new TableInfo();

        public object TInfoLock { get; } = new object();

        public int PrevoiusTheme { get; set; } = -1;
        
        public int PreviousQuest { get; set; } = -1;

        /// <summary>
        /// Выбранная тема
        /// </summary>
        public int ThemeIndex { get; set; } = -1;
        
        /// <summary>
        /// Выбранный вопрос
        /// </summary>
        public int QuestionIndex { get; set; } = -1;

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

        internal int CurPriceRight { get; set; }
        
        internal int CurPriceWrong { get; set; }

        /// <summary>
        /// Информация о системных ошибках в игре, которые неплохо бы отправлять автору, но которые не приводят к краху системы
        /// </summary>
        public StringBuilder SystemLog { get; } = new StringBuilder();

        public StringBuilder PersonsUpdateHistory { get; } = new StringBuilder();

        public StringBuilder EventLog { get; } = new StringBuilder();

        protected static string PrintAccount(ViewerAccount viewerAccount) => $"{viewerAccount?.Name}:{viewerAccount?.IsConnected}";

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
