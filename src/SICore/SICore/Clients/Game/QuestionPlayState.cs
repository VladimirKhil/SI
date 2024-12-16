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
    /// Should the player answers be validated after right answer.
    /// </summary>
    internal bool ValidateAfterRightAnswer => AnswerOptions != null;

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
