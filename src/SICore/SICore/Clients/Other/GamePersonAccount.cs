using SIData;
using System.ComponentModel;
using System.Diagnostics;

namespace SICore
{
    public class GamePersonAccount : Account
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
            set { _ready = value; OnPropertyChanged(); }
        }

        public GamePersonAccount(Account account)
            : base(account)
        {

        }
    }
}
