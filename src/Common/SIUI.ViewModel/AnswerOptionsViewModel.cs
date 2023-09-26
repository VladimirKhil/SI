namespace SIUI.ViewModel;

/// <summary>
/// Defines answer options view model.
/// </summary>
public sealed class AnswerOptionsViewModel
{
    /// <summary>
    /// Answer options.
    /// </summary>
    public ItemViewModel[] Options { get; set; } = Array.Empty<ItemViewModel>();
}
