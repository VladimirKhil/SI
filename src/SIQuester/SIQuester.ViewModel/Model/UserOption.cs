namespace SIQuester.ViewModel.Model;

/// <summary>
/// Defines a user-selectable option.
/// </summary>
public sealed class UserOption
{
    /// <summary>
    /// Option title.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// Option description.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Option handler.
    /// </summary>
    public Action Callback { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="UserOption" /> class.
    /// </summary>
    /// <param name="title">Option title.</param>
    /// <param name="description">Option description.</param>
    /// <param name="callback">Option handler.</param>
    public UserOption(string title, string description, Action callback)
    {
        Title = title;
        Description = description;
        Callback = callback;
    }
}
