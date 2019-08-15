using System;
using System.Diagnostics;

namespace SIPackages
{
    /// <summary>
    /// Информация об объекте в пакете
    /// </summary>
    public sealed class Info
    {
        /// <summary>
        /// Авторы
        /// </summary>
        public Authors Authors { get; } = new Authors();

        /// <summary>
        /// Источники
        /// </summary>
        public Sources Sources { get; } = new Sources();

        /// <summary>
        /// Комментарии
        /// </summary>
        public Comments Comments { get; } = new Comments();

        /// <summary>
        /// Расширение данных
        /// </summary>
        public string Extension { get; set; }

        public override string ToString() => string.Format("[{0}, {1}, {2}]", this.Authors, this.Sources, this.Comments);
    }
}
