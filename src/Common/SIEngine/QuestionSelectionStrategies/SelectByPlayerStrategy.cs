using SIPackages;

namespace SIEngine.QuestionSelectionStrategies;

/// <summary>
/// Allows to select next question by moving player.
/// </summary>
internal sealed class SelectByPlayerStrategy : ISelectionStrategy, IRoundTableController
{
    private readonly Round _round;
    private readonly ISIEnginePlayHandler _playHandler;
    private readonly Action<int, int> _selectionCallback;

    private Stage _stage = Stage.RoundThemes;

    /// <summary>
    /// Represents the table of theme-question indicies that could be selected.
    /// </summary>
    private readonly HashSet<(int, int)> _questionsTable = new();

    private readonly Stack<(int themeIndex, int questionIndex)> _history = new();
    private readonly Stack<(int themeIndex, int questionIndex)> _forward = new();

    public SelectByPlayerStrategy(
        Round round,
        ISIEnginePlayHandler playHandler,
        Action<int, int> selectionCallback)
    {
        _round = round;
        _playHandler = playHandler;
        _selectionCallback = selectionCallback;

        for (var themeIndex = 0; themeIndex < _round.Themes.Count; themeIndex++)
        {
            var themeQuestions = _round.Themes[themeIndex].Questions;

            for (var questionIndex = 0; questionIndex < themeQuestions.Count; questionIndex++)
            {
                if (themeQuestions[questionIndex].Price != Question.InvalidPrice)
                {
                    _questionsTable.Add((themeIndex, questionIndex));
                }
            }
        }
    }

    public bool ShouldPlayRound() => _questionsTable.Any();

    public bool CanMoveNext() => _stage == Stage.RoundThemes || _stage == Stage.RoundTable && _questionsTable.Any() || _stage == Stage.WaitSelection && _forward.Any();

    public void MoveNext()
    {
        switch (_stage)
        {
            case Stage.RoundThemes:
                _stage = OnRoundThemes();
                break;

            case Stage.RoundTable:
                _stage = OnRoundTable();
                break;

            case Stage.WaitSelection:
                OnWaitSelection();
                break;
        }
    }

    public bool CanMoveBack() => _history.Any();

    public (int themeIndex, int questionIndex) MoveBack()
    {
        var (themeIndex, questionIndex) = _history.Pop();
        _forward.Push((themeIndex, questionIndex));
        _questionsTable.Add((themeIndex, questionIndex));

        return (themeIndex, questionIndex);
    }

    public bool RemoveQuestion(int themeIndex, int questionIndex)
    {
        var result = _questionsTable.Remove((themeIndex, questionIndex));

        if (result && _stage == Stage.WaitSelection && !_questionsTable.Any())
        {
            _playHandler.CancelQuestionSelection();
            _stage = Stage.RoundTable;
        }

        return result;
    }

    public int? RestoreQuestion(int themeIndex, int questionIndex)
    {
        if (themeIndex < 0 || themeIndex >= _round.Themes.Count)
        {
            return null;
        }

        if (questionIndex < 0 || questionIndex >= _round.Themes[themeIndex].Questions.Count)
        {
            return null;
        }

        if (_round.Themes[themeIndex].Questions[questionIndex].Price == Question.InvalidPrice)
        {
            return null;
        }

        _questionsTable.Add((themeIndex, questionIndex));
        return _round.Themes[themeIndex].Questions[questionIndex].Price;
    }

    private Stage OnRoundThemes()
    {
        _playHandler.OnRoundThemes(_round.Themes, this);
        return Stage.RoundTable;
    }

    private Stage OnRoundTable()
    {
        _playHandler.AskForQuestionSelection(_questionsTable, SelectQuestion);
        return Stage.WaitSelection;
    }

    private void OnWaitSelection()
    {
        if (_forward.Count <= 0)
        {
            return;
        }

        var (themeIndex, questionIndex) = _forward.Pop();
        SelectQuestion(themeIndex, questionIndex);
    }

    private void SelectQuestion(int themeIndex, int questionIndex)
    {
        _history.Push((themeIndex, questionIndex));
        _questionsTable.Remove((themeIndex, questionIndex));
        _selectionCallback(themeIndex, questionIndex);
        _playHandler.OnQuestionSelected(themeIndex, questionIndex);

        _stage = Stage.RoundTable;
    }

    private enum Stage
    {
        RoundThemes,
        RoundTable,
        WaitSelection
    }
}
