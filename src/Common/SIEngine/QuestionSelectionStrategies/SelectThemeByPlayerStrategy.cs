using SIPackages;

namespace SIEngine.QuestionSelectionStrategies;

/// <summary>
/// Allows player having a turn to select only the theme. All theme questions are played sequentially.
/// </summary>
internal sealed class SelectThemeByPlayerStrategy : ISelectionStrategy, IRoundTableController
{
    private readonly Round _round;
    private readonly ISIEnginePlayHandler _playHandler;
    private readonly Action<int, int> _selectionCallback;

    private readonly SortedSet<int> _availableThemes = [];

    private Stage _stage = Stage.RoundThemes;

    private int _themeIndex = -1, _questionIndex = -1;

    public SelectThemeByPlayerStrategy(Round round, ISIEnginePlayHandler playHandler, Action<int, int> selectionCallback)
    {
        _round = round;
        _playHandler = playHandler;
        _selectionCallback = selectionCallback;

        for (var themeIndex = 0; themeIndex < _round.Themes.Count; themeIndex++)
        {
            if (_round.Themes[themeIndex].Questions.Any(q => q.Price != Question.InvalidPrice))
            {
                _availableThemes.Add(themeIndex);
            }
        }
    }

    public bool ShouldPlayRound() => _availableThemes.Count != 0;

    public bool CanMoveNext() =>
        _stage == Stage.RoundThemes
        || _stage == Stage.AskTheme && _availableThemes.Count != 0
        || _stage == Stage.Question;

    public void MoveNext()
    {
        switch (_stage)
        {
            case Stage.RoundThemes:
                _playHandler.OnRoundThemes(_round.Themes, this);
                _stage = Stage.AskTheme;
                break;

            case Stage.AskTheme:
                OnAskTheme();
                break;

            case Stage.Question:
                OnQuestion();
                break;
        }
    }

    public bool CanMoveBack() => false;

    public void MoveBack()
    {
    }

    public bool RemoveQuestion(int themeIndex, int questionIndex) => false;

    public bool RestoreQuestion(int themeIndex, int questionIndex) => false;

    private void OnAskTheme()
    {
        _stage = Stage.WaitTheme;

        if (_availableThemes.Count == 1)
        {
            SelectTheme(_availableThemes.First());
            return;
        }

        _playHandler.AskForThemeSelection(_availableThemes, SelectTheme);
    }

    private void SelectTheme(int themeIndex)
    {
        if (_stage != Stage.WaitTheme || !_availableThemes.Remove(themeIndex))
        {
            return;
        }

        _themeIndex = themeIndex;
        _questionIndex = -1;
        MoveNextQuestion();

        _stage = Stage.Question;
        _playHandler.OnTheme(_round.Themes[_themeIndex]);
    }

    private void OnQuestion()
    {
        _playHandler.OnQuestion(_round.Themes[_themeIndex].Questions[_questionIndex]);

        var questionIndex = _questionIndex;
        MoveNextQuestion();

        _stage = _questionIndex < _round.Themes[_themeIndex].Questions.Count ? Stage.Question : Stage.AskTheme;
        _selectionCallback(_themeIndex, questionIndex);
    }

    private void MoveNextQuestion()
    {
        do
        {
            _questionIndex++;
        } while (_questionIndex < _round.Themes[_themeIndex].Questions.Count
            && _round.Themes[_themeIndex].Questions[_questionIndex].Price == Question.InvalidPrice);
    }

    private enum Stage
    {
        RoundThemes,
        AskTheme,
        WaitTheme,
        Question
    }
}
