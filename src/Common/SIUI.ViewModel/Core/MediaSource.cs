using System.Runtime.Serialization;

namespace SIUI.ViewModel.Core;

[DataContract]
[KnownType(typeof(MemoryStream))]
public sealed class MediaSource
{
    [DataMember]
    public string Uri { get; private set; }

    public MediaSource(string uri) => Uri = uri;
}
