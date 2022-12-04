using SICore.BusinessLogic;
using SICore.Network.Clients;
using SIData;

namespace SICore;

public sealed class SimpleViewer : Viewer<IViewer>
{
    /// <summary>
    /// Запуск клиента
    /// </summary>
    public SimpleViewer(Client client, Account personData, bool isHost, ILocalizer localizer, ViewerData data)
        : base(client, personData, isHost, localizer, data)
    {
        
    }

    protected override IViewer CreateLogic(Account personData)
    {
        if (personData == null)
        {
            throw new ArgumentNullException(nameof(personData));
        }

        return personData.IsHuman ?
            (IViewer)new ViewerHumanLogic(ClientData, _viewerActions, LO) :
            new ViewerComputerLogic(ClientData, _viewerActions, (ComputerAccount)personData);
    }
}
