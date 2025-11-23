using SIEngine.Core;
using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Properties;
using SIQuester.ViewModel.Workspaces.Dialogs.Play;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Utils.Commands;
using Utils.Web;

namespace SIQuester.ViewModel.Workspaces.Dialogs;

/// <summary>
/// Defines a view model for question player.
/// </summary>
public sealed class QuestionPlayViewModel : WorkspaceViewModel, IQuestionEnginePlayHandler, IWebInterop
{
    private readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private QuestionEngine _questionEngine;
    private readonly QDocument _qDocument;
    private readonly QuestionViewModel _originalQuestion;

    private bool _singleAnswerer = true;
    private bool _isFinished;
    private AnswerOptionViewModel[]? _options;

    public override string Header => Resources.QuestionPlay;

    public Uri Source { get; } = new($"file:///{AppDomain.CurrentDomain.BaseDirectory}wwwroot/index.html");

    public event Action<string>? SendJsonMessage;

    /// <summary>
    /// Plays the next question fragment.
    /// </summary>
    public SimpleCommand Play { get; private set; }

    /// <summary>
    /// Replays the question from the beginning.
    /// </summary>
    public SimpleCommand Replay { get; private set; }

    /// <summary>
    /// Closes the question player dialog.
    /// </summary>
    public SimpleCommand CloseDialog { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the Replay button should be visible.
    /// </summary>
    public bool IsReplayVisible => _isFinished;

    /// <summary>
    /// Initializes a new instance of <see cref="QuestionPlayViewModel" /> class.
    /// </summary>
    /// <param name="question">Question to play.</param>
    /// <param name="document">Document that holds the question media content.</param>
    public QuestionPlayViewModel(QuestionViewModel question, QDocument document)
    {
        _originalQuestion = question;
        _qDocument = document;

        Play = new SimpleCommand(Play_Executed);
        Replay = new SimpleCommand(Replay_Executed);
        CloseDialog = new SimpleCommand(CloseDialog_Executed);

        InitializeQuestionEngine();

        if (!File.Exists(Source.AbsolutePath))
        {
            PlatformSpecific.PlatformManager.Instance.ShowErrorMessage($"File not found: {Source.AbsolutePath}");
        }
    }

    [MemberNotNull(nameof(_questionEngine))]
    private void InitializeQuestionEngine()
    {
        _questionEngine = new QuestionEngine(
            _originalQuestion.Model,
            new QuestionEngineOptions
            {
                FalseStarts = FalseStartMode.Enabled,
                ShowSimpleRightAnswers = true,
                DefaultTypeName = _originalQuestion.OwnerTheme?.OwnerRound?.Model.Type == RoundTypes.Final ? QuestionTypes.StakeAll : QuestionTypes.Simple
            },
            this);

        _isFinished = false;
        _options = null;
        Play.CanBeExecuted = true;
        
        // Notify visibility changes
        OnPropertyChanged(nameof(IsReplayVisible));
    }

    /// <summary>
    /// Plays the next question fragment.
    /// </summary>
    public void Play_Executed(object? arg)
    {
        try
        {
            if (_isFinished)
            {
                return;
            }

            if (!_questionEngine.CanNext)
            {
                _isFinished = true;
                Play.CanBeExecuted = false;
                
                // Notify visibility changes when question finishes
                OnPropertyChanged(nameof(IsReplayVisible));
                return;
            }

            OnMessage(new
            {
                Type = "endPressButtonByTimeout"
            });

            _isFinished = !_questionEngine.PlayNext();
            
            // Check if question finished after playing
            if (_isFinished)
            {
                Play.CanBeExecuted = false;
                OnPropertyChanged(nameof(IsReplayVisible));
            }
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private void Replay_Executed(object? arg)
    {
        try
        {
            // Reset the question engine to start from the beginning
            InitializeQuestionEngine();

            // Reset UI state
            _singleAnswerer = true;

            Play.Execute(null);
        }
        catch (Exception exc)
        {
            OnError(exc);
        }
    }

    private void CloseDialog_Executed(object? arg)
    {
        // Close the dialog by calling the inherited Close command
        Close.ExecuteAsync(arg);
    }

    public void OnQuestionContent(IReadOnlyCollection<ContentItem> content)
    {
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
                            screenContent.Add(new ContentInfo(ContentType.Image, contentItem.IsRef ? _qDocument.Images.Wrap(contentItem.Value).Uri : contentItem.Value));
                            break;

                        case ContentTypes.Video:
                            screenContent.Add(new ContentInfo(ContentType.Video, contentItem.IsRef ? _qDocument.Video.Wrap(contentItem.Value).Uri : contentItem.Value));
                            break;

                        case ContentTypes.Html:
                            screenContent.Add(new ContentInfo(ContentType.Html, contentItem.IsRef ? _qDocument.Html.Wrap(contentItem.Value).Uri : contentItem.Value));
                            break;

                        default:
                            break;
                    }
                    break;

                case ContentPlacements.Background:
                    var sound = contentItem.IsRef ? _qDocument.Audio.Wrap(contentItem.Value).Uri : contentItem.Value;

                    OnMessage(new
                    {
                        Type = "content",
                        Placement = "background",
                        Content = new object[] { new { Type = "audio", Value = sound } }
                    });
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
    }

    public void OnAskAnswer(string mode)
    {
        if (mode == StepParameterValues.AskAnswerMode_Button)
        {
            OnMessage(new
            {
                Type = "beginPressButton"
            });
        }
        else
        {
            OnMessage(new
            {
                Type = "replic",
                PersonCode = "s",
                Text = _singleAnswerer ? Resources.YourAnswer : Resources.ThinkAll
            });
        }
    }

    public bool OnButtonPressStart() => false;

    public bool OnSetAnswerer(string mode, string? select, string? stakeVisibility)
    {
        var multipleAnswerers = mode == StepParameterValues.SetAnswererMode_All 
            || mode == StepParameterValues.SetAnswererMode_Stake && select == StepParameterValues.SetAnswererSelect_AllPossible;
        
        _singleAnswerer = !multipleAnswerers;
        return false;
    }

    public bool OnSetPrice(string mode, NumberSet? availableRange) => false;

    public bool OnSetTheme(string themeName) => false;

    public bool OnAccept() => false;

    public void OnQuestionStart(bool buttonsRequired, ICollection<string> rightAnswers, Action skipQuestionCallback) { }

    public void OnContentStart(IReadOnlyList<ContentItem> contentItems, Action<int> moveToContentCallback) { }

    public void OnSimpleRightAnswerStart() { }

    public void OnAnswerStart() => OnMessage(new { Type = "replic", PersonCode = "s", Text = "" });

    public bool OnAnnouncePrice(NumberSet availableRange) => false;

    public bool OnAnswerOptions(AnswerOption[] answerOptions, IReadOnlyList<ContentItem[]> screenContentSequence)
    {
        var options = new List<AnswerOptionViewModel>();

        foreach (var option in answerOptions)
        {
            switch (option.Content.Type)
            {
                case ContentTypes.Text:
                    options.Add(new AnswerOptionViewModel(option.Label, new ContentInfo(ContentType.Text, option.Content.Value)));
                    break;

                case ContentTypes.Image:
                    options.Add(new AnswerOptionViewModel(
                        option.Label,
                        new ContentInfo(
                            ContentType.Image,
                            option.Content.IsRef ? _qDocument.Images.Wrap(option.Content.Value).Uri : option.Content.Value)));
                    break;

                default:
                    break;
            }
        }

        OnMessage(new
        {
            Type = "answerOptionsLayout",
            QuestionHasScreenContent = true,
            TypeNames = options.Select(o => o.Content.Type.ToString().ToLowerInvariant()).ToArray()
        });

        for (int i = 0; i < options.Count; i++)
        {
            OnMessage(new
            {
                Type = "answerOption",
                Index = i,
                options[i].Label,
                ContentType = options[i].Content.Type.ToString().ToLowerInvariant(),
                ContentValue = options[i].Content.Value,
            });
        }

        _options = options.ToArray();

        return false;
    }

    public bool OnRightAnswerOption(string rightOptionLabel)
    {
        if (_options == null)
        {
            return false;
        }

        for (var i = 0; i < _options.Length; i++)
        {
            if (_options[i].Label == rightOptionLabel)
            {
                OnMessage(new
                {
                    Type = "contentState",
                    Placement = "screen",
                    LayoutId = i + 1,
                    ItemState = 2 // right
                });

                break;
            }
        }

        return true;
    }

    private void OnMessage(object message) => SendJsonMessage?.Invoke(JsonSerializer.Serialize(message, SerializerOptions));

    public bool OnNumericAnswerType(int deviation) => false;
}
