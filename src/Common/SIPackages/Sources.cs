using System;
using System.Collections.Generic;
using SIPackages.Core;

namespace SIPackages
{
    /// <summary>
    /// Источники объекта в пакете
    /// </summary>
    public sealed class Sources : List<string>
    {
        /// <summary>
        /// Создание списка источников
        /// </summary>
        public Sources() { }

        public Sources(IList<string> collection)
            : base(collection)
        {

        }

        /// <summary>
        /// Строковое представление истчоников
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"Источники: {this.ToCommonString()}";
    }
}
