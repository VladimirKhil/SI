using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIQuester.Model
{
    public sealed class ErrorInfo
    {
        public Version Version { get; set; }
        public DateTime Time { get; set; }
        public string Error { get; set; }
    }
}
