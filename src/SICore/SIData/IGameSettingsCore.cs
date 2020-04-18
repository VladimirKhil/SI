namespace SIData
{
    public interface IGameSettingsCore<out T>
        where T: IAppSettingsCore
    {
        string HumanPlayerName { get; }

        Account Showman { get; }
        Account[] Players { get; }
        Account[] Viewers { get; }

		/// <summary>
		/// Настройки приложения
		/// </summary>
        T AppSettings { get; }

        bool RandomSpecials { get; }
        string NetworkGameName { get; }
        string NetworkGamePassword { get; }
        bool AllowViewers { get; }

		/// <summary>
		/// Автоматическая игра
		/// </summary>
		bool IsAutomatic { get; }
	}
}
