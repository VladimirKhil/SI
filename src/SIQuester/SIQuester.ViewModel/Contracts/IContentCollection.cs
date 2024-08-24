using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel.Contracts;

public interface IContentCollection
{
    ICommand AddFile { get; }

    SimpleCommand LinkUri { get; }

    ICommand LinkFile { get; }
}
