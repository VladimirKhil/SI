using System.Runtime.Serialization;

namespace SIUI.ViewModel;

/// <summary>
/// Defines well-known content types displayed on the table.
/// </summary>
[DataContract]
public enum QuestionContentType
{
    /// <summary>
    /// Void content.
    /// </summary>
    [EnumMember]
    Void,

    /// <summary>
    /// Clef content.
    /// </summary>
    [EnumMember]
    Clef,

    /// <summary>
    /// Empty content.
    /// </summary>
    [EnumMember]
    [Obsolete("Use Void or Clef type")]
    None,

    /// <summary>
    /// Text content.
    /// </summary>
    [EnumMember]
    Text,

    /// <summary>
    /// Image content.
    /// </summary>
    [EnumMember]
    Image,

    /// <summary>
    /// Video content.
    /// </summary>
    [EnumMember]
    Video,

    /// <summary>
    /// Special text content.
    /// </summary>
    [EnumMember]
    SpecialText,

    /// <summary>
    /// Html content.
    /// </summary>
    [EnumMember]
    Html,

    /// <summary>
    /// Loading content mode.
    /// </summary>
    [EnumMember]
    Loading,

    /// <summary>
    /// Collection of different content items.
    /// </summary>
    [EnumMember]
    Collection,
}
