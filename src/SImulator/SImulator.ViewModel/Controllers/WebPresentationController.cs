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

    private ItemViewModel[]? _answerOptions;

    private int _playerCount;

    public WebPresentationController(IDisplayDescriptor displayDescriptor, IPresentationListener presentationListener)
    {
        _displayDescriptor = displayDescriptor;
        _presentationListener = presentationListener;
    }

    public void AddLostButtonPlayerIndex(int playerIndex)
    {
        // TODO
    }

    public void AddPlayer()
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
            Name = "",
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

    public void OnQuestionStart()
    {
        _isAnswerSimple = false;
        _isAnswer = false;
    }

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

    public void RunMedia()
    {
        // TODO
    }

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
        if (playerIndex > -1)
        {
            SendMessage(new
            {
                Type = "setChooser",
                ChooserIndex = playerIndex,
                SetAnswerer = true
            });
        }
    }

    public void SetAnswerOptions(ItemViewModel[] answerOptions)
    {
        SendMessage(new
        {
            Type = "answerOptionsLayout",
            QuestionHasScreenContent = true,
            TypeNames = answerOptions.Select(o => o.Content.Type.ToString().ToLowerInvariant()).ToArray()
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

    public void SetMedia(MediaSource media, bool background)
    {
        if (background)
        {
            SendMessage(new
            {
                Type = "content",
                Placement = "background",
                Content = new[] { new { Type = "audio", Value = media.Uri } }
            });
        }
        else
        {
            SendMessage(new
            {
                Type = "content",
                Placement = "screen",
                Content = new[] { new { Type = "image", Value = media.Uri } }
            });
        }
    }

    public void SetQuestionContentType(QuestionContentType questionContentType) { }

    public void SetQuestionSound(bool sound) { }

    public void SetQuestionStyle(QuestionStyle questionStyle) { }

    public void BeginPressButton() => SendMessage(new
    {
        Type = "beginPressButton"
    });

    public void FinishQuestion() { }

    public void NoAnswer() => SendMessage(new
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

    public void SetSound(string sound = "") { }

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

    public async Task StartAsync()
    {
        await Task.WhenAll(_loadTSC.Task, PlatformManager.Instance.CreateMainViewAsync(this, _displayDescriptor));

        SendMessage(new
        {
            Type = "setSoundMap",
            SoundMap = new Dictionary<string, string>
            {
                ["final_delete"] = "../sounds/final_delete.mp3",
                ["final_think"] = "../sounds/final_think.mp3",
                ["round_begin"] = "../sounds/round_begin.mp3",
                ["round_themes"] = "../sounds/round_themes.mp3",
                ["round_timeout"] = "../sounds/round_timeout.mp3",
                ["question_noanswers"] = "../sounds/question_noanswers.mp3",
                ["question_norisk"] = "../sounds/question_norisk.mp3",
                ["question_secret"] = "../sounds/question_secret.mp3",
                ["question_stake"] = "../sounds/question_stake.mp3",
                ["round_begin"] = "../sounds/round_begin.mp3"
            }
        });
    }

    public Task StopAsync() => PlatformManager.Instance.CloseMainViewAsync();

    public void StopMedia()
    {
        // TODO
    }

    public void StopTimer()
    {
        SendMessage(new
        {
            Type = "timerStop",
            TimerIndex = 1
        });
    }

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

    public void OnFinalThink() => SendMessage(new
    {
        Type = "finalThink"
    });

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

    public void SetSimpleAnswer() => _isAnswerSimple = true;

    public void OnAnswerStart() => _isAnswer = true;

    public void ClearState()
    {
        SelectionCallback = null;
        DeletionCallback = null;
        
        SendMessage(new
        {
            Type = "stop"
        });
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
