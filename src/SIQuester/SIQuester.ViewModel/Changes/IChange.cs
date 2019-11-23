using System;
using System.Collections.Generic;
using System.Text;

namespace SIQuester
{
    /// <summary>
    /// Изменение документа
    /// </summary>
    public interface IChange
    {
        /// <summary>
        /// Отменить
        /// </summary>
        void Undo();

        /// <summary>
        /// Повторить
        /// </summary>
        void Redo();
    }
}
