namespace SIData;

/// <summary>
/// Defines an information about a game person.
/// </summary>
public sealed class ConnectionPersonData
{
    /// <summary>
    /// Person name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Person role.
    /// </summary>
    public GameRole Role { get; set; }

    /// <summary>
    /// Is the person connected.
    /// </summary>
    public bool IsOnline { get; set; }
}
