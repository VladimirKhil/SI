namespace SIData;

/// <summary>
/// Defines a computer account info.
/// </summary>
public sealed class ComputerAccountInfo
{
    /// <summary>
    /// Account avatar.
    /// </summary>
    public FileKey Picture { get; set; }

    /// <summary>
    /// Account data.
    /// </summary>
    public ComputerAccount Account { get; set; }
}
