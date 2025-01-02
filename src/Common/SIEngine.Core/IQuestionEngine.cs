namespace SIEngine.Core;

public interface IQuestionEngine
{
    string QuestionTypeName { get; }

    bool PlayNext();

    void MoveToAnswer();
}
