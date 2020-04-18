using System.Runtime.Serialization;

namespace SICore
{
    [DataContract]
    public sealed class ComputerAccountInfo
    {
        [DataMember]
        public FileKey Picture { get; set; }
        [DataMember]
        public ComputerAccount Account { get; set; }
    }
}
