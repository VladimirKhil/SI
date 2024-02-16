using SIData;

namespace SI.GameServer.Contract;

/// <summary>
/// Defines a game run request.
/// </summary>
/// <param name="GameSettings">Game options.</param>
/// <param name="PackageInfo">Game package descriptor.</param>
/// <param name="ComputerAccounts">Custom computer accounts information.</param>
public sealed record RunGameRequest(
    GameSettingsCore<AppSettingsCore> GameSettings,
    PackageInfo PackageInfo,
    ComputerAccount[] ComputerAccounts);
