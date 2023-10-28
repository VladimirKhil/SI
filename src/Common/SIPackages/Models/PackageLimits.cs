namespace SIPackages.Models;

/// <summary>
/// Defines limits for all package elements.
/// </summary>
public sealed class PackageLimits
{
    /// <summary>
    /// Common collection per element count.
    /// </summary>
    public int CollectionCount { get; set; } = 10;

    /// <summary>
    /// Package round count.
    /// </summary>
    public int RoundCount { get; set; } = 50;

    /// <summary>
    /// Round theme count.
    /// </summary>
    public int ThemeCount { get; set; } = 30;

    /// <summary>
    /// Theme question count.
    /// </summary>
    public int QuestionCount { get; set; } = 30;

    /// <summary>
    /// Script step count.
    /// </summary>
    public int StepCount { get; set; } = 30;

    /// <summary>
    /// Parameter count.
    /// </summary>
    public int ParameterCount { get; set; } = 30;

    /// <summary>
    /// Content item count.
    /// </summary>
    public int ContentItemCount { get; set; } = 30;

    /// <summary>
    /// Common text length.
    /// </summary>
    public int TextLength { get; set; } = 350;

    /// <summary>
    /// Content value length.
    /// </summary>
    public int ContentValueLength { get; set; } = 1500;
}
