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

    public GameController Controller { get; internal set; } = null!;

    public GameActions GameActions { get; internal set; } = null!;

    public PlayHandler(GameData state) => _state = state;

    public void OnRound(Round round, QuestionSelectionStrategyType strategyType)
    {
        Controller.OnRoundStart(round, strategyType);
        // TODO: I do not like ANY logic dependency on strategyType outside the strategy factory but do not have any better idea for now
        _state.PlayersValidator = strategyType == QuestionSelectionStrategyType.RemoveOtherThemes ? ThemeRemovalPlayersValidator : DefaultPlayersValidator;
    }

    public void OnRoundEnd(RoundEndReason reason)
    {
        switch (reason)
        {
            case RoundEndReason.Completed:
                Controller.OnRoundEmpty();
                break;
            
            case RoundEndReason.Timeout:
                Controller.OnRoundTimeout();
                break;
            
            case RoundEndReason.Manual:
                Controller.OnRoundEndedManually();
                break;
            
            default:
                break;
        }

        Controller.OnRoundEnded();
    }

    public void OnRoundSkip(QuestionSelectionStrategyType strategyType)
    {
        if (strategyType == QuestionSelectionStrategyType.RemoveOtherThemes)
        {
            Controller.OnFinalRoundSkip();
        }
        else
        {
            Controller.ScheduleExecution(Tasks.MoveNext, 10);
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
        Controller.InitThemes(themes, false, true, ThemesPlayMode.OneByOne);
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
        Controller.ScheduleExecution(Tasks.RoundTheme, 1, 0, true);
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
        Controller.SetContinuation(() => selectCallback(_state.ThemeIndex, _state.QuestionIndex));
        Controller.ScheduleExecution(Tasks.AskToSelectQuestion, 1, force: true); // TODO: why not calling AskToSelectQuestion directly?
    }

    public void OnQuestionSelected(int themeIndex, int questionIndex) => Controller.OnQuestionSelected(themeIndex, questionIndex);

    public void OnFinalThemes(IReadOnlyList<Theme> themes, bool willPlayAllThemes, bool isFirstPlay)
    {
        InformActivePlayers();

        Controller.InitThemes(themes, willPlayAllThemes, isFirstPlay, ThemesPlayMode.AllTogether);
        _state.ThemeDeleters = new ThemeDeletersEnumerator(_state.Players, _state.TInfo.RoundInfo.Count(t => t.Name != null));
        _state.ThemeDeleters.Reset(true);
        Controller.ScheduleExecution(Tasks.MoveNext, 30 + Random.Shared.Next(10));
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
            GameActions.SendMessage(msg.ToString());
        }
    }

    public void AskForThemeDelete(Action<int> deleteCallback)
    {
        Controller.SetContinuation(() => deleteCallback(_state.ThemeIndexToDelete));
        Controller.ScheduleExecution(Tasks.AskToDelete, 1, force: true); // TODO: why not calling AskToDelete directly?
    }

    public void OnThemeDeleted(int themeIndex) => Controller.OnThemeDeleted(themeIndex);

    public void OnThemeSelected(int themeIndex, int questionIndex)
    {
        if (_state.Round == null)
        {
            throw new InvalidOperationException("_gameData.Round == null");
        }

        Controller.AddHistory("::OnThemeSelected");
        _state.ThemeIndex = themeIndex;
        _state.Theme = _state.Round.Themes[themeIndex];
        _state.ThemesPlayMode = ThemesPlayMode.None;

        _state.QuestionIndex = questionIndex;
        _state.Question = _state.Theme.Questions[_state.QuestionIndex];

        Controller.AnnounceFinalTheme(_state.Question);
        _state.InformStages |= InformStages.Theme;
    }

    public void OnTheme(Theme theme)
    {
        _state.Theme = theme;
        GameActions.SendThemeInfo();
        _state.InformStages |= InformStages.Theme;
        Controller.ScheduleExecution(Tasks.MoveNext, 20);
    }

    public void OnQuestion(Question question) => Controller.OnQuestion(question);

    public void OnQuestionType(string typeName, bool isDefault)
    {
        _state.QuestionTypeName = typeName;
        Controller.OnQuestionType(typeName, isDefault);
    }

    public void OnQuestionRestored(int themeIndex, int questionIndex, int price)
    {
        var question = _state.TInfo.RoundInfo[themeIndex].Questions[questionIndex];
        question.Price = price;
        GameActions.SendMessageWithArgs(Messages.Toggle, themeIndex, questionIndex, price);
    }

    public bool OnQuestionEnd(string comments) => Controller.OnQuestionEnd();

    public void OnPackage(Package package) => Controller.OnPackage(package);

    public void OnGameThemes(IEnumerable<string> themes) => Controller.OnGameThemes(themes);

    public void OnPackageEnd() => Controller.OnEndGame();
}
