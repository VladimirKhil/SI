using SIPackages;
using System.ComponentModel;

namespace SIQuester.ViewModel;

public sealed class ShowmanCommentsViewModel : ModelViewBase
{
    private readonly Info _info;

    public bool HasValue => _info.ShowmanComments != null;

    public string Text
    {
        get => _info.ShowmanComments?.Text ?? "";
        set
        {
            if (_info.ShowmanComments != null && value.Length == 0)
            {
                var oldValue = _info.ShowmanComments.Text;
                _info.ShowmanComments = null;
                OnPropertyChanged<string>(oldValue, nameof(Text));
                return;
            }

            var comments = EnsureComments();

            if (comments.Text != value)
            {
                var oldValue = comments.Text;
                comments.Text = value;
                OnPropertyChanged<string>(oldValue);
                OnPropertyChanged(nameof(HasValue));
            }
        }
    }

    public ShowmanCommentsViewModel(Info info)
    {
        _info = info;
        _info.PropertyChanged += Info_PropertyChanged;
    }

    private void Info_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Info.ShowmanComments))
        {
            OnPropertyChanged(nameof(HasValue));
            OnPropertyChanged(nameof(Text));
        }
    }

    public void Clear() => Text = "";

    private Comments EnsureComments()
    {
        if (_info.ShowmanComments != null)
        {
            return _info.ShowmanComments;
        }

        _info.ShowmanComments = new Comments();
        return _info.ShowmanComments;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _info.PropertyChanged -= Info_PropertyChanged;
        }

        base.Dispose(disposing);
    }
}
