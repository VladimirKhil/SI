using SICore;
using SIData;
using SIGame.ViewModel.ViewModel;

namespace SIGame.ViewModel;

public sealed class ShowmanVM : IPersonViewModel
{
    public PersonAccount Model { get; }

    public bool IsPlayer => false;

    public bool IsHuman => Model.IsHuman;

    public bool IsConnected => Model.IsConnected;

    public Account[] Others => Model.Others;

    public string Name => Model.Name;

    public ShowmanVM(PersonAccount model) => Model = model;
}
