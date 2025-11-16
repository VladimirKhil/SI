using SIEngine.Rules;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.PlatformSpecific;
using SIPackages;
using SIPackages.Core;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Input;
using Utils;
using Utils.Commands;
using Utils.Web;

namespace SImulator.ViewModel.Controllers;

public sealed class WebPresentationController : IPresentationController, IWebInterop
{
    private readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDisplayDescriptor _displayDescriptor;
    private readonly IPresentationListener _presentationListener;
    private readonly TaskCompletionSource _loadTSC = new();

    private Action? _onLoad;

    public Uri Source { get; } = new($"file:///{AppDomain.CurrentDomain.BaseDirectory}webtable/index.html");

    public Action<int, int>? SelectionCallback { get; set; }

    public Action<int>? DeletionCallback { get; set; }

    public event Action<string>? SendJsonMessage;

    public event Action<Exception>? Error;

    private ItemViewModel[]? _answerOptions;

    private int _playerCount;
    private readonly SoundsSettings _soundsSettings;

    public bool CanControlMedia => false;
    
    public ICommand Stop { get; private set; }

    public WebPresentationController(IDisplayDescriptor displayDescriptor, IPresentationListener presentationListener, SoundsSettings soundsSettings)
    {
        _displayDescriptor = displayDescriptor;
        _presentationListener = presentationListener;
        _soundsSettings = soundsSettings;

        Stop = new SimpleCommand(arg =>
        {
            if (presentationListener != null)
            {
                UI.Execute(presentationListener.AskStop, OnError);
            }
        });
    }

    public void AddPlayer(string playerName)
    {
        SendMessage(new
        {
            Type = "addPlayerTable"
        });

        SendMessage(new
        {
            Type = "tableChangeType",
            Role = "player",
            Index = _playerCount,
            IsHuman = false,
            Name = playerName,
            Sex = 0
        });

        _playerCount++;
    }

    public void ClearPlayers()
    {
        while (_playerCount > 0)
        {
            RemovePlayer(0);
        }
    }

    public void ClearPlayersState() { }

    public void OnThemeComments(string comments) => SendMessage(new
    {
        Type = "themeComments",
        ThemeComments = comments
    });

    public void RunPlayerTimer(int playerIndex, int maxTime) => SendMessage(new
    {
        Type = "timerRun",
        TimerIndex = 2,
        TimerArgument = maxTime,
        TimerPersonIndex = playerIndex
    });

    public void PauseTimer(int currentTime) => SendMessage(new
    {
        Type = "timerPause",
        TimerIndex = 1,
        CurrentTime = currentTime
    });

    public void PlaySelection(int themeIndex)
    {
        SendMessage(new
        {
            Type = "themeDeleted",
            ThemeIndex = themeIndex,
        });

        // TODO: do not send when last theme left
        SendMessage(new
        {
            Type = "choose"
        });
    }

    public void PlaySimpleSelection(int themeIndex, int questionIndex) => SendMessage(new
    {
        Type = "questionSelected",
        ThemeIndex = themeIndex,
        QuestionIndex = questionIndex
    });

    public void RemovePlayer(int playerIndex)
    {
        SendMessage(new
        {
            Type = "deletePlayerTable",
            Index = playerIndex
        });

        _playerCount--;
    }

    public void RestoreQuestion(int themeIndex, int questionIndex, int price) => SendMessage(new
    {
        Type = "toggle",
        ThemeIndex = themeIndex,
        QuestionIndex = questionIndex,
        Price = price
    });

    public void ResumeMedia() => SendMessage(new { Type = "resumeMedia" });

    public void RunTimer() => SendMessage(new
    {
        Type = "timerResume",
        TimerIndex = 1
    });

    public void SeekMedia(int position)
    {
        // TODO
    }

    public void SetActivePlayerIndex(int playerIndex)
    {
        if (playerIndex <= -1)
        {
            return;
        }

        SendMessage(new
        {
            Type = "endPressButtonByPlayer",
            PlayerIndex = playerIndex
        });
    }

    public void SetAnswerOptions(ItemViewModel[] answerOptions)
    {
        SendMessage(new
        {
            Type = "answerOptionsLayout",
            QuestionHasScreenContent = true,
            TypeNames = answerOptions.Select(o => o.Content.Type.ToString().ToLowerInvariant()).ToArray()
        });

        SendMessage(new
        {
            Type = "askAnswer"
        });

        _answerOptions = answerOptions;
    }

