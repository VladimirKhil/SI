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

    internal void Clear() => AnswererIndicies.Clear();

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
