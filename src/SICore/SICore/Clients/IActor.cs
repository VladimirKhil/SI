using SICore.BusinessLogic;
using SICore.Network.Contracts;

namespace SICore;

public interface IActor : IDisposable
{
    IClient Client { get; }

    ILocalizer LO { get; }
}
