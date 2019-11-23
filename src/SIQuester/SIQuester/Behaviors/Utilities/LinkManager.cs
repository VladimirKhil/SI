using SIPackages;
using SIQuester.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIQuester.Utilities
{
    internal sealed class LinkManager: ILinkManager
    {
        public string GetLinkText(System.Collections.IList collection, int index, out bool canBeExtended, out string tail)
        {
            if (collection is AuthorsViewModel authors)
            {
                canBeExtended = false;
                tail = null;

                var owner = authors.Owner.Owner;
                while (owner.Owner != null)
                    owner = owner.Owner;

                var document = ((PackageViewModel)owner).Document.Document;

                var author = document.GetLink(authors.Model, index);
                return author?.ToString();
            }

            if (collection is SourcesViewModel sources)
            {
                canBeExtended = true;

                var owner = sources.Owner.Owner;
                while (owner.Owner != null)
                    owner = owner.Owner;

                var document = ((PackageViewModel)owner).Document.Document;

                var source = document.GetLink(sources.Model, index, out tail);
                return source?.ToString();
            }

            canBeExtended = false;
            tail = null;
            return null;
        }
    }
}
