using SIPackages;

namespace SIEngine.QuestionSelectionStrategies;

/// <summary>
/// Allows to select question by removing all other themes in round.
/// First question of left theme is played.
/// </summary>
internal sealed class RemoveOtherThemesStrategy : ISelectionStrategy
{
    private readonly Round _round;
    private readonly EngineOptions _options;
    private readonly ISIEnginePlayHandler _playHandler;
    private readonly Action<int, int> _selectionCallback;

    private Stage _stage = Stage.RoundThemes;

    private readonly HashSet<int> _leftFinalThemesIndicies = new();
    private readonly List<Theme> _finalThemes = new();
    private bool _isFirstPlay = true;

    public RemoveOtherThemesStrategy(Round round, EngineOptions options, ISIEnginePlayHandler playHandler, Action<int, int> selectionCallback)
    {
        _round = round;
        _options = options;
        _playHandler = playHandler;
        _selectionCallback = selectionCallback;

        var themes = _round.Themes;

        for (var i = 0; i < themes.Count; i++)
        {
            if (!string.IsNullOrEmpty(themes[i].Name) && themes[i].Questions.Any())
            {
                _finalThemes.Add(themes[i]);
            }
        }
    }

    public bool ShouldPlayRound() => _finalThemes.Any() && _playHandler.ShouldPlayRoundWithRemovableThemes();

    public bool CanMoveNext() => _stage switch
    {
        Stage.RoundThemes => true,
        Stage.AskDelete => true,
        Stage.ThemeSelected => true,
        _ => false,
    };

    public void MoveNext()
    {
        switch (_stage)
        {
            case Stage.RoundThemes:
                _stage = OnRoundThemes();
                break;

            case Stage.AskDelete:
                _stage = OnAskDelete();
                break;

            case Stage.ThemeSelected:
                _stage = OnThemeSelected();
                break;
        }
    }

    private Stage OnRoundThemes()
    {
        _leftFinalThemesIndicies.Clear();

        for (var i = 0; i < _finalThemes.Count; i++)
        {
            _leftFinalThemesIndicies.Add(i);
        }

        _playHandler.OnFinalThemes(_finalThemes, _options.PlayAllQuestionsInFinalRound, _isFirstPlay);
        return _finalThemes.Count == 1 ? Stage.ThemeSelected : Stage.AskDelete;
    }

    private Stage OnAskDelete()
    {
        _playHandler.AskForThemeDelete(DeleteTheme);
        return Stage.WaitDelete;
    }

    private void DeleteTheme(int themeIndex)
    {
        if (_stage != Stage.WaitDelete)
        {
            return;
        }

        if (!_leftFinalThemesIndicies.Remove(themeIndex))
        {
            return;
        }

        _playHandler.OnThemeDeleted(themeIndex);
        _stage = _leftFinalThemesIndicies.Count == 1 ? Stage.ThemeSelected : Stage.AskDelete;
    }

    private Stage OnThemeSelected()
    {
        var themeIndex = _leftFinalThemesIndicies.First();
        var theme = _finalThemes[themeIndex];
        var publicThemeIndex = _round.Themes.IndexOf(theme);
        var questionIndex = 0;

        _finalThemes.RemoveAt(themeIndex);
        _selectionCallback(publicThemeIndex, questionIndex);
        _playHandler.OnThemeSelected(publicThemeIndex, questionIndex);
        
        if (_options.PlayAllQuestionsInFinalRound && _finalThemes.Any())
        {
            _isFirstPlay = false;
            return Stage.RoundThemes;
        }

        return Stage.End;
    }

    private enum Stage
    {
        RoundThemes,
        AskDelete,
        WaitDelete,
        ThemeSelected,
        End
    }
}
