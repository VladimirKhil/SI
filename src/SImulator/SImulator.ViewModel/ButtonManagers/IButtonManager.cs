using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;
using System;

namespace SImulator.ViewModel.ButtonManagers
{
    /// <summary>
    /// Объект, способный обрабатывать нажатия кнопок игроками
    /// </summary>
    public interface IButtonManager: IDisposable
    {
        /// <summary>
        /// Активировать кнопки
        /// </summary>
        bool Run();
        /// <summary>
        /// Деактивировать кнопки
        /// </summary>
        void Stop();

        event Func<GameKey, bool> KeyPressed;
        event Func<PlayerInfo, bool> PlayerPressed;
        event Func<Guid, bool, PlayerInfo> GetPlayerByGuid;
    }
}
