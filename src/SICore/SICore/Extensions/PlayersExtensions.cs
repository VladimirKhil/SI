namespace SICore.Extensions;

internal static class PlayersExtensions
{
    internal static int SelectRandomIndex(this List<PlayerAccount> players)
    {
        var availablePlayerCount = players.Count(p => p.CanBeSelected);
        var selectedIndex = Random.Shared.Next(availablePlayerCount);
        var currentIndex = 0;

        for (var i = 0; i < players.Count; i++)
        {
            if (players[i].CanBeSelected)
            {
                if (currentIndex == selectedIndex)
                {
                    return i;
                }

                currentIndex++;
            }
        }

        return -1;
    }
}
