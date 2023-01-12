using SICore.Network.Clients;

namespace SICore;

public static class PlayerDataExtensions
{
    /// <summary>
    /// Сумма ближайшего преследователя
    /// </summary>
    public static int BigSum(this ViewerData viewerData, Client client) => viewerData.Players.Where(player => player.Name != client.Name).Max(player => player.Sum);

    /// <summary>
    /// Сумма дальнего преследователя
    /// </summary>
    public static int SmallSum(this ViewerData viewerData, Client client) => viewerData.Players.Where(player => player.Name != client.Name).Min(player => player.Sum);

    /// <summary>
    /// Собственный счёт
    /// </summary>
    public static int MySum(this ViewerData viewerData) => ((PlayerAccount)viewerData.Me).Sum;
}
