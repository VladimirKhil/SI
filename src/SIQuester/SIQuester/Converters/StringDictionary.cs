using System.Runtime.Serialization;

namespace SIQuester.Converters;

[Serializable]
public sealed class StringDictionary : Dictionary<string, string>
{
    public StringDictionary()
    {
    }

    private StringDictionary(SerializationInfo serializationInfo, StreamingContext streamingContext)
    {
        
    }
}
