namespace SIQuester.ViewModel.Configuration;

/// <summary>
/// Defines application options.
/// </summary>
public sealed class AppOptions
{
    /// <summary>
    /// Options configuration setion name.
    /// </summary>
    public static readonly string ConfigurationSectionName = "SIQuester";

    /// <summary>
    /// Upgrade opened packages to new format.
    /// </summary>
    public bool UpgradePackages { get; set; }
}
