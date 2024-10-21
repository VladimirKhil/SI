namespace SICore.Contracts;

/// <summary>
/// Allows to generate PINs.
/// </summary>
public interface IPinHelper
{
    /// <summary>
    /// Gets the current PIN.
    /// </summary>
    int? Pin { get; }

    /// <summary>
    /// Generates a new PIN or uses a previously generated one.
    /// </summary>
    int GeneratePin();
}
