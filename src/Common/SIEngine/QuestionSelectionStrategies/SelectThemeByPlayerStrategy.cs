using SIPackages;

namespace SIEngine.QuestionSelectionStrategies;

/// <summary>
/// Allows player having a turn to select only the theme. All theme questions are played sequentially.
/// </summary>
internal sealed class SelectThemeByPlayerStrategy : ISelectionStrategy
{
    private readonly Round _round;
    private readonly ISIEnginePlayHandler _playHandler;
    private readonly Action<int, int> _selectionCallback;

    private readonly SortedSet<int> _availableThemes = [];
    private readonly List<int> _listedThemes = [];

    private Stage _stage = Stage.ThemeList;
    private bool _isFirstPlay = true;

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
        _stage == Stage.ThemeList && _availableThemes.Count != 0
        || _stage == Stage.AskTheme
        || _stage == Stage.Question;

    public void MoveNext()
    {
        switch (_stage)
        {
            case Stage.ThemeList:
                OnThemeList();
                break;

            case Stage.AskTheme:
                OnAskTheme();
                break;

            case Stage.Question:
                OnQuestion();
                break;
        }
    }

    private void OnThemeList()
    {
        _listedThemes.Clear();
        _listedThemes.AddRange(_availableThemes);

        _playHandler.OnThemeList(_listedThemes.Select(index => _round.Themes[index]).ToList(), true, _isFirstPlay);
        _isFirstPlay = false;
        _stage = Stage.AskTheme;
    }

    private void OnAskTheme()
    {
        _stage = Stage.WaitTheme;

        if (_listedThemes.Count == 1)
        {
            SelectListedTheme(0);
            return;
        }

        _playHandler.AskForThemeSelection(Enumerable.Range(0, _listedThemes.Count).ToArray(), SelectListedTheme);
    }

    private void SelectListedTheme(int listedIndex)
    {
        if (listedIndex < 0 || listedIndex >= _listedThemes.Count)
        {
            return;
        }

        SelectTheme(_listedThemes[listedIndex]);
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

        _stage = _questionIndex < _round.Themes[_themeIndex].Questions.Count ? Stage.Question : Stage.ThemeList;
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
        ThemeList,
        AskTheme,
        WaitTheme,
        Question
    }
}
