using System.Collections.Generic;
using System.Threading.Tasks;

namespace SImulator.Implementation.ButtonManagers.WebNew;

public interface IGameRepository
{
    ICollection<string> BannedNames { get; }

    ConnectionPersonData[] Players { get; }

    Task<bool> TryAddPlayerAsync(string id, string userName);

    Task<bool> TryRemovePlayerAsync(string playerName);

    GameInfo? TryGetGameById(int gameId);

    void OnPlayerPress(string playerName);

    void InformPlayer(string playerName, string connectionId);

    void OnPlayerAnswer(string playerName, string answer, bool isPreliminary);

    void OnPlayerPass(string playerName);

    void OnPlayerStake(string playerName, int stake);
}
