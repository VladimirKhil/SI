using System.Threading.Tasks;

namespace SImulator.Implementation.ButtonManagers.WebNew;

public interface IButtonClient
{
    /// <summary>
    /// Receives incoming message.
    /// </summary>
    /// <param name="message">Incoming message.</param>
    Task Receive(Message message);
}
