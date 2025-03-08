using SIPackages.TypeConverters;
using System.ComponentModel;

namespace SIPackages;

/// <summary>
/// Defines a package item comments.
/// </summary>
[TypeConverter(typeof(CommentsTypeConverter))]
public sealed record Comments
{
    /// <summary>
    /// Comments text.
    /// </summary>
    [DefaultValue("")]
    public string Text { get; set; } = "";
}