    public void SetAnswerState(int answerIndex, ItemState state) => SendMessage(new
    {
        Type = "contentState",
        Placement = "screen",
        LayoutId = answerIndex + 1,
        ItemState = (int)state
    });

    public void SetCaption(string caption) => SendMessage(new
    {
        Type = "tableCaption",
        Caption = caption
    });

    public void SetGameThemes(IEnumerable<string> themes) => SendMessage(new
    {
        Type = "gameThemes",
        Themes = themes.ToArray()
    });

    public void SetQuestionContentType(QuestionContentType questionContentType) { }

    public void BeginPressButton() => SendMessage(new { Type = "beginPressButton" });

    public void FinishQuestion() { }

    public void NoAnswer() => SendMessage(new
    {
        Type = "endPressButtonByTimeout"
    });

    public void SetRoundThemes(string[] themes, bool isFinal)
    {
        SendMessage(new
        {
            Type = "roundThemes",
            Themes = themes,
            PlayMode = isFinal ? "AllTogether" : "OneByOne"
        });

        if (isFinal)
        {
            SendMessage(new
            {
                Type = "choose"
            });
        }
    }

    public void SetTable(ThemeInfoViewModel[] table) => SendMessage(new
    {
        Type = "table",
        Table = table.Select(t => new { t.Name, Questions = t.Questions.Select(q => q.Price).ToArray() }).ToArray()
    });

    public void SetStage(TableStage stage) { }

    public void SetRoundTable()
    {
        SendMessage(new
        {
            Type = "showTable"
        });

        SendMessage(new
        {
            Type = "choose"
        });
    }

    public void OnPackage(string packageName, MediaInfo? packageLogo) => SendMessage(new
    {
        Type = "package",
        PackageName = packageName,
        PackageLogo = packageLogo?.Uri?.OriginalString
    });

    public void SetRound(string roundName, QuestionSelectionStrategyType selectionStrategyType) => SendMessage(new
    {
        Type = "stage",
        Stage = "Round",
        StageName = roundName,
        StageIndex = -1,
        Rules = selectionStrategyType.ToString()
    });

    public void SetText(string text = "")
    {
        // TODO
    }

    public void SetTimerMaxTime(int maxTime) => SendMessage(new
    {
        Type = "timerMaximum",
        TimerIndex = 1,
        Maximum = maxTime
    });

    public async void ShowAnswerOptions()
    {
        if (_answerOptions == null)
        {
            return;
        }

        try
        {
            for (var i = 0; i < _answerOptions.Length; i++)
            {
                SendMessage(new
                {
                    Type = "answerOption",
                    Index = i,
                    _answerOptions[i].Label,
                    ContentType = _answerOptions[i].Content.Type.ToString().ToLowerInvariant(),
                    ContentValue = _answerOptions[i].Content.Value,
                });

                await Task.Delay(1000);
            }
        }
        catch (Exception exc)
        {
            Trace.TraceError("ShowAnswerOptions error: " + exc.Message);
        }
    }

    public Task StartAsync(Action onLoad)
    {
        _onLoad = onLoad;
        return Task.WhenAll(_loadTSC.Task, PlatformManager.Instance.CreateMainViewAsync(this, _displayDescriptor));
    }

    private void InitInternal() => SendMessage(new
    {
        Type = "setSoundMap",
        SoundMap = new Dictionary<string, string>
        {
            ["game_themes"] = GetSoundUri(_soundsSettings.GameThemes),
            ["final_delete"] = GetSoundUri(_soundsSettings.FinalDelete),
            ["final_think"] = GetSoundUri(_soundsSettings.FinalThink),
            ["round_begin"] = GetSoundUri(_soundsSettings.RoundBegin),
            ["round_themes"] = GetSoundUri(_soundsSettings.RoundThemes),
            ["round_timeout"] = GetSoundUri(_soundsSettings.RoundTimeout),
            ["applause_small"] = GetSoundUri(_soundsSettings.AnswerRight),
            ["applause_big"] = GetSoundUri(_soundsSettings.AnswerRight),
            ["question_for_yourself"] = GetSoundUri(_soundsSettings.NoRiskQuestion),
            ["question_secret"] = GetSoundUri(_soundsSettings.SecretQuestion),
            ["question_stake"] = GetSoundUri(_soundsSettings.StakeQuestion),
            ["question_for_all_with_stake"] = GetSoundUri(_soundsSettings.StakeForAllQuestion),
            ["question_for_all"] = GetSoundUri(_soundsSettings.ForAllQuestion),
            ["answer_wrong"] = GetSoundUri(_soundsSettings.AnswerWrong),
            ["question_noanswers"] = GetSoundUri(_soundsSettings.NoAnswer),
            ["question_selected"] = GetSoundUri(_soundsSettings.QuestionSelected),
            ["button_pressed"] = GetSoundUri(_soundsSettings.PlayerPressed),
        }
    });

