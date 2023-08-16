namespace SIQuester.ViewModel.Configuration;

/// <summary>
/// Defines application options.
/// </summary>
public sealed class AppOptions
{
    /// <summary>
    /// Options configuration section name.
    /// </summary>
    public static readonly string ConfigurationSectionName = "SIQuester";

    /// <summary>
    /// Upgrade new packages to new format.
    /// </summary>
    public bool UpgradeNewPackages { get; set; }

    /// <summary>
    /// Upgrade opened packages to new format.
    /// </summary>
    public bool UpgradeOpenedPackages { get; set; }
}
