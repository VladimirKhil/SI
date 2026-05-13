using SIPackages;

namespace SIQuester.ViewModel;

public class CommentsViewModel(Comments comments) : ModelViewBase
{
    public string Text
    {
        get => comments.Text;
        set
        {
            if (comments.Text != value)
            {
                var oldValue = comments.Text;
                comments.Text = value;
                OnPropertyChanged<string>(oldValue);
            }
        }
    }

    public void Clear() => Text = "";
}
