using System.Collections.Generic;

namespace SICore
{
    internal sealed class PosUnicList : List<DirPoint>
    {
        private readonly int _min = 0;
        private readonly int _max = 0;

        public PosUnicList()
        {
        }

        public PosUnicList(int min, int max)
        {
            _max = max;
            _min = min;
        }

        public new void Add(DirPoint item)
        {
            foreach (DirPoint p in this)
                if (p.Value == item.Value && p.Direction == item.Direction)
                    return;

            if (!Contains(item) && item.Value > 0 && (_max == 0 || item.Value <= _max) && item.Value >= _min)
                base.Add(item);
        }
    }
}
