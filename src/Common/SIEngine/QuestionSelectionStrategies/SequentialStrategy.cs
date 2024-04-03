using SIPackages;

namespace SIEngine.QuestionSelectionStrategies;

internal sealed class SequentialStrategy : ISelectionStrategy
{
    private readonly Round _round;
    private readonly ISIEnginePlayHandler _playHandler;
    private readonly Action<int, int> _selectionCallback;

    public SequentialStrategy(Round round, ISIEnginePlayHandler playHandler, Action<int, int> selectionCallback)
    {
        _round = round;
        _playHandler = playHandler;
        _selectionCallback = selectionCallback;
    }
}
