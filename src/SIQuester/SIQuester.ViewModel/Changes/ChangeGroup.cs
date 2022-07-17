using System.Collections.Generic;

namespace SIQuester
{
    /// <summary>
    /// Defines a change group which acts like a single change.
    /// </summary>
    internal sealed class ChangeGroup : List<IChange>, IChange
    {
        public void Undo()
        {
            for (var i = Count - 1; i > -1; i--)
            {
                this[i].Undo();
            }
        }

        public void Redo() => ForEach(item => item.Redo());
    }
}
