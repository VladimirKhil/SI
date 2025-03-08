using SIPackages;

namespace SIQuester.ViewModel;

public sealed class CommentsViewModel : ModelViewBase
{
    private readonly Comments _comments;

    public string Text
    {
        get => _comments.Text;
        set
        {
            if (_comments.Text != value)
            {
                var oldValue = _comments.Text;
                _comments.Text = value;
               OnPropertyChanged<string>(oldValue);
            }
        }
    }

    public CommentsViewModel(Comments comments) => _comments = comments;
}
