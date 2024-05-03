using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.PlatformSpecific;
using SIPackages;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;
using System;
using System.Text.Json;
using Utils;
using Utils.Web;

namespace SImulator.ViewModel.Controllers;

internal sealed class WebPresentationController : IPresentationController, IWebInterop
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

    public void ClearPlayersState()
    {
        // TODO
    }

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

    public void RunTimer()
    {
        throw new NotImplementedException();
    }

    public void SeekMedia(int position)
    {
        throw new NotImplementedException();
    }

    public void SetActivePlayerIndex(int playerIndex)
    {
        throw new NotImplementedException();
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

    public void SetScreenContent(IReadOnlyCollection<ContentGroup> content)
    {
        // TODO: next
    }

    public void SetSound(string sound = "")
    {
        // TODO
    }

    public void SetStage(TableStage stage)
    {
        if (stage == TableStage.RoundTable)
        {
            OnMessage(new
            {
                Type = "showTable"
            });
        }
    }

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

    public void SetTimerMaxTime(int maxTime)
    {
        throw new NotImplementedException();
    }

    public void ShowAnswerOptions()
    {
        throw new NotImplementedException();
    }

    public async void Start()
    {
        await PlatformManager.Instance.CreateMainViewAsync(this, _displayDescriptor);
    }

    public async void StopGame() => await PlatformManager.Instance.CloseMainViewAsync();

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
        // TODO
    }

    public void UpdateShowPlayers(bool showPlayers)
    {
        // TODO
    }

    private void OnMessage(object message) =>
        UI.Execute(
            () => SendJsonMessage?.Invoke(JsonSerializer.Serialize(message, SerializerOptions)),
            exc => PlatformManager.Instance.ShowMessage(exc.Message));
}
