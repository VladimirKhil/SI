using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;
using System;

namespace SImulator.ViewModel.ButtonManagers
{
    /// <summary>
    /// Supports players buttons.
    /// </summary>
    public interface IButtonManager : IAsyncDisposable
    {
        /// <summary>
        /// Enables players buttons.
        /// </summary>
        bool Run();

        /// <summary>
        /// Disables players buttons.
        /// </summary>
        void Stop();

        event Func<GameKey, bool> KeyPressed;
        event Func<PlayerInfo, bool> PlayerPressed;
        event Func<string, bool, PlayerInfo> GetPlayerById;
    }
}
