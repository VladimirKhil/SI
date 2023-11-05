namespace SIUI.ViewModel;

/// <summary>
/// Defines content item view model.
/// </summary>
/// <param name="Type">Content type.</param>
/// <param name="Value">Text value or content uri.</param>
/// <param name="Weight">Relative content weight (used for calculating content space size).</param>
/// <param name="TextSpeed">Text reading speed.</param>
public sealed record ContentViewModel(ContentType Type, string Value, double Weight = 1.0, double TextSpeed = 0.0);
