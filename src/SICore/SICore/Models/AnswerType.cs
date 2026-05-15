namespace SICore.Models;

/// <summary>
/// Defines the type of answer expected.
/// </summary>
public enum AnswerType
{
    /// <summary>
    /// Text answer.
    /// </summary>
    Text,

    /// <summary>
    /// Numeric answer.
    /// </summary>
    Number,

    /// <summary>
    /// Point answer.
    /// </summary>
    Point,

    /// <summary>
    /// Answer result is managed by client.
    /// </summary>
    Client,
}
