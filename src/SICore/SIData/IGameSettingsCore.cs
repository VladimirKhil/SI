namespace SIData
{
    /// <summary>
    /// Defines game basic settings.
    /// </summary>
    public interface IGameSettingsCore<out T>
        where T: IAppSettingsCore
    {
        /// <summary>
        /// Game host name.
        /// </summary>
        string HumanPlayerName { get; }

        Account Showman { get; }

        Account[] Players { get; }

        Account[] Viewers { get; }

        /// <summary>
        /// Core settings.
        /// </summary>
        T AppSettings { get; }

        bool RandomSpecials { get; }

        string NetworkGameName { get; }

        string NetworkGamePassword { get; }

        /// <summary>
        /// Автоматическая игра
        /// </summary>
        bool IsAutomatic { get; }
    }
}
