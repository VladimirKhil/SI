using System.Runtime.Serialization;

namespace SICore
{
    [DataContract]
    public sealed class ConnectionPersonData
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public GameRole Role { get; set; }
        [DataMember]
        public bool IsOnline { get; set; }
    }
}
