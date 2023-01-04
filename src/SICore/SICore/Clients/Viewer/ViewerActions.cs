using SICore.BusinessLogic;
using SICore.Network;
using SICore.Network.Clients;
using SIData;

namespace SICore;

public sealed class ViewerActions
{
    public ILocalizer LO { get; }

    public Client Client { get; }

    public ViewerActions(Client client, ILocalizer localizer)
    {
        Client = client;
        LO = localizer;
    }

    /// <summary>
    /// Отправить сообщение всем
    /// </summary>
    /// <param name="text">Текст сообщения</param>
    public void SendMessage(string text) => Client.SendMessage(text, receiver: NetworkConstants.GameName);

    public void SendMessage(params string[] args) =>
        Client.SendMessage(string.Join(Message.ArgsSeparator, args), receiver: NetworkConstants.GameName);

    public void SendMessageWithArgs(params object[] args) =>
        Client.SendMessage(string.Join(Message.ArgsSeparator, args), receiver: NetworkConstants.GameName);

    /// <summary>
    /// Жмёт на игровую кнопку
    /// </summary>
    internal void PressGameButton() => SendMessage(Messages.I);
}
