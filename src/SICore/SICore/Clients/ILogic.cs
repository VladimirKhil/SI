using SICore.Network.Contracts;
using System;

namespace SICore
{
    /// <summary>
    /// Логика участника игры (человека или компьютера)
    /// </summary>
    public interface ILogic : IAsyncDisposable
    {
        void AddLog(string s);

        Data Data { get; }
    }
}
