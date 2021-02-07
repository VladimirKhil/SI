using System;

namespace SI.GameServer.Client
{
    public interface IUIThreadExecutor
    {
        void ExecuteOnUIThread(Action action);
    }
}
