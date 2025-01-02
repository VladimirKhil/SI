using SIEngine.Core;
using SIPackages;

namespace SIEngine;

/// <summary>
/// Allows to create question engine for a question.
/// </summary>
public sealed class QuestionEngineFactory : IQuestionEngineFactory
{
    private readonly IQuestionEnginePlayHandler _playHandler;

    public QuestionEngineFactory(IQuestionEnginePlayHandler playHandler) => _playHandler = playHandler;

    /// <summary>
    /// Creates question engine for a question.
    /// </summary>
    /// <param name="question">Question being played.</param>
    /// <param name="questionEngineOptions">Engine options.</param>
    /// <returns>Created engine.</returns>
    public IQuestionEngine CreateEngine(Question question, QuestionEngineOptions questionEngineOptions) =>
        new QuestionEngine(question, questionEngineOptions, _playHandler);
}
