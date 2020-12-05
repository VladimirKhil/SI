using SIPackages.Core;

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
