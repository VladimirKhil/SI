using SIEngine.Rules;
using SImulator.ViewModel.Model;
using SIPackages;
using SIPackages.Core;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;

namespace SImulator.ViewModel.Contracts;

/// <summary>
/// Represents a game presentation controller.
/// </summary>
public interface IPresentationController : IDisposable
{
    Action<int, int>? SelectionCallback { get; set; }
    
    Action<int>? DeletionCallback { get; set; }

    event Action<Exception>? Error;

    /// <summary>
    /// Starts new game.
    /// </summary>
    /// <param name="onLoad">Action to be called when the view is loaded.</param>
    Task StartAsync(Action onLoad);

    /// <summary>
    /// Stops the game.
    /// </summary>
    Task StopAsync();

    void SetGameThemes(IEnumerable<string> themes);

    void SetRoundThemes(ThemeInfoViewModel[] themes, bool isFinal);

    void SetStage(TableStage stage);

    void SetRoundTable();

    void OnPackage(string packageName, MediaInfo? packageLogo);

    void SetRound(string roundName, QuestionSelectionStrategyType selectionStrategyType);

    void SetTheme(string themeName);

    void SetQuestion(int questionPrice);

    void SetText(string text = "");

    void SetQuestionContentType(QuestionContentType questionContentType);

    void SetQuestionStyle(QuestionStyle questionStyle) { }

    void OnContentStart();

    void SetQuestionSound(bool sound);

    void AddPlayer(string playerName);

    void RemovePlayer(int playerIndex);

    void ClearPlayers();

    void UpdatePlayerInfo(int index, PlayerInfo player, string? propertyName = null);

    void UpdateSettings(Settings settings);

    void UpdateShowPlayers(bool showPlayers);

    void SetSound(string sound = "") { }

    void PlaySimpleSelection(int theme, int quest);

    void PlaySelection(int theme);

    void SetActivePlayerIndex(int playerIndex);

    void AddLostButtonPlayerIndex(int playerIndex);

    void ClearPlayersState();

    void SeekMedia(int position);

    void ResumeMedia();

    void StopMedia();

    void RestoreQuestion(int themeIndex, int questionIndex, int price);

    void SetCaption(string caption);
    
    void SetTimerMaxTime(int maxTime);

    void RunTimer();

    void PauseTimer(int currentTime);

    void StopTimer();

    void StopThinkingTimer() { }

    /// <summary>
    /// Sets answer options (invisible by default) and corresponding table layout.
    /// </summary>
    /// <param name="answerOptions">Answer options.</param>
    void SetAnswerOptions(ItemViewModel[] answerOptions);

    /// <summary>
    /// Makes answer options visible on screen.
    /// </summary>
    void ShowAnswerOptions();

    /// <summary>
    /// Sets answer state.
    /// </summary>
    /// <param name="answerIndex">Answer index.</param>
    /// <param name="state">Answer state.</param>
    void SetAnswerState(int answerIndex, ItemState state);

    void OnQuestionStart();

    void BeginPressButton();

    bool OnQuestionContent(
        IReadOnlyCollection<ContentItem> content,
        Func<ContentItem, string?> tryGetMediaUri,
        string? textToShow);

    void FinishQuestion();

    void SetQuestionType(string typeName, string aliasName, int activeThemeIndex);
    
    void SetLanguage(string language) { }

    void SetAppSound(bool isEnabled);

    void SetSimpleAnswer() { }

    void OnAnswerStart();

    void ClearState();

    void OnQuestionEnd() { }

    void PlayerIsRight(int playerIndex);

    void PlayerIsWrong(int playerIndex);

    void NoAnswer();

    void OnFinalThink();
}
