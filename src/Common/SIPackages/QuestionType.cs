using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SIPackages
{
    /// <summary>
    /// Тип вопроса
    /// </summary>
    public sealed class QuestionType : Named
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<QuestionTypeParam> _parameters = new List<QuestionTypeParam>();

        /// <summary>
        /// Параметры типа
        /// </summary>
        public List<QuestionTypeParam> Params => _parameters;

        public QuestionType()
            : base(QuestionTypes.Simple)
        {
            
        }

        public string this[string name]
        {
            get
            {
                var item = _parameters.FirstOrDefault(qtp => qtp.Name == name);
                return item != null ? item.Value : "";
            }
            set
            {
                var item = _parameters.FirstOrDefault(qtp => qtp.Name == name);
                if (item != null)
                {
                    item.Value = value;
                }
                else
                {
                    _parameters.Add(new QuestionTypeParam { Name = name, Value = value });
                }
            }
        }

        public override bool Contains(string value) =>
            Name.Contains(value)
                || _parameters.Any(item => item.Name.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > -1
                    || item.Value.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) > -1);

        public override IEnumerable<SearchData> Search(string value)
        {
            foreach (var item in SearchExtensions.Search(ResultKind.TypeName, Name, value))
            {
                yield return item;
            }

            for (int i = 0; i < _parameters.Count; i++)
            {
                var index = _parameters[i].Name.IndexOf(value, StringComparison.CurrentCultureIgnoreCase);
                if (index > -1)
                    yield return new SearchData(_parameters[i].Name, index, i, ResultKind.TypeParamName);

                index = _parameters[i].Value.IndexOf(value, StringComparison.CurrentCultureIgnoreCase);
                if (index > -1)
                    yield return new SearchData(_parameters[i].Value, index, i, ResultKind.TypeParamValue);
            }
        }

        /// <summary>
        /// Строковое представление типа
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Name}({string.Join(", ", _parameters.Select(param => param.ToString()))})";
    }
}
