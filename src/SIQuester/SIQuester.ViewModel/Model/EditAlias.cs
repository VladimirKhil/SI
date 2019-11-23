using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace SIQuester.Model
{
    public sealed class EditAlias
    {
        public string VisibleName { get; private set; }
        public Color Color { get; private set; }

        public EditAlias()
        {

        }

        public EditAlias(string visibleName, Color color)
        {
            VisibleName = visibleName;
            Color = color;
        }
    }
}
