using SICore.Network;
using SICore.Network.Clients;
using SIData;

namespace SICore;

public sealed class ViewerActions
{
    public Client Client { get; }

    public ViewerActions(Client client) => Client = client;

    /// <summary>
    /// Отправить сообщение всем
    /// </summary>
    /// <param name="text">Текст сообщения</param>
    public void SendMessage(string text) => Client.SendMessage(text, receiver: NetworkConstants.GameName);

    public void SendMessage(params string[] args) =>
        Client.SendMessage(string.Join(Message.ArgsSeparator, args), receiver: NetworkConstants.GameName);

    public void SendMessageWithArgs(params object[] args) =>
        Client.SendMessage(string.Join(Message.ArgsSeparator, args), receiver: NetworkConstants.GameName);

    public void PressButton(DateTimeOffset? tryStartTime)
    {
        var pressDuration = tryStartTime.HasValue ? (int)DateTimeOffset.UtcNow.Subtract(tryStartTime.Value).TotalMilliseconds : -1;
        SendMessageWithArgs(Messages.I, pressDuration);
    }

    /// <summary>
    /// Sends game info request.
    /// </summary>
    public void GetInfo() => SendMessage(Messages.Info);

    public void Pause(bool pause) => SendMessage(Messages.Pause, pause ? "+" : "-");

    public void Move(object arg) => SendMessageWithArgs(Messages.Move, arg);
}
