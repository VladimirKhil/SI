namespace SImulator.Implementation.ButtonManagers.WebNew;

public interface IGameRepository
{
    void AddPlayer(string userName);
    
    GameInfo? TryGetGameById(int gameId);
}
