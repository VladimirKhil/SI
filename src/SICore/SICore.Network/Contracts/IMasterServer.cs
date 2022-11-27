using System.Threading.Tasks;

namespace SICore.Network.Contracts
{
    public interface IMasterServer: INode
    {
        ValueTask KickAsync(string name, bool ban = false);
    }
}
