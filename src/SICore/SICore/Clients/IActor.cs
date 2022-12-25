using SICore.BusinessLogic;
using SICore.Network.Contracts;

namespace SICore;

public interface IActor : IAsyncDisposable
{
    IClient Client { get; }

    ILocalizer LO { get; }

    /// <summary>
    /// Добавить сообщение в лог
    /// </summary>
    /// <param name="s">Текст сообщения</param>
    void AddLog(string s);
}
