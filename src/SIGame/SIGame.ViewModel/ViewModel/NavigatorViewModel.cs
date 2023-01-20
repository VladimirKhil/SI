using SICore;
using SIGame.ViewModel.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SIGame.ViewModel;

public sealed class NavigatorViewModel : INotifyPropertyChanged, ICloseable, IDisposable
{
    private readonly Stack<ContentBox> _history = new();

    private ContentBox _content;

    public ContentBox Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                if (_content != null)
                {
                    if (_content.Data is INavigatable navigatable)
                    {
                        navigatable.Navigate -= Content_Navigate;
                    }

                    if (_content.Data is INavigationNode navigationNode)
                    {
                        navigationNode.Close -= NavigationNode_Close;
                    }

                    _history.Push(_content);
                    Back.CanBeExecuted = true;
                }

                _content = value;

                if (_content != null)
                {
                    if (_content.Data is INavigatable navigatable)
                    {
                        navigatable.Navigate += Content_Navigate;
                    }

                    if (_content.Data is INavigationNode navigationNode)
                    {
                        navigationNode.Close += NavigationNode_Close;
                    }
                }

                OnPropertyChanged();
            }
        }
    }

    private void NavigationNode_Close() => Back_Executed(true);

    private void Content_Navigate(ContentBox contentBox)
    {
        Content = contentBox;
    }

    public CustomCommand Back { get; private set; }
    public ICommand Cancel { get; set; }
    public ICommand CancelBase { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action? Closed;

    public NavigatorViewModel()
    {
        Back = new CustomCommand(Back_Executed) { CanBeExecuted = false };
        CancelBase = new CustomCommand(CancelBase_Executed);
    }

    private void CancelBase_Executed(object? arg)
    {
        Closed?.Invoke();
        Cancel.Execute(null);
    }

    private void Back_Executed(object? arg)
    {
        var currentValue = _content.Data;

        if (_content.Data is INavigatable navigatable)
        {
            navigatable.Navigate -= Content_Navigate;
        }

        if (_content.Data is INavigationNode navigationNode)
        {
            navigationNode.Close -= NavigationNode_Close;
        }

        _content = _history.Pop();

        if (_content.Data is INavigatable navigatable2)
        {
            navigatable2.Navigate += Content_Navigate;

            if (arg == null)
            {
                navigatable2.OnNavigatedFrom(currentValue);
            }
        }

        if (_content.Data is INavigationNode navigationNode2)
        {
            navigationNode2.Close += NavigationNode_Close;
        }

        Back.CanBeExecuted = _history.Any();
        OnPropertyChanged(nameof(Content));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        if (_content.Data is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
