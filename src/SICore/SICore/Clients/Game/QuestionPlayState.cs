using SICore.Models;
using SIEngine.Core;
using SIPackages;

namespace SICore.Clients.Game;

/// <summary>
/// Contains question play state.
/// </summary>
internal sealed class QuestionPlayState
{
    /// <summary>
    /// Indicies of players that are obligated to answer.
    /// </summary>
    internal HashSet<int> AnswererIndicies { get; } = new();

    /// <summary>
    /// Are multiple persons answering this question.
    /// </summary>
    internal bool AreMultipleAnswerers { get; private set; } // => AnswererIndicies.Count > 1; - in the future

    /// <summary>
    /// Answer options.
    /// </summary>
    internal AnswerOption[]? AnswerOptions { get; set; }

    /// <summary>
    /// Used answer options indicies.
    /// </summary>
    internal HashSet<string> UsedAnswerOptions { get; } = new();

    /// <summary>
    /// Has layout been set.
    /// </summary>
    internal bool LayoutShown { get; set; }

    /// <summary>
    /// Are answer options shown.
    /// </summary>
    internal bool AnswerOptionsShown { get; set; }

    /// <summary>
    /// Screen content sequence.
    /// </summary>
    internal IReadOnlyList<ContentItem[]>? ScreenContentSequence { get; set; }

    /// <summary>
    /// Does the question have hidden stakes.
    /// </summary>
    internal bool HiddenStakes { get; set; }

    /// <summary>
    /// Content completions waiting table.
    /// </summary>
    internal Dictionary<(string ContentType, string ContentValue), Completion> MediaContentCompletions { get; } = new();

    /// <summary>
    /// Answer validations status.
    /// </summary>
    internal Dictionary<string, (bool, double)?> Validations { get; } = new();

    /// <summary>
    /// Number of active validations.
    /// </summary>
    internal int ActiveValidationCount => Validations.Count(pair => pair.Value == null);

    /// <summary>
    /// Should the player answers be validated after right answer.
    /// </summary>
    internal bool ValidateAfterRightAnswer => AnswerOptions != null;

    /// <summary>
    /// Is the question in answer display mode.
    /// </summary>
    internal bool IsAnswer { get; set; }

    /// <summary>
    /// Question right answer.
    /// </summary>
    public ICollection<string> RightAnswers { get; internal set; } = Array.Empty<string>();

    /// <summary>
    /// Has the answer stage been announced.
    /// </summary>
    internal bool IsAnswerAnnounced { get; set; }

    /// <summary>
    /// Marks simple (text-only) answer.
    /// </summary>
    internal bool IsAnswerSimple { get; set; }

    /// <summary>
    /// Should the question be answered by pressing button.
    /// </summary>
    internal bool UseButtons { get; set; }

    /// <summary>
    /// Could appellation messages be collected.
    /// </summary>
    public AppellationState AppellationState { get; set; }

    /// <summary>
    /// Pending appellations.
    /// </summary>
    internal List<(string, bool)> Appellations { get; } = new();

    /// <summary>
    /// Pending appellations index.
    /// </summary>
    internal int AppellationIndex { get; set; }

    /// <summary>
    /// Gets a value indicating whether the answer is a numeric value.
    /// </summary>
    public bool IsNumericAnswer { get; internal set; }

    /// <summary>
    /// Defines acceptable deviation for numeric answers.
    /// </summary>
    public int NumericAnswerDeviation { get; internal set; }

    /// <summary>
    /// Marks whether the question has flexible pricing.
    /// </summary>
    public bool FlexiblePrice { get; internal set; }

    internal void Clear()
    {
        AnswererIndicies.Clear();
        AnswerOptions = null;
        UsedAnswerOptions.Clear();
        LayoutShown = false;
        AnswerOptionsShown = false;
        ScreenContentSequence = null;
        HiddenStakes = false;
        MediaContentCompletions.Clear();
        Validations.Clear();
        IsAnswer = false;
        RightAnswers = Array.Empty<string>();
        IsAnswerAnnounced = false;
        IsAnswerSimple = false;
        UseButtons = false;
        AppellationState = AppellationState.None;
        Appellations.Clear();
        AppellationIndex = 0;
        IsNumericAnswer = false;
        NumericAnswerDeviation = 0;
        FlexiblePrice = false;
    }

    internal void RemovePlayer(int playerIndex)
    {
        if (!AnswererIndicies.Any(index => index >= playerIndex))
        {
            return;
        }

        var answererIndices = AnswererIndicies.ToArray();
        AnswererIndicies.Clear();

        for (int i = 0; i < answererIndices.Length; i++)
        {
            if (answererIndices[i] == playerIndex)
            {
                continue;
            }
            else if (answererIndices[i] > playerIndex)
            {
                AnswererIndicies.Add(answererIndices[i] - 1);
            }
            else
            {
                AnswererIndicies.Add(answererIndices[i]);
            }
        }
    }

    internal void SetSingleAnswerer(int index)
    {
        AnswererIndicies.Clear();
        AnswererIndicies.Add(index);
        AreMultipleAnswerers = false;
    }

    internal void SetMultipleAnswerers(IEnumerable<int> answerers)
    {
        AnswererIndicies.Clear();

        foreach (var item in answerers)
        {
            AnswererIndicies.Add(item);
        }

        AreMultipleAnswerers = true;
    }
}
