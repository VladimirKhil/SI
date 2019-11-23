using System;
using System.Collections.Generic;
using System.Text;

namespace SIQuester.Converters
{
    [Serializable]
    public sealed class StringDictionary: Dictionary<string, string>
    {
        public StringDictionary()
        {
        }

        private StringDictionary(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            
        }
    }
}
