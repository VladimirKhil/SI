namespace SIPackages.Helpers;

/// <summary>
/// Provides package helper methods.
/// </summary>
public static class PackageExtensions
{
    /// <summary>
    /// Creates a new round.
    /// </summary>
    /// <param name="package">Package.</param>
    /// <param name="type">Round type.</param>
    /// <param name="name">Round name.</param>
    public static Round CreateRound(this Package package, string type, string? name)
    {
        var round = new Round
        {
            Name = name ?? (package.Rounds.Count + 1).ToString(),
            Type = type
        };

        package.Rounds.Add(round);
        return round;
    }
}
