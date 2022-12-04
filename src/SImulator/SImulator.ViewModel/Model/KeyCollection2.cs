using SImulator.ViewModel.Core;
using System.Collections.ObjectModel;

namespace SImulator.ViewModel.Model;

public sealed class KeyCollection2 : ObservableCollection<GameKey>
{
    public KeyCollection2()
    {

    }

    public KeyCollection2(IEnumerable<GameKey> collection)
        : base(collection)
    {

    }
}
