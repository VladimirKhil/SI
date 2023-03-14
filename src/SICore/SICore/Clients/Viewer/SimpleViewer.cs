using SICore.BusinessLogic;
using SICore.Network.Clients;
using SIData;

namespace SICore;

public sealed class SimpleViewer : Viewer<IViewerLogic>
{
    /// <summary>
    /// Запуск клиента
    /// </summary>
    public SimpleViewer(Client client, Account personData, bool isHost, ILocalizer localizer, ViewerData data)
        : base(client, personData, isHost, localizer, data)
    {
        
    }

    protected override IViewerLogic CreateLogic(Account personData)
    {
        if (personData == null)
        {
            throw new ArgumentNullException(nameof(personData));
        }

        return personData.IsHuman ?
            new ViewerHumanLogic(ClientData, _viewerActions, LO) :
            new ViewerComputerLogic(ClientData, _viewerActions, (ComputerAccount)personData);
    }
}
