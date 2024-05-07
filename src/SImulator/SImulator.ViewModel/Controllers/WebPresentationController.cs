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

    public void OnQuestionStart() { }

    public void PauseTimer(int currentTime)
    {
        throw new NotImplementedException();
    }

    public void PlayComplexSelection(int theme, int quest, bool setActive)
    {
        throw new NotImplementedException();
    }

    public void PlaySelection(int theme)
    {
        throw new NotImplementedException();
    }

    public void PlaySimpleSelection(int theme, int quest) => OnMessage(new
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
        throw new NotImplementedException();
    }

    public void RunMedia()
    {
        throw new NotImplementedException();
    }

    public void RunTimer() => OnMessage(new
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

    public void SetCaption(string caption) => OnMessage(new
    {
        Type = "tableCaption",
        Caption = caption
    });

    public void SetGameThemes(IEnumerable<string> themes) => OnMessage(new
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

    public void BeginPressButton() => OnMessage(new
    {
        Type = "beginPressButton"
    });

    public void SetRoundThemes(ThemeInfoViewModel[] themes, bool isFinal)
    {
        OnMessage(new
        {
            Type = "roundThemes",
            Themes = themes.Select(t => t.Name).ToArray(),
            Print = true
        });

        OnMessage(new
        {
            Type = "table",
            Table = themes.Select(t => new { t.Name, Questions = t.Questions.Select(q => q.Price).ToArray() }).ToArray(),
            IsFinal = isFinal
        });
    }

    public void SetSound(string sound = "")
    {
        // TODO
    }

    public void SetStage(TableStage stage)
    {
        // TODO
    }

    public void SetRoundTable() => OnMessage(new
    {
        Type = "showTable"
    });

    public void SetRound(string roundName) => OnMessage(new
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

    public void SetTimerMaxTime(int maxTime) => OnMessage(new
    {
        Type = "timerMaximum",
        TimerIndex = 1,
        Maximum = maxTime
    });

    public void ShowAnswerOptions()
    {
        throw new NotImplementedException();
    }

    public Task StartAsync() => PlatformManager.Instance.CreateMainViewAsync(this, _displayDescriptor);

    public Task StopAsync() => PlatformManager.Instance.CloseMainViewAsync();

    public void StopMedia()
    {
        throw new NotImplementedException();
    }

    public void StopTimer()
    {
        // TODO
    }

    public void UpdatePlayerInfo(int index, PlayerInfo player)
    {
        throw new NotImplementedException();
    }

    public void UpdateSettings(Settings settings)
    {
        // TODO: will be implemented in the future
    }

    public void UpdateShowPlayers(bool showPlayers) => OnMessage(new
    {
        Type = "playersVisibilityChanged",
        IsVisible = showPlayers
    });

    public void SetTheme(string themeName) => OnMessage(new
    {
        Type = "theme",
        ThemeName = themeName
    });

    public void SetQuestion(int questionPrice) => OnMessage(new
    {
        Type = "question",
        QuestionPrice = questionPrice
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
                    OnMessage(new
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

                    OnMessage(new
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
            OnMessage(new
            {
                Type = "content",
                Placement = "screen",
                Content = screenContent.Select(sc => new { Type = sc.Type.ToString().ToLowerInvariant(), sc.Value }).ToArray()
            });
        }

        return hasMedia;
    }

    private void OnMessage(object message) =>
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
