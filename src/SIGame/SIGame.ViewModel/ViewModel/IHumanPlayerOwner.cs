namespace SIGame.ViewModel;

public interface IHumanPlayerOwner
{
    string HumanPlayerName { get; set; }

    AppSettings AppSettings { get; }
}
