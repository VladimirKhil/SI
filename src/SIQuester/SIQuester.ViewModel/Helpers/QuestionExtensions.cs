using SIPackages;
using SIPackages.Core;

namespace SIQuester.ViewModel.Helpers;

/// <summary>
/// Provides helper functions for detecting filled question parts.
/// </summary>
internal static class QuestionExtensions
{
    /// <summary>
    /// Detects whether the question has both content and a right answer filled in.
    /// </summary>
    /// <param name="question">Question to check.</param>
    internal static bool IsFilled(this Question question) => HasContent(question) && question.HasAnswer();

    /// <summary>
    /// Detects whether the question right answer is filled in.
    /// </summary>
    /// <param name="question">Question to check.</param>
    internal static bool HasAnswer(this Question question)
    {
        var answerType = question.Parameters.TryGetValue(QuestionParameterNames.AnswerType, out var answerTypeParameter)
            ? answerTypeParameter.SimpleValue
            : StepParameterValues.SetAnswerTypeType_Text;

        return answerType switch
        {
            // The answer is validated by the client (e.g. an HTML minigame); there is nothing to fill in the package
            StepParameterValues.SetAnswerTypeType_ManagedByClient => true,
            // Every answer option must have content
            StepParameterValues.SetAnswerTypeType_Select => HasFilledOptions(question),
            // The position on the image must be set
            StepParameterValues.SetAnswerTypeType_Point => HasSelectedPoint(question),
            // Text and number answers are stored as plain right answers
            _ => question.Right.Any(answer => !string.IsNullOrWhiteSpace(answer)),
        };
    }

    /// <summary>
    /// Detects whether the question body contains media.
    /// </summary>
    /// <param name="question">Question to check.</param>
    /// <remarks>
    /// Unlike <see cref="Question.HasMediaContent" />, the media of the other parameters
    /// (the complex answer in the first place) is not taken into account.
    /// </remarks>
    internal static bool HasQuestionMedia(this Question question) =>
        question.Parameters.TryGetValue(QuestionParameterNames.Question, out var body)
        && body.ContentValue != null
        && body.ContentValue.Any(item => item.Type != ContentTypes.Text);

    // Only the question parameter counts as content (text or media); the complex answer parameter is ignored
    private static bool HasContent(Question question) =>
        question.Parameters.TryGetValue(QuestionParameterNames.Question, out var body)
        && body.ContentValue != null
        && body.ContentValue.Any(item => !string.IsNullOrWhiteSpace(item.Value));

    private static bool HasFilledOptions(Question question) =>
        question.Parameters.TryGetValue(QuestionParameterNames.AnswerOptions, out var options)
        && options.GroupValue != null
        && options.GroupValue.Count > 0
        && options.GroupValue.Values.All(option =>
            option.ContentValue != null
            && option.ContentValue.Any(item => !string.IsNullOrWhiteSpace(item.Value)));

    // Point answers store the position as "x,y,aspect"; the point is set only when all three parts are present
    private static bool HasSelectedPoint(Question question) =>
        question.Right.Count > 0
        && question.Right[0].Split(',') is { Length: 3 } parts
        && parts.All(part => !string.IsNullOrWhiteSpace(part));
}
