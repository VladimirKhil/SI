using SIEngine.Core;

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
    /// Answer options.
    /// </summary>
    internal AnswerOption[]? AnswerOptions { get; set; }

    /// <summary>
    /// Has layout been set.
    /// </summary>
    internal bool LayoutShown { get; set; }

    /// <summary>
    /// Are answer options shown.
    /// </summary>
    internal bool AnswerOptionsShown { get; set; }

    internal void Clear()
    {
        AnswererIndicies.Clear();
        AnswerOptions = null;
        LayoutShown = false;
        AnswerOptionsShown = false;
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
    }

    internal void SetMultipleAnswerers(IEnumerable<int> answerers)
    {
        AnswererIndicies.Clear();

        foreach (var item in answerers)
        {
            AnswererIndicies.Add(item);
        }
    }
}
