using SIEngine;
using SIEngine.Rules;

namespace SICore.Clients.Game;

internal sealed class PlayHandler : ISIEnginePlayHandler
{
    private readonly GameData _gameData;

    public PlayHandler(GameData gameData) => _gameData = gameData;

    public bool ShouldPlayRound(QuestionSelectionStrategyType questionSelectionStrategyType)
    {
        if (questionSelectionStrategyType != QuestionSelectionStrategyType.RemoveOtherThemes)
        {
            return true;
        }

        var playRound = false;

        for (var i = 0; i < _gameData.Players.Count; i++)
        {
            if (_gameData.Players[i].Sum <= 0)
            {
                _gameData.Players[i].InGame = false;
            }
            else
            {
                playRound = true;
                _gameData.Players[i].InGame = true;
            }
        }

        if (_gameData.Settings.AppSettings.AllowEveryoneToPlayHiddenStakes && !playRound)
        {
            // Nobody has positive score, but we allow everybody to play and delete themes
            for (var i = 0; i < _gameData.Players.Count; i++)
            {
                _gameData.Players[i].InGame = true;
            }
            
            playRound = true;
        }

        return playRound;
    }
}
