namespace SIPackages.Core;

/// <summary>
/// Defines well-known step parameter types.
/// </summary>
public static class StepParameterTypes
{
    /// <summary>
    /// Parameter value is a string.
    /// </summary>
    public const string Simple = "simple";

    /// <summary>
    /// Parameter value is a collection of content items.
    /// </summary>
    public const string Content = "content";

    /// <summary>
    /// Parameter value is a collection of other parameters.
    /// </summary>
    public const string Group = "group";

    /// <summary>
    /// Parameter value is a set of numbers. Is is a subtype of <see cref="Simple" /> type.
    /// </summary>
    /// <remarks>
    /// The value could be a simple number of could have a range syntax [minimum;maximum]/step or [minimum;maximum].
    /// </remarks>
    public const string NumberSet = "numberSet";
}
