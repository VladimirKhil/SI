using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIQuester.ViewModel
{
    public sealed class LinkViewModel
    {
        public string Title { get; set; }

        public string Uri { get; set; }
        public bool IsMultiline { get; set; }
    }
}
