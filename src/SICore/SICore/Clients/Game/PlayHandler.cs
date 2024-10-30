using SICore.Utils;
using SIEngine;
using SIEngine.Models;
using SIEngine.Rules;
using SIPackages;
using SIUI.Model;

namespace SICore.Clients.Game;

internal sealed class PlayHandler : ISIEnginePlayHandler
{
    private readonly GameData _gameData;

    public GameLogic? GameLogic { get; internal set; }

    public GameActions? GameActions { get; internal set; }

    public PlayHandler(GameData gameData) => _gameData = gameData;

    public void OnRound(Round round, QuestionSelectionStrategyType strategyType)
    {
        GameLogic?.OnRoundStart(round, strategyType);
        // TODO: I do not like ANY logic dependency on strategyType outside the strategy factory but do not have any better idea for now
        _gameData.PlayersValidator = strategyType == QuestionSelectionStrategyType.RemoveOtherThemes ? ThemeRemovalPlayersValidator : DefaultPlayersValidator;
    }

    public void OnRoundEnd(RoundEndReason reason)
    {
        switch (reason)
        {
            case RoundEndReason.Completed:
                GameLogic?.OnRoundEmpty();
                break;
            
            case RoundEndReason.Timeout:
                GameLogic?.OnRoundTimeout();
                break;
            
            case RoundEndReason.Manual:
                GameLogic?.OnRoundEndedManually();
                break;
            
            default:
                break;
        }

        GameLogic?.OnRoundEnded();
    }

    public void OnRoundSkip(QuestionSelectionStrategyType strategyType)
    {
        if (strategyType == QuestionSelectionStrategyType.RemoveOtherThemes)
        {
            GameLogic?.OnFinalRoundSkip();
        }
    }

    public static bool DefaultPlayersValidator() => true;

    public bool ThemeRemovalPlayersValidator() => _gameData.Players.Any(player => player.InGame);

    public void OnRoundThemes(IReadOnlyList<Theme> themes, IRoundTableController tableController)
    {
        if (themes.Count == 0)
        {
            throw new ArgumentException("Themes collection is empty", nameof(themes));
        }

        // _gameData.TInfo.RoundInfo is initialized here
        GameLogic?.InitThemes(themes, false, true, Models.ThemesPlayMode.OneByOne);

        // Filling initial questions table
        _gameData.ThemeInfoShown.Clear();

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

        _gameData.TableInformStageLock.WithLock(
            () =>
            {
                GameActions?.InformTable();
                _gameData.TableInformStage = 2;
            },
            5000);

        _gameData.TableController = tableController;
        _gameData.IsQuestionAskPlaying = false;
        GameLogic?.ScheduleExecution(Tasks.AskFirst, 19 * _gameData.TInfo.RoundInfo.Count + Random.Shared.Next(10));
    }

    public bool ShouldPlayRoundWithRemovableThemes()
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
        GameLogic?.ScheduleExecution(Tasks.AskToChoose, 1, force: true); // TODO: why not calling AskToChoose directly?
    }

    public void OnQuestionSelected(int themeIndex, int questionIndex) => GameLogic?.OnQuestionSelected(themeIndex, questionIndex);

    public void OnFinalThemes(IReadOnlyList<Theme> themes, bool willPlayAllThemes, bool isFirstPlay)
    {
        InformActivePlayers();

        GameLogic?.InitThemes(themes, willPlayAllThemes, isFirstPlay, Models.ThemesPlayMode.AllTogether);
        _gameData.ThemeDeleters = new ThemeDeletersEnumerator(_gameData.Players, _gameData.TInfo.RoundInfo.Count(t => t.Name != null));
        _gameData.ThemeDeleters.Reset(true);
        GameLogic?.ScheduleExecution(Tasks.MoveNext, 30 + Random.Shared.Next(10));
    }

    private void InformActivePlayers()
    {
        var s = new MessageBuilder(Messages.FinalRound);

        for (var i = 0; i < _gameData.Players.Count; i++)
        {
            s.Add(_gameData.Players[i].InGame ? '+' : '-');
        }

        GameActions?.SendMessage(s.ToString());
    }

    public void AskForThemeDelete(Action<int> deleteCallback)
    {
        GameLogic?.SetContinuation(() => deleteCallback(_gameData.ThemeIndexToDelete));
        GameLogic?.ScheduleExecution(Tasks.AskToDelete, 1);
    }

    public void OnThemeDeleted(int themeIndex) => GameLogic?.OnThemeDeleted(themeIndex);

    public void OnThemeSelected(int themeIndex, int questionIndex)
    {
        if (_gameData.Round == null)
        {
            throw new InvalidOperationException("_gameData.Round == null");
        }

        GameLogic?.AddHistory("::OnThemeSelected");
        _gameData.ThemeIndex = themeIndex;
        _gameData.Theme = _gameData.Round.Themes[themeIndex];

        _gameData.QuestionIndex = questionIndex;
        _gameData.Question = _gameData.Theme.Questions[_gameData.QuestionIndex];

        GameLogic?.AnnounceFinalTheme();
    }

    public void OnTheme(Theme theme)
    {
        _gameData.Theme = theme;
        GameActions?.SendThemeInfo();
        GameLogic?.ScheduleExecution(Tasks.ThemeInfo, 20, 1);
    }

    public void OnQuestion(Question question) => GameLogic?.OnQuestion(question);

    public void OnQuestionType(string typeName, bool isDefault)
    {
        _gameData.QuestionTypeName = typeName;
        GameLogic?.OnQuestionType(isDefault);
    }

    public void OnQuestionRestored(int themeIndex, int questionIndex, int price)
    {
        var question = _gameData.TInfo.RoundInfo[themeIndex].Questions[questionIndex];
        question.Price = price;
        GameActions?.SendMessageWithArgs(Messages.Toggle, themeIndex, questionIndex, price);
    }
}
