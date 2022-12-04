using System.Runtime.Serialization;

namespace SIUI.ViewModel.Core;

[DataContract]
[KnownType(typeof(MemoryStream))]
public sealed class MediaSource
{
    [DataMember]
    public Stream? Stream { get; private set; }

    [DataMember]
    public string Uri { get; private set; }

    public MediaSource(Stream? stream, string uri)
    {
        Stream = stream;
        Uri = uri;
    }

    public MediaSource(string uri)
    {
        Uri = uri;
    }
}
