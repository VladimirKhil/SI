using SIPackages;

namespace SIEngine.QuestionSelectionStrategies;

internal sealed class RemoveOtherThemesStrategy : ISelectionStrategy
{
    private readonly Round _round;
    private readonly ISIEnginePlayHandler _playHandler;
    private readonly Action<int, int> _selectionCallback;

    public RemoveOtherThemesStrategy(Round round, ISIEnginePlayHandler playHandler, Action<int, int> selectionCallback)
    {
        _round = round;
        _playHandler = playHandler;
        _selectionCallback = selectionCallback;
    }

    public bool ShouldPlayRound() => _round.Themes.Any(theme => theme.Name != null) && _playHandler.ShouldPlayQuestionForAll();
}
