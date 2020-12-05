using System.Collections.Generic;

namespace SIQuester
{
    /// <summary>
    /// Группа изменений, хранимая как единое изменение
    /// </summary>
    internal sealed class ChangeGroup: List<IChange>, IChange
    {
        #region IChange Members

        public void Undo()
        {
            for (int i = this.Count - 1; i > -1; i--)
            {
                this[i].Undo();
            }
        }

        public void Redo()
        {
            this.ForEach(item => item.Redo());
        }

        #endregion
    }
}
