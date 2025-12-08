using SICore.Models;
using SICore.Utils;
using SIEngine;
using SIEngine.Models;
using SIEngine.Rules;
using SIPackages;
using SIUI.Model;

namespace SICore.Clients.Game;

internal sealed class PlayHandler : ISIEnginePlayHandler
{
    private readonly GameData _state;

    public GameLogic GameLogic { get; internal set; } = null!;

    public GameActions? GameActions { get; internal set; }

    public PlayHandler(GameData state) => _state = state;

    public void OnRound(Round round, QuestionSelectionStrategyType strategyType)
    {
        GameLogic?.OnRoundStart(round, strategyType);
        // TODO: I do not like ANY logic dependency on strategyType outside the strategy factory but do not have any better idea for now
        _state.PlayersValidator = strategyType == QuestionSelectionStrategyType.RemoveOtherThemes ? ThemeRemovalPlayersValidator : DefaultPlayersValidator;
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
        else
        {
            GameLogic?.ScheduleExecution(Tasks.MoveNext, 10);
        }
    }

    public static bool DefaultPlayersValidator() => true;

    public bool ThemeRemovalPlayersValidator() => _state.Players.Any(player => player.InGame);

    public void OnRoundThemes(IReadOnlyList<Theme> themes, IRoundTableController tableController)
    {
        if (themes.Count == 0)
        {
            throw new ArgumentException("Themes collection is empty", nameof(themes));
        }

        _state.Themes = themes;

        // _gameData.TInfo.RoundInfo is initialized here
        GameLogic?.InitThemes(themes, false, true, ThemesPlayMode.OneByOne);
        _state.InformStages |= InformStages.RoundThemesComments;

        // Filling initial questions table
        var maxQuestionsInTheme = themes.Max(t => t.Questions.Count);

        for (var i = 0; i < themes.Count; i++)
        {
            var questionsCount = themes[i].Questions.Count;
            _state.TInfo.RoundInfo[i].Questions.Clear();

            for (var j = 0; j < maxQuestionsInTheme; j++)
            {
                _state.TInfo.RoundInfo[i].Questions.Add(
                    new QuestionInfo
                    {
                        Price = j < questionsCount ? themes[i].Questions[j].Price : Question.InvalidPrice
                    });
            }
        }

        _state.TableController = tableController;
        _state.IsQuestionAskPlaying = false;
        GameLogic?.ScheduleExecution(Tasks.RoundTheme, 1, 0, true);
    }

    public bool ShouldPlayRoundWithRemovableThemes()
    {
        var playRound = false;

        for (var i = 0; i < _state.Players.Count; i++)
        {
            if (_state.Players[i].Sum <= 0)
            {
                _state.Players[i].InGame = false;
            }
            else
            {
                playRound = true;
                _state.Players[i].InGame = true;
            }
        }

        if (_state.Settings.AppSettings.AllowEveryoneToPlayHiddenStakes && !playRound)
        {
            // Nobody has positive score, but we allow everybody to play and delete themes
            for (var i = 0; i < _state.Players.Count; i++)
            {
                _state.Players[i].InGame = true;
            }
            
            playRound = true;
        }

        return playRound;
    }

    public void AskForQuestionSelection(IReadOnlyCollection<(int, int)> options, Action<int, int> selectCallback)
    {
        GameLogic?.SetContinuation(() => selectCallback(_state.ThemeIndex, _state.QuestionIndex));
        GameLogic?.ScheduleExecution(Tasks.AskToSelectQuestion, 1, force: true); // TODO: why not calling AskToSelectQuestion directly?
    }

    public void OnQuestionSelected(int themeIndex, int questionIndex) => GameLogic?.OnQuestionSelected(themeIndex, questionIndex);

    public void OnFinalThemes(IReadOnlyList<Theme> themes, bool willPlayAllThemes, bool isFirstPlay)
    {
        InformActivePlayers();

        GameLogic?.InitThemes(themes, willPlayAllThemes, isFirstPlay, ThemesPlayMode.AllTogether);
        _state.ThemeDeleters = new ThemeDeletersEnumerator(_state.Players, _state.TInfo.RoundInfo.Count(t => t.Name != null));
        _state.ThemeDeleters.Reset(true);
        GameLogic?.ScheduleExecution(Tasks.MoveNext, 30 + Random.Shared.Next(10));
    }

    private void InformActivePlayers()
    {
        var passed = new List<object>();

        for (var i = 0; i < _state.Players.Count; i++)
        {
            if (!_state.Players[i].InGame)
            {
                passed.Add(i);
            }
        }

        if (passed.Count > 0)
        {
            var msg = new MessageBuilder(Messages.PlayerState, PlayerState.Pass).AddRange(passed);
            GameActions?.SendMessage(msg.ToString());
        }
    }

    public void AskForThemeDelete(Action<int> deleteCallback)
    {
        GameLogic?.SetContinuation(() => deleteCallback(_state.ThemeIndexToDelete));
        GameLogic?.ScheduleExecution(Tasks.AskToDelete, 1, force: true); // TODO: why not calling AskToDelete directly?
    }

    public void OnThemeDeleted(int themeIndex) => GameLogic?.OnThemeDeleted(themeIndex);

    public void OnThemeSelected(int themeIndex, int questionIndex)
    {
        if (_state.Round == null)
        {
            throw new InvalidOperationException("_gameData.Round == null");
        }

        GameLogic?.AddHistory("::OnThemeSelected");
        _state.ThemeIndex = themeIndex;
        _state.Theme = _state.Round.Themes[themeIndex];
        _state.ThemesPlayMode = ThemesPlayMode.None;

        _state.QuestionIndex = questionIndex;
        _state.Question = _state.Theme.Questions[_state.QuestionIndex];

        GameLogic?.AnnounceFinalTheme(_state.Question);
        _state.InformStages |= InformStages.Theme;
    }

    public void OnTheme(Theme theme)
    {
        _state.Theme = theme;
        GameActions?.SendThemeInfo();
        _state.InformStages |= InformStages.Theme;
        GameLogic?.ScheduleExecution(Tasks.MoveNext, 20);
    }

    public void OnQuestion(Question question) => GameLogic?.OnQuestion(question);

    public void OnQuestionType(string typeName, bool isDefault)
    {
        _state.QuestionTypeName = typeName;
        GameLogic?.OnQuestionType(typeName, isDefault);
    }

    public void OnQuestionRestored(int themeIndex, int questionIndex, int price)
    {
        var question = _state.TInfo.RoundInfo[themeIndex].Questions[questionIndex];
        question.Price = price;
        GameActions?.SendMessageWithArgs(Messages.Toggle, themeIndex, questionIndex, price);
    }

    public bool OnQuestionEnd(string comments) => GameLogic.OnQuestionEnd();

    public void OnPackage(Package package) => GameLogic?.OnPackage(package);

    public void OnGameThemes(IEnumerable<string> themes) => GameLogic?.OnGameThemes(themes);

    public void OnPackageEnd() => GameLogic?.OnEndGame();
}
