using SIEngine.Rules;

namespace SIEngine.Tests;

internal class SIEnginePlayHandlerMock : ISIEnginePlayHandler
{
    public bool ShouldPlayRound(QuestionSelectionStrategyType questionSelectionStrategyType)
    {
        return true;
    }
}
