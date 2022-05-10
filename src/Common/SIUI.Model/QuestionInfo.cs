using System.Runtime.Serialization;

namespace SIUI.Model
{
    /// <summary>
    /// Defines question information.
    /// </summary>
    [DataContract]
    public sealed class QuestionInfo
    {
        /// <summary>
        /// Question price.
        /// </summary>
        [DataMember]
        public int Price { get; set; } = -1;
    }
}
