using System;

namespace SICore
{
    /// <summary>
    /// Зритель SIGame
    /// </summary>
    public interface IViewerClient : IActor
    {
        IConnector Connector { get; set; }

        /// <summary>
        /// Является ли владельцем сервера
        /// </summary>
        bool IsHost { get; }

        ViewerData MyData { get; }

        IViewer MyLogic { get; }
        string Avatar { get; set; }

        event Action PersonConnected;
        event Action PersonDisconnected;
        event Action<int, string, string> Timer;

        void GetInfo();

        void Pause();

        void Init();

        event Action<IViewerClient> Switch;
        event Action StageChanged;
        event Action<string> Ad;
        event Action<bool> IsPausedChanged;

        event Action OnIsHostChanged;

        void RecreateCommands();
        void Move(object arg);
    }
}
