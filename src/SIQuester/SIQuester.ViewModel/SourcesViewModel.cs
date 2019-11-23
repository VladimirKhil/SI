using SIPackages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace SIQuester.ViewModel
{
    public sealed class SourcesViewModel: LinksViewModel
    {
        public Sources Model { get; private set; }

        public SourcesViewModel(Sources model, InfoViewModel owner)
            : base(model, owner)
        {
            this.Model = model;
        }

        protected override void LinkTo(int index, object arg)
        {
            this.OwnerDocument.Document.SetSourceLink(this, index, this.OwnerDocument.Document.Sources.IndexOf((SourceInfo)arg));
        }
    }
}
