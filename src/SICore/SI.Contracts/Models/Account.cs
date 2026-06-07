namespace SI.Contracts.Models;

/// <summary>
/// Defines a person account.
/// </summary>
public sealed class Account
{
    /// <summary>
    /// Account name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Account type.
    /// </summary>
    public AccountType Type { get; set; }

    /// <summary>
    /// Account gender.
    /// </summary>
    public Gender Gender { get; set; }

    /// <summary>
    /// Account avatar URI.
    /// </summary>
    public string AvatarUri { get; set; } = "";
}
