namespace SIGame.ViewModel.Models;

[Flags]
public enum GamesFilter
{
    NoFilter = 0,
    New = 1,
    Sport = 2,
    Tv = 4,
    NoPassword = 8,
    All = 15
}
