namespace SICore.Contracts;

/// <summary>
/// Defines an intelligence behavior for showman.
/// </summary>
internal interface IShowmanIntelligence
{
    /// <summary>
    /// Validates the answer.
    /// </summary>
    /// <param name="answer">Given answer.</param>
    /// <param name="rightAnswers">Right question answers.</param>
    /// <param name="wrongAnswers">Wrong question answers.</param>
    bool ValidateAnswer(string answer, string[] rightAnswers, string[] wrongAnswers);
}
