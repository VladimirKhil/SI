using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SICore
{
    [DataContract]
    public sealed class PackageKey: FileKey
    {
        [DataMember]
        public string ID { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is PackageKey other))
                return base.Equals(obj);

            return Name == other.Name && ID == other.ID;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() * (ID == null ? -1 : ID.GetHashCode());
        }

        public override string ToString()
        {
            return $"{Name}_{BitConverter.ToString(Hash)}_{ID}";
        }
    }
}
