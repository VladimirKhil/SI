namespace SIUI.ViewModel;

/// <summary>
/// Defines content item view model.
/// </summary>
/// <param name="Type">Content type.</param>
/// <param name="Value">Text value or content uri.</param>
/// <param name="TextSpeed">Text reading speed.</param>
/// <param name="OriginalValue">Original content value.</param>
public sealed record ContentViewModel(ContentType Type, string Value, double TextSpeed = 0.0, string? OriginalValue = null);
