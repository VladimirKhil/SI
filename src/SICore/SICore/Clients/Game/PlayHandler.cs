using SIEngine;
using SIPackages;
using SIUI.Model;

namespace SICore.Clients.Game;

internal sealed class PlayHandler : ISIEnginePlayHandler
{
    private readonly GameData _gameData;

    public GameLogic? GameLogic { get; internal set; }

    public GameActions? GameActions { get; internal set; }

    public PlayHandler(GameData gameData) => _gameData = gameData;

    public void OnRoundThemes(IReadOnlyList<Theme> themes, IRoundTableController tableController)
    {
        if (themes.Count == 0)
        {
            throw new ArgumentException("Themes collection is empty", nameof(themes));
        }

        // _gameData.TInfo.RoundInfo is initialized here
        GameLogic?.InitThemes(themes, false, true);

        // Filling initial questions table
        _gameData.ThemeInfoShown = new bool[themes.Count];

        var maxQuestionsInTheme = themes.Max(t => t.Questions.Count);

        for (var i = 0; i < themes.Count; i++)
        {
            var questionsCount = themes[i].Questions.Count;
            _gameData.TInfo.RoundInfo[i].Questions.Clear();

            for (var j = 0; j < maxQuestionsInTheme; j++)
            {
                _gameData.TInfo.RoundInfo[i].Questions.Add(
                    new QuestionInfo
                    {
                        Price = j < questionsCount ? themes[i].Questions[j].Price : Question.InvalidPrice
                    });
            }
        }

        _gameData.TableInformStageLock.WithLock(() =>
        {
            GameActions?.InformTable();
            _gameData.TableInformStage = 2;
        },
        5000);

        _gameData.TableController = tableController;
        _gameData.IsQuestionPlaying = false;
        GameLogic?.ScheduleExecution(Tasks.AskFirst, 19 * _gameData.TInfo.RoundInfo.Count + Random.Shared.Next(10));
    }

    public bool ShouldPlayQuestionForAll()
    {
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

    public void AskForQuestionSelection(IReadOnlyCollection<(int, int)> options, Action<int, int> selectCallback)
    {
        GameLogic?.SetContinuation(() => selectCallback(_gameData.ThemeIndex, _gameData.QuestionIndex));
        GameLogic?.ScheduleExecution(Tasks.AskToChoose, 20);
    }

    public void CancelQuestionSelection()
    {
        GameLogic?.ClearContinuation();
        GameLogic?.PlanExecution(Tasks.MoveNext, 1);
    }

    public void OnQuestionSelected(int themeIndex, int questionIndex) => GameLogic?.OnQuestionSelected(themeIndex, questionIndex);
}
