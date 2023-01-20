using System.Windows.Input;

namespace SIGame.ViewModel;

public sealed class ContentBox : IDisposable
{
    public object Data { get; set; }

    public string Title { get; set; }

    public ICommand Cancel { get; set; }

    public void Dispose()
    {
        if (Data is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
