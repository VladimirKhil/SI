using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;

namespace SIQuester.Model
{
    /// <summary>
    /// Результат поиска
    /// </summary>
    public sealed class SearchResult
    {
        /// <summary>
        /// Имя файла
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Найденный фрагмент из трёх частей (до совпадения, совпадение, после совпадения)
        /// </summary>
        public SearchMatch Fragment { get; set; }
    }
}
