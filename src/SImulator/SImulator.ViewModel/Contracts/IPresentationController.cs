using SImulator.ViewModel.Model;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;

namespace SImulator.ViewModel.Contracts;

/// <summary>
/// Represents a game presentation controller.
/// </summary>
public interface IPresentationController : IDisposable
{
    Action<int, int>? SelectionCallback { get; set; }

    /// <summary>
    /// Starts new game.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the game.
    /// </summary>
    void StopGame();

    void SetGameThemes(IEnumerable<string> themes);

    void SetRoundThemes(ThemeInfoViewModel[] themes, bool isFinal);

    void SetMedia(MediaSource media, bool background);

    void SetStage(TableStage stage);

    void SetText(string text = "");

    /// <summary>
    /// Sets content to display.
    /// </summary>
    /// <param name="content">Content to display.</param>
    void SetScreenContent(IReadOnlyCollection<ContentGroup> content);

    void SetQuestionContentType(QuestionContentType questionContentType);

    void SetQuestionStyle(QuestionStyle questionStyle);

    void SetQuestionSound(bool sound);

    void AddPlayer();

    void RemovePlayer(string playerName);

    void ClearPlayers();

    void UpdatePlayerInfo(int index, PlayerInfo player);

    void UpdateSettings(Settings settings);

    void UpdateShowPlayers(bool showPlayers);

    void SetSound(string sound = "");

    void PlaySimpleSelection(int theme, int quest);

    void PlayComplexSelection(int theme, int quest, bool setActive);

    void PlaySelection(int theme);

    void SetActivePlayerIndex(int playerIndex);

    void AddLostButtonPlayerIndex(int playerIndex);

    void ClearPlayersState();

    void SeekMedia(int position);

    void RunMedia();

    void StopMedia();

    void RestoreQuestion(int themeIndex, int questionIndex, int price);

    void SetCaption(string caption);
    
    void SetTimerMaxTime(int maxTime);

    void RunTimer();

    void PauseTimer(int currentTime);

    void StopTimer();

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
}
