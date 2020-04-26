using System.Globalization;

namespace SICore.Network.Contracts
{
    public interface INetworkLocalizer
    {
        string this[string key] { get; }
        CultureInfo Culture { get; }
    }
}
