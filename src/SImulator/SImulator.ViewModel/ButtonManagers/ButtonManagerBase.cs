using SImulator.Model;
using SImulator.ViewModel.Core;
using System;

namespace SImulator.ViewModel.ButtonManagers
{
    public abstract class ButtonManagerBase: IButtonManager
    {
        public abstract bool Run();
        public abstract void Stop();

        public event Func<GameKey, bool> KeyPressed;

        public event Func<Guid, bool, PlayerInfo> GetPlayerByGuid;

        public event Func<PlayerInfo, bool> PlayerPressed;

        public abstract void Dispose();

        protected bool OnKeyPressed(GameKey key)
        {
            if (KeyPressed != null)
                return KeyPressed(key);

            return false;
        }

        protected PlayerInfo OnGetPlayerByGuid(Guid guid, bool strict)
        {
            if (GetPlayerByGuid != null)
                return GetPlayerByGuid(guid, strict);

            return null;
        }

        protected bool OnPlayerPressed(PlayerInfo player)
        {
            if (PlayerPressed != null)
                return PlayerPressed(player);

            return false;
        }
    }
}
