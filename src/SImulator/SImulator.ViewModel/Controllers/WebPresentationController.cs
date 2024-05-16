using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.PlatformSpecific;
using SIPackages;
using SIPackages.Core;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;
using System.Text.Json;
using Utils;
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

    private bool _isAnswer = false;
    private bool _isAnswerSimple = false;

    public Uri Source { get; } = new($"file:///{AppDomain.CurrentDomain.BaseDirectory}webtable/index.html");

    public Action<int, int>? SelectionCallback { get; set; }

    public Action<int>? DeletionCallback { get; set; }

    public event Action<string>? SendJsonMessage;

    public event Action<Exception>? Error;

    public WebPresentationController(IDisplayDescriptor displayDescriptor, IPresentationListener presentationListener)
    {
        _displayDescriptor = displayDescriptor;
        _presentationListener = presentationListener;
    }

    public void AddLostButtonPlayerIndex(int playerIndex)
    {
        throw new NotImplementedException();
    }

    public void AddPlayer()
    {
        throw new NotImplementedException();
    }

    public void ClearPlayers()
    {
        // TODO
    }

    public void ClearPlayersState() { }

    public void Dispose() { }

    public void OnQuestionStart()
    {
        _isAnswerSimple = false;
        _isAnswer = false;
    }

    public void PauseTimer(int currentTime)
    {
        throw new NotImplementedException();
    }

    public void PlayComplexSelection(int theme, int quest, bool setActive) => SendMessage(new
    {
        Type = "questionSelected",
        ThemeIndex = theme,
        QuestionIndex = quest
    });

    public void PlaySelection(int theme)
    {
        SendMessage(new
        {
            Type = "themeDeleted",
            ThemeIndex = theme,
        });

        // TODO: do not send when last theme left
        SendMessage(new
        {
            Type = "choose"
        });
    }

    public void PlaySimpleSelection(int theme, int quest) => SendMessage(new
    {
        Type = "questionSelected",
        ThemeIndex = theme,
        QuestionIndex = quest
    });

    public void RemovePlayer(string playerName)
    {
        throw new NotImplementedException();
    }

    public void RestoreQuestion(int themeIndex, int questionIndex, int price)
    {
        // TODO
    }

    public void RunMedia()
    {
        throw new NotImplementedException();
    }

    public void RunTimer() => SendMessage(new
    {
        Type = "timerResume",
        TimerIndex = 1
    });

    public void SeekMedia(int position)
    {
        throw new NotImplementedException();
    }

    public void SetActivePlayerIndex(int playerIndex)
    {
        // TODO
    }

    public void SetAnswerOptions(ItemViewModel[] answerOptions)
    {
        throw new NotImplementedException();
    }

    public void SetAnswerState(int answerIndex, ItemState state)
    {
        throw new NotImplementedException();
    }

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

    public void SetMedia(MediaSource media, bool background)
    {
        throw new NotImplementedException();
    }

    public void SetQuestionContentType(QuestionContentType questionContentType) { }

    public void SetQuestionSound(bool sound)
    {
        // TODO
    }

    public void SetQuestionStyle(QuestionStyle questionStyle) { }

    public void BeginPressButton() => SendMessage(new
    {
        Type = "beginPressButton"
    });

    public void FinishQuestion() => SendMessage(new
    {
        Type = "endPressButtonByTimeout"
    });

    public void SetRoundThemes(ThemeInfoViewModel[] themes, bool isFinal)
    {
        SendMessage(new
        {
            Type = "roundThemes",
            Themes = themes.Select(t => t.Name).ToArray(),
            PlayMode = isFinal ? "AllTogether" : "OneByOne"
        });

        SendMessage(new
        {
            Type = "table",
            Table = themes.Select(t => new { t.Name, Questions = t.Questions.Select(q => q.Price).ToArray() }).ToArray(),
            IsFinal = isFinal
        });

        if (isFinal)
        {
            SendMessage(new
            {
                Type = "choose"
            });
        }
    }

    public void SetSound(string sound = "")
    {
        // TODO
    }

    public void SetStage(TableStage stage)
    {
        // TODO
    }

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

    public void SetRound(string roundName) => SendMessage(new
    {
        Type = "stage",
        Stage = "Round",
        StageName = roundName,
        StageIndex = -1
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

    public void ShowAnswerOptions()
    {
        throw new NotImplementedException();
    }

    public Task StartAsync() => Task.WhenAll(_loadTSC.Task, PlatformManager.Instance.CreateMainViewAsync(this, _displayDescriptor));

    public Task StopAsync() => PlatformManager.Instance.CloseMainViewAsync();

    public void StopMedia()
    {
        throw new NotImplementedException();
    }

    public void StopTimer()
    {
        SendMessage(new
        {
            Type = "timerStop",
            TimerIndex = 1
        });

        SendMessage(new
        {
            Type = "endPressButtonByTimeout"
        });
    }

    public void UpdatePlayerInfo(int index, PlayerInfo player)
    {
        throw new NotImplementedException();
    }

    public void UpdateSettings(Settings settings)
    {
        // TODO: will be implemented in the future
    }

    public void UpdateShowPlayers(bool showPlayers) => SendMessage(new
    {
        Type = "playersVisibilityChanged",
        IsVisible = showPlayers
    });

    public void SetTheme(string themeName) => SendMessage(new
    {
        Type = "theme",
        ThemeName = themeName
    });

    public void SetQuestion(int questionPrice) => SendMessage(new
    {
        Type = "question",
        QuestionPrice = questionPrice
    });

    public void OnContentStart()
    {
        if (_isAnswer && !_isAnswerSimple)
        {
            SendMessage(new
            {
                Type = "rightAnswerStart",
                Answer = "" // TODO: provide simple right answer here
            });

            _isAnswer = false;
        }
    }

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
                        Type = "replic",
                        PersonCode = "s",
                        Text = contentItem.Value
                    });
                    break;

                case ContentPlacements.Screen:
                    switch (contentItem.Type)
                    {
                        case ContentTypes.Text:
                            if (_isAnswerSimple)
                            {
                                SendMessage(new
                                {
                                    Type = "rightAnswer",
                                    Answer = contentItem.Value
                                });

                                break;
                            }

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

    public void SetQuestionType(string typeName, string aliasName) => SendMessage(new
    {
        Type = "questionType",
        QuestionType = typeName
    });

    public void SetLanguage(string language) => SendMessage(new
    {
        Type = "setLanguage",
        Language = language
    });

    public void SetSimpleAnswer()
    {
        _isAnswerSimple = true;
    }

    public void OnAnswerStart()
    {
        _isAnswer = true;
    }

    public void ClearState()
    {
        SelectionCallback = null;
        DeletionCallback = null;
        
        SendMessage(new
        {
            Type = "stop"
        });
    }

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
                _loadTSC.SetResult();
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

            default:
                break;
        }
    }

    private void SendMessage(object message) =>
        UI.Execute(
            () => SendJsonMessage?.Invoke(JsonSerializer.Serialize(message, SerializerOptions)),
            exc => PlatformManager.Instance.ShowMessage(exc.Message));

    /// <summary>
    /// Defines content info.
    /// </summary>
    /// <param name="Type">Content type.</param>
    /// <param name="Value">Content value.</param>
    private sealed record ContentInfo(ContentType Type, string Value);
}
