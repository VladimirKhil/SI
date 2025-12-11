namespace SIEngine.Core;

/// <summary>
/// Defines a question engine that manages question playback as a state machine.
/// The engine progresses through a finite sequence of steps, invoking handler methods
/// at each stage to display content, accept answers, and manage game flow.
/// </summary>
/// <remarks>
/// <para>
/// The engine processes questions in 4 major stages:
/// 1. Preambula (setting prices, layout, answerers, themes, etc.)
/// 2. Displaying question content (text, images, audio, video)
/// 3. Asking for answer(s) (enabling buttons, accepting inputs)
/// 4. Displaying the right answer
/// </para>
/// <para>
/// The state machine guarantees that it will reach the end in a finite number of steps
/// by calling <see cref="PlayNext"/> repeatedly until it returns false.
/// </para>
/// </remarks>
public interface IQuestionEngine
{
    /// <summary>
    /// Gets the question type name.
    /// </summary>
    /// <remarks>
    /// This property is obsolete and should not be used. It will be removed in the future.
    /// </remarks>
    [Obsolete("This property is obsolete and will be removed in the future.")]
    string QuestionTypeName { get; }

    /// <summary>
    /// Advances the state machine to the next step and invokes the appropriate handler method.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the state machine should pause (e.g., waiting for user interaction or content playback);
    /// <c>false</c> if the question has finished playing or there are no more steps.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method should be called repeatedly to progress through the question.
    /// When it returns <c>true</c>, the caller should wait for the appropriate event
    /// (e.g., content finished, user pressed button, time elapsed) before calling it again.
    /// When it returns <c>false</c>, the question has completed.
    /// </para>
    /// <para>
    /// The method processes script steps sequentially, calling handler methods from
    /// <see cref="IQuestionEnginePlayHandler"/> to notify about state changes and content display.
    /// </para>
    /// </remarks>
    bool PlayNext();

    /// <summary>
    /// Moves the execution directly to the answer display stage, skipping intermediate steps.
    /// This is typically called when the question was answered or all players have passed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Current implementation: Jumps to the first step after the last AskAnswer step.
    /// </para>
    /// <para>
    /// Future enhancement: Should play all remaining steps in fast mode rather than skipping them.
    /// This would ensure that all necessary question parts are ready when displaying the answer.
    /// For example, if multiple content items use the same placement/layout, only the last one
    /// should be displayed, but all should be processed to maintain state consistency.
    /// </para>
    /// </remarks>
    void MoveToAnswer();
}
