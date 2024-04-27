namespace SI.GameServer.Contract;

/// <summary>
/// Defines an automatic game run request.
/// </summary>
/// <param name="Culture">Game culture.</param>
public sealed record RunAutoGameRequest(string Culture);
