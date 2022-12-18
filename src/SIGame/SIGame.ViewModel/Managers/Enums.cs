using System.ComponentModel.DataAnnotations;

namespace SIGame.ViewModel;

/// <summary>
/// Типы аккаунта
/// </summary>
public enum AccountTypes
{
    /// <summary>
    /// Человек
    /// </summary>
    [Display(Description = "AccountTypes_Human")]
    Human,
    /// <summary>
    /// Компьютер
    /// </summary>
    [Display(Description = "AccountTypes_Computer")]
    Computer
}

/// <summary>
/// Стадия настроек
/// </summary>
public enum SettingsStages
{
    None,
    Human,
    Computer,
    Showman,
    Time,
    PackageStore,
    New
}

/// <summary>
/// Варианты сетевой игры
/// </summary>
public enum NetworkGameType
{
    /// <summary>
    /// Игровой сервер СИ
    /// </summary>
    [Display(Description = "NetworkGameType_NetworkGameServer")]
    GameServer,
    /// <summary>
    /// Прямое подключение
    /// </summary>
    [Display(Description = "NetworkGameType_DirectConnection")]
    DirectConnection
}
