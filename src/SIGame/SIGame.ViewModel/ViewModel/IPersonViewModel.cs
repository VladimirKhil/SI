using SICore;
using SIData;

namespace SIGame.ViewModel;

public interface IPersonViewModel
{
    bool IsPlayer { get; }

    string Name { get; }

    bool IsHuman { get; }

    bool IsConnected { get; }

    Account[] Others { get; }
}
