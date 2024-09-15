namespace SImulator.Implementation.ButtonManagers.WebNew;

public interface IGameRepository
{
    void AddPlayer(string userName);

    void RemovePlayer(string userName);

    GameInfo? TryGetGameById(int gameId);

    void OnPlayerPress(string playerName);

    void InformPlayer(string playerName, string connectionId);
}
