using SIQuester.ViewModel;

namespace SIQuester.Utilities;

internal sealed class LinkManager : ILinkManager
{
    public string GetLinkText(System.Collections.IList collection, int index, out bool canBeExtended, out string tail)
    {
        if (collection is AuthorsViewModel authors)
        {
            canBeExtended = false;
            tail = null;

            var owner = authors.Owner?.Owner;

            if (owner == null)
            {
                return null;
            }

            while (owner.Owner != null)
            {
                owner = owner.Owner;
            }

            var document = ((PackageViewModel)owner).Document?.Document;

            if (document == null)
            {
                return null;
            }

            var author = document.GetLink(authors.Model, index);
            return author?.ToString();
        }

        if (collection is SourcesViewModel sources)
        {
            canBeExtended = true;
            tail = null;

            var owner = sources.Owner?.Owner;

            if (owner == null)
            {
                return null;
            }

            while (owner.Owner != null)
            {
                owner = owner.Owner;
            }

            var document = ((PackageViewModel)owner).Document?.Document;

            if (document == null)
            {
                return null;
            }

            var source = document.GetLink(sources.Model, index, out tail);
            return source?.ToString();
        }

        canBeExtended = false;
        tail = null;
        return null;
    }
}