    public void SetAppSound(bool isEnabled) => SendMessage(new { Type = "setAppSound", IsEnabled = isEnabled });

    private static string GetSoundUri(string sound)
    {
        if (!Uri.TryCreate(sound, UriKind.RelativeOrAbsolute, out var uri))
        {
            return sound;
        }

        if (uri.IsAbsoluteUri)
        {
            return sound;
        }

        return $"../sounds/{sound}";
    }

    public Task StopAsync() => PlatformManager.Instance.CloseMainViewAsync();

    public void StopMedia()
    {
        // TODO
    }

    public void StopTimer() => SendMessage(new
    {
        Type = "timerStop",
        TimerIndex = 1
    });

    public void StopThinkingTimer() => SendMessage(new
    {
        Type = "timerStop",
        TimerIndex = 2
    });

    public void UpdatePlayerInfo(int playerIndex, PlayerInfo player, string? propertyName = null)
    {
        if (propertyName == null || propertyName == nameof(PlayerInfo.Name))
        {
            SendMessage(new
            {
                Type = "tableSet",
                Role = "player",
                Index = playerIndex,
                Replacer = player.Name,
                ReplacerSex = 0
            });
        }

        if (propertyName == nameof(PlayerInfo.Sum))
        {
            SendMessage(new
            {
                Type = "sum",
                PlayerIndex = playerIndex,
                Value = player.Sum
            });
        }
    }

    public void OnFinalThink() => SendMessage(new { Type = "finalThink" });
    
    public void UpdateSettings(Settings settings) => SendMessage(new
    {
        Type = "setOptions",
        TableTextColor = ConvertWpfToHtmlColor(settings.TableColorString),
        TableBackgroundColor = ConvertWpfToHtmlColor(settings.TableBackColorString)
    });

    private static string ConvertWpfToHtmlColor(string wpfColor)
    {
        if (wpfColor.Length == 9 && wpfColor.StartsWith("#"))
        {
            return $"#{wpfColor.Substring(3)}{wpfColor.Substring(1, 2)}";
        }

        return wpfColor; // Return as-is if not in expected format
    }

    public void UpdateShowPlayers(bool showPlayers) => SendMessage(new
    {
        Type = "playersVisibilityChanged",
        IsVisible = showPlayers
    });

    public void SetTheme(string themeName, bool animate) => SendMessage(new
    {
        Type = "theme",
        ThemeName = themeName,
        Animate = animate
    });

    public void SetQuestionPrice(int questionPrice) => SendMessage(new
    {
        Type = "question",
        QuestionPrice = questionPrice
    });

    public void SetQuestionComments(string comments) => SendMessage(new
    {
        Type = "questionComments",
        QuestionComments = comments
    });

    public void OnComplexRightAnswer(string answer) => SendMessage(new
    {
        Type = "rightAnswerStart",
        Answer = answer
    });

    public bool OnQuestionContent(IReadOnlyCollection<ContentItem> content, Func<ContentItem, string?> tryGetMediaUri, string? textToShow)
    {
        var hasMedia = false;
        var screenContent = new List<ContentInfo>();

        foreach (var contentItem in content)
        {
            switch (contentItem.Placement)
            {
                case ContentPlacements.Replic:
                    SendMessage(new
                    {
                        Type = "content",
                        Placement = "replic",
                        Content = new object[] { new { Type = "text", contentItem.Value } }
                    });
                    break;

                case ContentPlacements.Screen:
                    switch (contentItem.Type)
                    {
                        case ContentTypes.Text:
                            screenContent.Add(new ContentInfo(ContentType.Text, contentItem.Value));
                            break;

                        case ContentTypes.Image:
                            screenContent.Add(new ContentInfo(ContentType.Image, tryGetMediaUri(contentItem) ?? ""));
                            break;

                        case ContentTypes.Video:
                            screenContent.Add(new ContentInfo(ContentType.Video, tryGetMediaUri(contentItem) ?? ""));
                            hasMedia = true;
                            break;

                        case ContentTypes.Html:
                            screenContent.Add(new ContentInfo(ContentType.Html, tryGetMediaUri(contentItem) ?? ""));
                            break;

                        default:
                            break;
                    }
                    break;

                case ContentPlacements.Background:
                    var sound = tryGetMediaUri(contentItem);

                    SendMessage(new
                    {
                        Type = "content",
                        Placement = "background",
                        Content = new object[] { new { Type = "audio", Value = sound } }
                    });

                    hasMedia = true;
                    break;

                default:
                    break;
            }
        }

        if (screenContent.Count > 0)
        {
            SendMessage(new
            {
                Type = "content",
                Placement = "screen",
                Content = screenContent.Select(sc => new { Type = sc.Type.ToString().ToLowerInvariant(), sc.Value }).ToArray()
            });
        }

        return hasMedia;
    }

