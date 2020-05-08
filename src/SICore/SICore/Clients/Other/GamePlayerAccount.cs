using SIData;
using System;

namespace SICore
{
    public sealed class GamePlayerAccount : GamePersonAccount
    {
        private int _sum = 0;
        private bool _pass = false;
        private bool _inGame = true;

        /// <summary>
        /// Сумма игрока
        /// </summary>
        public int Sum
        {
            get { return _sum; }
            set { _sum = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Может ли жать на кнопку
        /// </summary>
        internal bool CanPress
        {
            get { return _pass; }
            set { _pass = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Находится ли игрок в игре
        /// </summary>
        public bool InGame
        {
            get { return _inGame; }
            set { _inGame = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Ответ игрока
        /// </summary>
        internal string Answer { get; set; };

        /// <summary>
        /// Ответ верен
        /// </summary>
        internal bool AnswerIsRight { get; set; }

        /// <summary>
        /// Ответ заведомо неверен
        /// </summary>
        internal bool AnswerIsWrong { get; set; }

        /// <summary>
        /// Ставка в финале
        /// </summary>
        internal int FinalStake { get; set; }

        /// <summary>
        /// Вспомогательная переменная
        /// </summary>
        internal bool Flag { get; set; }

        /// <summary>
        /// Участвует ли конкретный игрок в торгах на аукционе
        /// </summary>
        internal bool StakeMaking { get; set; }

        /// <summary>
        /// Штраф за пинг (для выравнивания шансов)
        /// </summary>
        internal int PingPenalty { get; set; }

        /// <summary>
        /// Время последней неудачной попытки нажать кнопку
        /// </summary>
        internal DateTime LastBadTryTime { get; set; }

        public GamePlayerAccount(Account account)
            : base(account)
        {
            
        }
    }
}
