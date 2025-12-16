using SIEngine.Rules;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Model;
using SIPackages;
using SIPackages.Core;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;

namespace SImulator.ViewModel.Tests;

/// <summary>
/// Test implementation of IPresentationController that captures all commands issued to web presentation.
/// Used for testing Simulator ViewModel to verify correct command sequence and parameters.
/// </summary>
internal sealed class TestWebPresentationController : IPresentationController
{
    private readonly List<string> _commands = new();
    private readonly object _lock = new();
    
    /// <summary>
    /// Gets all commands issued to the presentation controller in order.
    /// </summary>
    public IReadOnlyList<string> Commands
    {
        get
        {
            lock (_lock)
            {
                return _commands.ToList();
            }
        }
    }

    public Action<int, int>? SelectionCallback { get; set; }
    public Action<int>? DeletionCallback { get; set; }
    public event Action<Exception>? Error;
    public bool CanControlMedia => false;

    private void AddCommand(string command)
    {
        lock (_lock)
        {
            _commands.Add(command);
        }
    }

    public Task StartAsync(Action onLoad)
    {
        AddCommand("StartAsync");
        onLoad?.Invoke();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        AddCommand("StopAsync");
        return Task.CompletedTask;
    }

    public void SetGameThemes(IEnumerable<string> themes)
    {
        AddCommand($"SetGameThemes: {string.Join(", ", themes)}");
    }

    public void SetRoundThemes(string[] themes, bool isFinal)
    {
        AddCommand($"SetRoundThemes: [{string.Join(", ", themes)}], IsFinal={isFinal}");
    }

    public void SetTable(ThemeInfoViewModel[] table)
    {
        AddCommand($"SetTable: {table.Length} themes");
    }

    public void SetStage(TableStage stage)
    {
        AddCommand($"SetStage: {stage}");
    }

    public void SetRoundTable()
    {
        AddCommand("SetRoundTable");
    }

    public void AskToSelectQuestion()
    {
        AddCommand("AskToSelectQuestion");
    }

    public void OnPackage(string packageName, MediaInfo? packageLogo)
    {
        AddCommand($"OnPackage: {packageName}");
    }

    public void SetRound(string roundName, QuestionSelectionStrategyType selectionStrategyType)
    {
        AddCommand($"SetRound: {roundName}, Strategy={selectionStrategyType}");
    }

    public void SetTheme(string themeName, bool animate)
    {
        AddCommand($"SetTheme: {themeName}, Animate={animate}");
    }

    public void SetQuestionPrice(int questionPrice)
    {
        AddCommand($"SetQuestionPrice: {questionPrice}");
    }

    public void SetQuestionComments(string comments)
    {
        AddCommand($"SetQuestionComments: {comments}");
    }

    public void SetText(string text = "")
    {
        AddCommand($"SetText: {text}");
    }

    public void SetQuestionContentType(QuestionContentType questionContentType)
    {
        AddCommand($"SetQuestionContentType: {questionContentType}");
    }

    public void SetQuestionStyle(QuestionStyle questionStyle)
    {
        AddCommand($"SetQuestionStyle: {questionStyle}");
    }

    public void OnContentStart()
    {
        AddCommand("OnContentStart");
    }

    public void OnSimpleRightAnswer(string answer)
    {
        AddCommand($"OnSimpleRightAnswer: {answer}");
    }

    public void OnComplexRightAnswer(string answer)
    {
        AddCommand($"OnComplexRightAnswer: {answer}");
    }

    public void SetQuestionSound(bool sound)
    {
        AddCommand($"SetQuestionSound: {sound}");
    }

    public void AddPlayer(string playerName)
    {
        AddCommand($"AddPlayer: {playerName}");
    }

    public void RemovePlayer(int playerIndex)
    {
        AddCommand($"RemovePlayer: {playerIndex}");
    }

    public void ClearPlayers()
    {
        AddCommand("ClearPlayers");
    }

    public void UpdatePlayerInfo(int index, PlayerInfo player, string? propertyName = null)
    {
        AddCommand($"UpdatePlayerInfo: Index={index}, Name={player.Name}, Property={propertyName}");
    }

    public void UpdateSettings(Settings settings)
    {
        AddCommand("UpdateSettings");
    }

    public void UpdateShowPlayers(bool showPlayers)
    {
        AddCommand($"UpdateShowPlayers: {showPlayers}");
    }

    public void SetSound(string sound = "")
    {
        AddCommand($"SetSound: {sound}");
    }

    public void PlaySimpleSelection(int theme, int quest)
    {
        AddCommand($"PlaySimpleSelection: Theme={theme}, Question={quest}");
    }

    public void PlaySelection(int theme)
    {
        AddCommand($"PlaySelection: Theme={theme}");
    }

