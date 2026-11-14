using SI.Contracts;
using SI.Contracts.Models;

namespace SI.GameServer.Contract;

/// <summary>
/// Defines a game run request based on contract settings.
/// </summary>
/// <param name="RoomSettings">Room settings.</param>
/// <param name="RulesSettings">Rules settings.</param>
/// <param name="TimeSettings">Time settings.</param>
/// <param name="Language">Game language.</param>
/// <param name="PackageInfo">Game package descriptor.</param>
/// <param name="ComputerAccounts">Custom computer accounts information.</param>
public sealed record RunGameRequest2(
    RoomSettings RoomSettings,
    RulesSettings RulesSettings,
    TimeSettings TimeSettings,
    string Language,
    PackageInfo PackageInfo,
    ComputerAccount[] ComputerAccounts);
