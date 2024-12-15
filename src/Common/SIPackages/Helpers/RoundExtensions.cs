namespace SIPackages.Helpers;

/// <summary>
/// Provides extension methods for rounds.
/// </summary>
public static class RoundExtensions
{
    /// <summary>
    /// Creates a new theme.
    /// </summary>
    /// <param name="round">Round.</param>
    /// <param name="name">Theme name.</param>
    public static Theme CreateTheme(this Round round, string? name)
    {
        var theme = new Theme { Name = name ?? "" };
        round.Themes.Add(theme);
        return theme;
    }
}
