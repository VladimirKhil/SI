using System.Runtime.Serialization;

namespace SIPackages;

/// <summary>
/// Defines a list of package authors.
/// </summary>
[CollectionDataContract(Name = "Authors", Namespace = "")]
public sealed class AuthorInfoList : List<AuthorInfo> { }
