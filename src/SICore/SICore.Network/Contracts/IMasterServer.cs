using System.Threading.Tasks;

namespace SICore.Network.Contracts
{
    public interface IMasterServer: IServer
    {
        ValueTask KickAsync(string name, bool ban = false);
    }
}
