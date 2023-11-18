using SICore.BusinessLogic;
using SICore.Network.Clients;
using SIData;

namespace SICore;

public sealed class SimpleViewer : Viewer
{
    /// <summary>
    /// Запуск клиента
    /// </summary>
    public SimpleViewer(Client client, Account personData, bool isHost, ILocalizer localizer, ViewerData data)
        : base(client, personData, isHost, localizer, data)
    {
        
    }
}
