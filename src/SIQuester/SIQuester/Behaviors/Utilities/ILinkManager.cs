using System.Collections;

namespace SIQuester.Utilities;

public interface ILinkManager
{
    string GetLinkText(IList collection, int index, out bool canBeSpecified, out string tail);
}
