using SICore.Models;
using SIData;

namespace SICore;

/// <summary>
/// Defines a game person controller.
/// </summary>
public interface IPersonController
{
    bool CanSwitchType { get; }

    /// <summary>
    /// Получение сообщений
    /// </summary>
    void ReceiveText(Message m);

    /// <summary>
    /// Новое состояние игры
    /// </summary>
    void Stage();

    /// <summary>
    /// Game themes received.
    /// </summary>
    void GameThemes() { }

    /// <summary>
    /// Handles round themes.
    /// </summary>
    /// <param name="playMode">Themes play mode.</param>
    void RoundThemes(ThemesPlayMode playMode) { }

    /// <summary>
    /// Handles question selection.
    /// </summary>
    void OnQuestionSelected(int themeIndex, int questionIndex);

    /// <summary>
    /// Handles right answer or label.
    /// </summary>
    /// <param name="answer">Right answer or label.</param>
    void OnRightAnswer(string answer);

    /// <summary>
    /// Handles right complex answer start.
    /// </summary>
    /// <param name="answer">Simple answer text.</param>
    void OnRightAnswerStart(string answer) { }

    void Resume();

    /// <summary>
    /// Можно жать на кнопку
    /// </summary>
    void Try(bool questionNotFinished) { }

    /// <summary>
    /// Жать уже нельзя
    /// </summary>
    void EndTry(string text);

    /// <summary>
    /// Показать табло
    /// </summary>
    void ShowTablo();

    /// <summary>
    /// Person score changed.
    /// </summary>
    void OnPersonScoreChanged(int playerIndex, bool isRight, int sum) { }

    /// <summary>
    /// Handles question start.
    /// </summary>
    /// <param name="isDefaultType">Does the question have a default type for the current round.</param>
    void OnQuestionStart(bool isDefaultType) { }

    /// <summary>
    /// Завершение раунда
    /// </summary>
    void StopRound();

    /// <summary>
    /// Удалена тема
    /// </summary>
    void Out(int themeIndex);

    /// <summary>
    /// Winner is defined.
    /// </summary>
    void OnWinner(int winnerIndex) { }

    /// <summary>
    /// Время вышло
    /// </summary>
    void TimeOut();

    /// <summary>
    /// Размышления в финале
    /// </summary>
    void FinalThink();

    void OnAd(string? text = null) { }

    /// <summary>
    /// Обновление изображения
    /// </summary>
    /// <param name="i"></param>
    /// <param name="path"></param>
    void UpdatePicture(Account account, string path);

    void OnTextSpeed(double speed);

    void SetText(string text, bool animate = false, TableStage stage = TableStage.Round) { }

    void OnPauseChanged(bool isPaused);

    void TableLoaded();

    void PrintGreeting();

    void OnTextShape(string[] mparams) { }

    void OnTimeChanged();

    void OnTimerChanged(int timerIndex, string timerCommand, string arg, string? person = null);

    void OnPersonFinalStake(int playerIndex);

    void OnPersonPass(int playerIndex);

    void OnReplic(string personCode, string text);

    void OnRoundContent(string[] mparams) { }

    void OnAtomHint(string hint) { }

    void ReloadMedia() { }

    void OnBannedList(IEnumerable<BannedInfo> banned) { }

    void OnBanned(BannedInfo bannedInfo) { }

    void OnUnbanned(string ip) { }

    void SetCaption(string caption) { }

    void OnGameMetadata(string gameName, string packageName, string contactUri, string voiceChatUri) { }

    void AddPlayer(PlayerAccount account) { }

    void RemovePlayerAt(int index) { }

    void OnInfo() { }

    void OnAnswerOptions(bool questionHasScreenContent, IEnumerable<string> optionsTypes) { }

    void OnContent(string[] mparams) { }

    void OnContentAppend(string[] mparams) { }

    void OnContentState(string[] mparams) { }

    /// <summary>
    /// Clears all possible selection options.
    /// </summary>
    /// <param name="full">Should game table selection be cleared too.</param>
    void ClearSelections(bool full = false);

    void ClearQuestionState() { }

    void OnThemeComments(string comments) { }

    void UpdateAvatar(PersonAccount person, string contentType, string uri) { }

    void OnContentShape(string shape) { }

    void OnOptions2(string[] mparams) { }

    void OnToggle(int themeIndex, int questionIndex, int price) { }

    void OnStopPlay() { }

    void OnSelectPlayer(SelectPlayerReason reason) { }

    /// <summary>
    /// Selects question on game table.
    /// </summary>
    void SelectQuestion();

    void OnEnableButton() { }

    void OnDisableButton() { }

    void OnSetJoinMode(JoinMode joinMode) { }

    void SelectPlayer() { }

    /// <summary>
    /// Validates the answer.
    /// </summary>
    void IsRight(string name, bool voteForRight, string answer);

    /// <summary>
    /// Validates the answer.
    /// </summary>
    /// <param name="playerIndex">Player index.</param>
    /// <param name="answer">Player answer.</param>
    void ValidateAnswer(int playerIndex, string answer);

    /// <summary>
    /// Reacts to sending answer request.
    /// </summary>
    void Answer() { }

    /// <summary>
    /// Handles game hint.
    /// </summary>
    /// <param name="hint">Game hint.</param>
    void OnHint(string hint) { }

    /// <summary>
    /// Handles start of thinking.
    /// </summary>
    void StartThink() { }

    /// <summary>
    /// Handles end of thinking.
    /// </summary>
    void EndThink();

    /// <summary>
    /// Handles game report request.
    /// </summary>
    /// <param name="report">Report text.</param>
    void Report(string report);

    /// <summary>
    /// Handles game theme.
    /// </summary>
    /// <param name="themeName">Theme name.</param>
    /// <param name="questionCount">Number of questions in the theme.</param>
    /// <param name="animate">Should theme be animated.</param>
    void OnTheme(string themeName, int questionCount, bool animate);
    
    /// <summary>
    /// Handles player answering outcome.
    /// </summary>
    void OnPlayerOutcome(int playerIndex, bool isRight);

    /// <summary>
    /// Handles game closing.
    /// </summary>
    void OnGameClosed() { }

    void OnCanPressButton() { }

    void OnPersonConnected() { }

    void OnPersonDisconnected() { }

    void OnPersonStake(int stakerIndex) { }

    void OnClientSwitch(IViewerClient viewer) { }

    void DeleteTheme();

    void MakeStake();

    void AddLog(string message) { }

    void OnSelfDisconnected() { }

    void OnHostChanged(string? initiator, string newHost) { }

    void OnPersonsUpdated() { }

    void OnThemeInfo(string themeName) { }

    void OnQuestionAuthors(IEnumerable<string> authors) { }

    void OnQuestionSources(IEnumerable<string> sources) { }

    void OnShowmanReplic(int messageIndex, MessageCode messageCode) { }
}
