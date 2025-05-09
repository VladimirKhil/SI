using SICore;
using SIData;
using SIGame.ViewModel.ViewModel;

namespace SIGame.ViewModel;

public sealed class PlayerViewModel : IPersonViewModel
{
    public PlayerAccount Model { get; }

    public bool IsPlayer => true;

    public bool IsHuman => Model.IsHuman;

    public bool IsConnected => Model.IsConnected;

    public Account[] Others => Model.Others;

    public string Name => Model.Name;

    public PlayerViewModel(PlayerAccount model) => Model = model;
}
