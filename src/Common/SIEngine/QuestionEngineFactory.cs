using SIEngine.Core;
using SIPackages;

namespace SIEngine;

/// <summary>
/// Allows to create question engine for a question.
/// </summary>
public sealed class QuestionEngineFactory
{
    private readonly IQuestionEnginePlayHandler _playHandler;

    public QuestionEngineFactory(IQuestionEnginePlayHandler playHandler) => _playHandler = playHandler;

    /// <summary>
    /// Creates question engine for a question.
    /// </summary>
    /// <param name="question">Question being played.</param>
    /// <param name="isFinal">Does the question belong to final round.</param>
    /// <returns>Created engine.</returns>
    public QuestionEngine CreateEngine(Question question, bool isFinal) =>
        new(question, isFinal, _playHandler);
}
