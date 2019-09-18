using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.SI
{
    /// <summary>
    /// Категория пакета
    /// </summary>
    public sealed class PackageCategory
    {
        public int ID { get; set; }
        /// <summary>
        /// Имя категории
        /// </summary>
        public string Name { get; set; }
    }
}
