using System.Runtime.Serialization;

namespace SIUI.Model
{
    /// <summary>
    /// Данные вопроса
    /// </summary>
    [DataContract]
    public sealed class QuestionInfo
    {
        /// <summary>
        /// Цена вопроса
        /// </summary>
        [DataMember]
        public int Price { get; set; } = -1;
    }
}