    public void SetActivePlayerIndex(int playerIndex)
    {
        AddCommand($"SetActivePlayerIndex: {playerIndex}");
    }

    public void AddLostButtonPlayerIndex(int playerIndex)
    {
        AddCommand($"AddLostButtonPlayerIndex: {playerIndex}");
    }

    public void ClearPlayersState()
    {
        AddCommand("ClearPlayersState");
    }

    public void SeekMedia(int position)
    {
        AddCommand($"SeekMedia: {position}");
    }

    public void ResumeMedia()
    {
        AddCommand("ResumeMedia");
    }

    public void StopMedia()
    {
        AddCommand("StopMedia");
    }

    public void RestoreQuestion(int themeIndex, int questionIndex, int price)
    {
        AddCommand($"RestoreQuestion: Theme={themeIndex}, Question={questionIndex}, Price={price}");
    }

    public void SetCaption(string caption)
    {
        AddCommand($"SetCaption: {caption}");
    }

    public void SetTimerMaxTime(int maxTime)
    {
        AddCommand($"SetTimerMaxTime: {maxTime}");
    }

    public void RunTimer()
    {
        AddCommand("RunTimer");
    }

    public void RunPlayerTimer(int playerIndex, int maxTime)
    {
        AddCommand($"RunPlayerTimer: Player={playerIndex}, MaxTime={maxTime}");
    }

    public void PauseTimer(int currentTime)
    {
        AddCommand($"PauseTimer: {currentTime}");
    }

    public void StopTimer()
    {
        AddCommand("StopTimer");
    }

    public void StopThinkingTimer()
    {
        AddCommand("StopThinkingTimer");
    }

    public void SetAnswerOptions(ItemViewModel[] answerOptions)
    {
        AddCommand($"SetAnswerOptions: {answerOptions.Length} options");
    }

    public void ShowAnswerOptions()
    {
        AddCommand("ShowAnswerOptions");
    }

    public void SetAnswerState(int answerIndex, ItemState state)
    {
        AddCommand($"SetAnswerState: Answer={answerIndex}, State={state}");
    }

    public void OnThemeComments(string comments)
    {
        AddCommand($"OnThemeComments: {comments}");
    }

    public void OnQuestionStart()
    {
        AddCommand("OnQuestionStart");
    }

    public void BeginPressButton()
    {
        AddCommand("BeginPressButton");
    }

    public bool OnQuestionContent(IReadOnlyCollection<ContentItem> content, Func<ContentItem, string?> tryGetMediaUri, string? textToShow)
    {
        AddCommand($"OnQuestionContent: {content.Count} items, Text={textToShow}");
        return true;
    }

    public void FinishQuestion()
    {
        AddCommand("FinishQuestion");
    }

    public void SetQuestionType(string typeName, string aliasName, int activeThemeIndex)
    {
        AddCommand($"SetQuestionType: Type={typeName}, Alias={aliasName}, ThemeIndex={activeThemeIndex}");
    }

    public void SetLanguage(string language)
    {
        AddCommand($"SetLanguage: {language}");
    }

    public void SetReadingSpeed(int readingSpeed)
    {
        AddCommand($"SetReadingSpeed: {readingSpeed}");
    }

    public void SetAttachContentToTable(bool attach)
    {
        AddCommand($"SetAttachContentToTable: {attach}");
    }

    public void SetAppSound(bool isEnabled)
    {
        AddCommand($"SetAppSound: {isEnabled}");
    }

    public void SetSimpleAnswer()
    {
        AddCommand("SetSimpleAnswer");
    }

    public void OnAnswerStart()
    {
        AddCommand("OnAnswerStart");
    }

    public void ClearState()
    {
        AddCommand("ClearState");
    }

    public void OnQuestionEnd()
    {
        AddCommand("OnQuestionEnd");
    }

    public void PlayerIsRight(int playerIndex)
    {
        AddCommand($"PlayerIsRight: {playerIndex}");
    }

    public void PlayerIsWrong(int playerIndex)
    {
        AddCommand($"PlayerIsWrong: {playerIndex}");
    }

    public void NoAnswer()
    {
        AddCommand("NoAnswer");
    }

    public void OnFinalThink()
    {
        AddCommand("OnFinalThink");
    }

    public void SetPause(bool pause, int passedTime)
    {
        AddCommand($"SetPause: Pause={pause}, Time={passedTime}");
    }

    public void ShowQRCode(string? value)
    {
        AddCommand($"ShowQRCode: {value}");
    }

    public void OnPlayerPassed(int playerIndex)
    {
        AddCommand($"OnPlayerPassed: {playerIndex}");
    }

    public void Dispose()
    {
        AddCommand("Dispose");
    }
}
