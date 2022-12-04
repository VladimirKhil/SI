using System.Runtime.Serialization;

namespace SImulator.ViewModel.Core;

[DataContract]
public sealed class ErrorInfo
{
    [DataMember]
    public string Version { get; set; }
    [DataMember]
    public DateTime Time { get; set; }
    [DataMember]
    public string Error { get; set; }
}

[DataContract]
public sealed class ErrorInfoList : List<ErrorInfo> { }
