using SICore;
using SICore.Network.Clients;
using SIData;
using SImulator.ViewModel.Contracts;

namespace SImulator.ViewModel.Services;

public sealed class PresentationHandler(Client client, IPresentationController presentationController) : MessageHandler(client)
{
    public override ValueTask OnMessageReceivedAsync(Message message)
    {
        presentationController.SendRawMessage(message);
        return ValueTask.CompletedTask;
    }
}
