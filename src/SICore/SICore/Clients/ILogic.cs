using SICore.Network.Contracts;
using System;

namespace SICore
{
    /// <summary>
    /// Логика участника игры (человека или компьютера)
    /// </summary>
    public interface ILogic : IDisposable
    {
        void AddLog(string s);

        Data Data { get; }

        void SetInfo(IAccountInfo accountInfo);
    }
}
