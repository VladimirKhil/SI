using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SIUI.Model
{
    /// <summary>
    /// Данные темы
    /// </summary>
    [DataContract]
    public sealed class ThemeInfo
    {
        /// <summary>
        /// Название темы
        /// </summary>
        [DataMember]
        public string Name { get; set; } = "";

        /// <summary>
        /// Вопросы темы
        /// </summary>
        [DataMember]
        public List<QuestionInfo> Questions { get; } = new List<QuestionInfo>();
    }
}
