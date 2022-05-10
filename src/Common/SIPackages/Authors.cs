using System;
using System.Collections.Generic;
using SIPackages.Core;

namespace SIPackages
{
    /// <summary>
    /// Авторы объекта в пакете
    /// </summary>
    public sealed class Authors : List<string>
    {
        /// <summary>
        /// Создание списка авторов
        /// </summary>
        public Authors() { }

        /// <summary>
        /// Создание списка авторов
        /// </summary>
        /// <param name="collection"></param>
        public Authors(IList<string> collection)
            : base(collection)
        {

        }

        /// <summary>
        /// Строковое представление авторов
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"Авторы: {this.ToCommonString()}";
    }
}
