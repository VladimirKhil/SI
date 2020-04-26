using System;
using System.Linq;
using System.Runtime.Serialization;

namespace SICore
{
    [DataContract(Name = "FileKey", Namespace = "http://schemas.datacontract.org/2004/07/SIHost")]
    public class FileKey
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public byte[] Hash { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is FileKey other))
                return base.Equals(obj);

            return Name == other.Name && (Hash == null && other.Hash == null || Hash.SequenceEqual(other.Hash));
        }

        public override int GetHashCode()
        {
            return Hash != null ? Convert.ToBase64String(Hash).GetHashCode() : (Name != null ? Name.GetHashCode() : -1);
        }

        public override string ToString() => $"{Convert.ToBase64String(Hash)}_{Name}";

        public static FileKey Parse(string s)
        {
            var index = s.IndexOf('_');
            if (index == -1)
                throw new InvalidCastException();

            return new FileKey { Name = s.Substring(index + 1), Hash = Convert.FromBase64String(s.Substring(0, index)) };
        }
    }
}
