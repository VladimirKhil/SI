using SIPackages;

namespace SIEngine.QuestionSelectionStrategies;

/// <summary>
/// Plays questions one by one.
/// </summary>
internal sealed class SequentialStrategy : ISelectionStrategy
{
    private readonly Round _round;
    private readonly ISIEnginePlayHandler _playHandler;
    private readonly Action<int, int> _selectionCallback;

    private Stage _stage = Stage.Theme;

    private int _themeIndex = -1, _questionIndex = -1;

    public SequentialStrategy(Round round, ISIEnginePlayHandler playHandler, Action<int, int> selectionCallback)
    {
        _round = round;
        _playHandler = playHandler;
        _selectionCallback = selectionCallback;

        MoveNextTheme();
    }
    public bool ShouldPlayRound() => CanMoveNext();

    public bool CanMoveNext() => _themeIndex < _round.Themes.Count;

    public void MoveNext()
    {
        switch (_stage)
        {
            case Stage.Theme:
                _stage = OnTheme();
                break;

            case Stage.Question:
                _stage = OnQuestion();
                break;
        }
    }

    private Stage OnTheme()
    {
        _playHandler.OnTheme(_round.Themes[_themeIndex]);
        MoveNextQuestion();
        return Stage.Question;
    }

    private Stage OnQuestion()
    {
        _playHandler.OnQuestion(_round.Themes[_themeIndex].Questions[_questionIndex]);
        _selectionCallback(_themeIndex, _questionIndex);

        MoveNextQuestion();

        if (_questionIndex < _round.Themes[_themeIndex].Questions.Count)
        {
            return Stage.Question;
        }

        MoveNextTheme();
        _questionIndex = -1;
        return Stage.Theme;
    }

    private void MoveNextTheme()
    {
        do
        {
            _themeIndex++;
        } while (_themeIndex < _round.Themes.Count
            && !_round.Themes[_themeIndex].Questions.Any(q => q.Price != Question.InvalidPrice));
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
        Theme,
        Question
    }
}
