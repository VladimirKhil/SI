namespace SIPackages.Core;

/// <summary>
/// Defines search result source kinds.
/// </summary>
public enum ResultKind
{
    /// <summary>
    /// Object name.
    /// </summary>
    Name,
    /// <summary>
    /// Object author.
    /// </summary>
    Author,
    /// <summary>
    /// Object source.
    /// </summary>
    Source,
    /// <summary>
    /// Object comment.
    /// </summary>
    Comment,
    /// <summary>
    /// Object text.
    /// </summary>
    Text,
    /// <summary>
    /// Right answers.
    /// </summary>
    Right,
    /// <summary>
    /// Wrong answers.
    /// </summary>
    Wrong,
    /// <summary>
    /// Question type name.
    /// </summary>
    TypeName,
    /// <summary>
    /// Question type parameter name.
    /// </summary>
    TypeParamName,
    /// <summary>
    /// Question type parameter value.
    /// </summary>
    TypeParamValue
}

/// <summary>
/// Defines well-known media types.
/// </summary>
public enum MediaTypes
{
    /// <summary>
    /// Images.
    /// </summary>
    Images,

    /// <summary>
    /// Sounds.
    /// </summary>
    Audio,

    /// <summary>
    /// Video.
    /// </summary>
    Video
}
