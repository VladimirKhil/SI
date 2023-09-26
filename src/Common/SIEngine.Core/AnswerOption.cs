using SIPackages;

namespace SIEngine.Core;

/// <summary>
/// Defines answer option.
/// </summary>
/// <param name="Label">Option label.</param>
/// <param name="Content">Option content.</param>
public sealed record AnswerOption(string Label, ContentItem Content);
