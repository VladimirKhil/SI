using SIPackages;

namespace SIEngine.Core;

public interface IQuestionEngineFactory
{
    IQuestionEngine CreateEngine(Question question, QuestionEngineOptions questionEngineOptions);
}
