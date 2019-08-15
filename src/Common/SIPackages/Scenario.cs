using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIPackages
{
    /// <summary>
    /// Сценарий вопроса
    /// </summary>
    public sealed class Scenario : List<Atom>
    {
        public override string ToString() => string.Join(Environment.NewLine, this.Select(atom => atom.ToString()).ToArray());

        public bool ContainsQuery(string value) => this.Any(item => item.Contains(value));

        public IEnumerable<SearchData> Search(string value)
        {
            for (var i = 0; i < this.Count; i++)
            {
                foreach (var item in this[i].Search(value))
                {
                    item.ItemIndex = i;
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Добавление атома
        /// </summary>
        /// <param name="type">Тип атома</param>
        /// <param name="text">Добавляемая строка</param>
        /// <param name="time">Время показа атома</param>
        public Atom Add(string type, string text, int time = 0)
        {
            var atom = new Atom
            {
                Type = type,
                Text = text,
                AtomTime = time
            };

            Add(atom);
            return atom;
        }

        /// <summary>
        /// Добавление атома
        /// </summary>
        /// <param name="text">Добавляемая строка</param>
        public Atom Add(string text)
        {
            return Add(AtomTypes.Text, text);
        }
    }
}
