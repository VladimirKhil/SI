using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIPackages.Core
{
    /// <summary>
    /// Информация о потоках, не поддерживающих свойство Length
    /// </summary>
    public sealed class StreamInfo
    {
        public Stream Stream { get; set; }
        public long Length { get; set; }
    }
}
