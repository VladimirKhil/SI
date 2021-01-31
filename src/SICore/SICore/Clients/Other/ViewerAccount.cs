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
        private bool _isConnected = false;

        /// <summary>
        /// В игре ли
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
            set { if (_isConnected != value) { _isConnected = value; OnPropertyChanged(); } }
        }

        public ViewerAccount(string name, bool isMale, bool isConnected)
            : base(name, isMale)
        {
            _isConnected = isConnected;
        }

        public ViewerAccount(Account account)
            : base(account)
        {

        }

        public ViewerAccount()
        {

        }
    }
}
