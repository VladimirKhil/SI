using SIData;
using System.Diagnostics;

namespace SICore
{
    /// <summary>
    /// Аккаунт с информацией о присутствии в игре
    /// </summary>
    public class ViewerAccount : Account
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _connected = false;

        /// <summary>
        /// В игре ли
        /// </summary>
        public bool Connected
        {
            get { return _connected; }
            set { if (_connected != value) { _connected = value; OnPropertyChanged(); } }
        }

        public ViewerAccount(string name, bool sex, bool connected)
            : base(name, sex)
        {
            _connected = connected;
        }

        public ViewerAccount(Account account)
            : base(account)
        {

        }
    }
}
