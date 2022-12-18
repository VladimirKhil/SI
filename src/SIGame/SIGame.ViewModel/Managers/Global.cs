namespace SIGame.ViewModel;

/// <summary>
/// Глобальные функции приложения
/// </summary>
public static class Global
{
    /// <summary>
    /// Папка игровых пакетов
    /// </summary>
    public static string PackagesUri => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

    /// <summary>
    /// Папка изображений игроков
    /// </summary>
    public static string PhotoUri => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photo");
}
