using SIPackages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIQuester.ViewModel
{
    public sealed class SelectableTheme
    {
        public bool IsSelected { get; set; }

        public string Name
        {
            get { return Theme.Name; }
        }

        public Theme Theme { get; }

        public SelectableTheme(Theme theme)
        {
            Theme = theme;
        }
    }
}
