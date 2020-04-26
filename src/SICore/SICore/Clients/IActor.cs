﻿using SICore.BusinessLogic;
using SICore.Network.Contracts;
using System;

namespace SICore
{
    public interface IActor: IDisposable
    {
        IClient Client { get; }

        ILocalizer LO { get; }

        /// <summary>
        /// Добавить сообщение в лог
        /// </summary>
        /// <param name="s">Текст сообщения</param>
        void AddLog(string s);
    }
}