    public void OnSimpleRightAnswer(string answer) => SendMessage(new
    {
        Type = "rightAnswer",
        Answer = answer
    });

    public void SetQuestionType(string typeName, string aliasName, int activeThemeIndex) => SendMessage(new
    {
        Type = "questionType",
        QuestionType = typeName
    });

    public void SetLanguage(string language) => SendMessage(new
    {
        Type = "setLanguage",
        Language = language
    });

    public void SetReadingSpeed(int readingSpeed) => SendMessage(new
    {
        Type = "setReadingSpeed",
        ReadingSpeed = readingSpeed
    });

    public void SetAttachContentToTable(bool attach) => SendMessage(new
    {
        Type = "setAttachContentToTable",
        Attach = attach
    });

    public void SetPause(bool pause, int passedTime) => SendMessage(new
    {
        Type = "pause",
        IsPaused = pause,
        CurrentTime = new int[] { 0, passedTime, 0 }
    });

    public void ClearState()
    {
        SelectionCallback = null;
        DeletionCallback = null;
        
        SendMessage(new { Type = "stop" });
    }

    public void OnQuestionEnd() => SendMessage(new
    {
        Type = "questionEnd"
    });

    public void PlayerIsRight(int playerIndex) => SendMessage(new
    {
        Type = "person",
        PlayerIndex = playerIndex,
        IsRight = true
    });

    public void PlayerIsWrong(int playerIndex) => SendMessage(new
    {
        Type = "person",
        PlayerIndex = playerIndex,
        IsRight = false
    });

    public void AddLostButtonPlayerIndex(int playerIndex) => SendMessage(new
    {
        Type = "wrongTry",
        PlayerIndex = playerIndex
    });

    public void ShowQRCode(string? value) => SendMessage(new
    {
        Type = "qrCode",
        QrCode = value
    });

    public void OnPlayerPassed(int playerIndex) => SendMessage(new { Type = "pass", PlayerIndex = playerIndex });

    public void OnMessage(string message)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(message);

        if (data == null)
        {
            return;
        }

        var type = data["type"].GetString();
        
        switch (type)
        {
            case "loaded":
                if (!_loadTSC.Task.IsCompleted)
                {
                    _loadTSC.SetResult();
                }

                _onLoad?.Invoke();
                InitInternal();
                break;

            case "loadError":
                _loadTSC.SetException(new Exception(data["error"].GetString()));
                break;

            case "move":
                _presentationListener.AskNext();
                break;

            case "selectQuestion":
                SelectionCallback?.Invoke(data["themeIndex"].GetInt32(), data["questionIndex"].GetInt32());
                break;

            case "deleteTheme":
                DeletionCallback?.Invoke(data["themeIndex"].GetInt32());
                break;

            case "sendAnswer":
                var answer = data["answer"].GetString();
                int answerIndex;

                if (_answerOptions != null)
                {
                    for (answerIndex = 0; answerIndex < _answerOptions.Length; answerIndex++)
                    {
                        if (_answerOptions[answerIndex].Label == answer)
                        {
                            _presentationListener.OnAnswerSelected(answerIndex);
                            break;
                        }
                    }
                }

                SendMessage(new
                {
                    Type = "askAnswer"
                });

                break;

            default:
                break;
        }
    }

    public void Dispose() { }

    private void SendMessage(object message) =>
        UI.Execute(
            () => SendJsonMessage?.Invoke(JsonSerializer.Serialize(message, SerializerOptions)),
            OnError);

    private void OnError(Exception exc) => Error?.Invoke(exc);

    /// <summary>
    /// Defines content info.
    /// </summary>
    /// <param name="Type">Content type.</param>
    /// <param name="Value">Content value.</param>
    private sealed record ContentInfo(ContentType Type, string Value);
}
