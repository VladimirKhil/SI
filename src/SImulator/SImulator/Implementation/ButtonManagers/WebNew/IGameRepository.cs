using System.Collections.Generic;

namespace SImulator.Implementation.ButtonManagers.WebNew;

public interface IGameRepository
{
    ICollection<string> BannedNames { get; }

    void AddPlayer(string id, string userName);

    void RemovePlayer(string userName);

    GameInfo? TryGetGameById(int gameId);

    void OnPlayerPress(string playerName);

    void InformPlayer(string playerName, string connectionId);

    void OnPlayerAnswer(string playerName, string answer, bool isPreliminary);

    void OnPlayerPass(string playerName);
}
