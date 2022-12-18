using SIData;

namespace SIGame;

/// <summary>
/// Информация об игре при подключении
/// </summary>
public sealed class ConnectionGameData
{
    public string GameName { get; set; }

    public string Host { get; set; }

    public ConnectionPersonData[] Persons { get; set; }

    internal static ConnectionGameData Create(string[] gameInfo)
    {
        var result = new ConnectionGameData
        {
            GameName = gameInfo[1],
            Host = gameInfo[2]
        };

        var playersCount = int.Parse(gameInfo[3]);
        var personsCount = (gameInfo.Length - 4) / 3;// +1;
        result.Persons = new ConnectionPersonData[personsCount];
        result.Persons[0] = new ConnectionPersonData { Name = gameInfo[4], Role = GameRole.Showman, IsOnline = gameInfo[5] == "+" };

        for (int i = 1; i < result.Persons.Length/* - 1*/; i++)
        {
            if (i < playersCount + 1)
                result.Persons[i] = new ConnectionPersonData { Name = gameInfo[4 + i * 3], Role = GameRole.Player, IsOnline = gameInfo[5 + i * 3] == "+" };
            else
                result.Persons[i] = new ConnectionPersonData { Name = gameInfo[4 + i * 3], Role = GameRole.Viewer, IsOnline = gameInfo[5 + i * 3] == "+" };
        }

        return result;
    }
}
