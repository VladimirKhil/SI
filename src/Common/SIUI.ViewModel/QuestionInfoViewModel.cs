using SIUI.Model;

namespace SIUI.ViewModel;

/// <summary>
/// Defines question information view model.
/// </summary>
public sealed class QuestionInfoViewModel : ViewModelBase<QuestionInfo>
{
    public const int InvalidPrice = -1;

    private QuestionInfoStages _state = QuestionInfoStages.None;

    /// <summary>
    /// Question price.
    /// </summary>
    public int Price
    {
        get => _model.Price;
        set { _model.Price = value; OnPropertyChanged(); }
    }

    public QuestionInfoStages State
    {
        get => _state;
        set { _state = value; OnPropertyChanged(); }
    }

    public QuestionInfoViewModel()
    {
        
    }

    public QuestionInfoViewModel(QuestionInfo questionInfo) : this() => _model = questionInfo;

    internal async Task SilentFlashOutAsync()
    {
        await Task.Delay(500);

        _state = QuestionInfoStages.None;
        Price = InvalidPrice;
    }
}
