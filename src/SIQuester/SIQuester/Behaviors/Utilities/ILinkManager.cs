using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace SIQuester.Utilities
{
    public interface ILinkManager
    {
        string GetLinkText(IList collection, int index, out bool canBeSpecified, out string tail);
    }
}
