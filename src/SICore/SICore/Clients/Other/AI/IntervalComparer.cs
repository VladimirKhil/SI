using System.Collections.Generic;

namespace SICore
{
    internal sealed class IntervalComparer : IComparer<DirPoint>
    {
        #region IComparer<DirFloat> Members

        public int Compare(DirPoint x, DirPoint y)
        {
            float a = x.Value;
            float b = y.Value;

            if (a < b)
                return -1;
            if (a == b)
                if (x.Direction == y.Direction)
                    return 0;
                else if (x.Direction)
                    return 1;
                else
                    return -1;
            return 1;
        }

        #endregion
    }
}
