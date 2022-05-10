using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SIUI.Model
{
    /// <summary>
    /// Defines themes information.
    /// </summary>
    [DataContract]
    public sealed class ThemeInfo
    {
        /// <summary>
        /// Theme name.
        /// </summary>
        [DataMember]
        public string Name { get; set; } = "";

        /// <summary>
        /// Questions information.
        /// </summary>
        [DataMember]
        public List<QuestionInfo> Questions { get; } = new List<QuestionInfo>();
    }
}
