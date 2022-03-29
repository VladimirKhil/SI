namespace SIPackages.Core
{
    /// <summary>
    /// Где нашлось
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

    public enum MediaTypes
    {
        /// <summary>
        /// Изображения
        /// </summary>
        Images,
        /// <summary>
        /// Звуки
        /// </summary>
        Audio,
        /// <summary>
        /// Видео
        /// </summary>
        Video
    }
}
