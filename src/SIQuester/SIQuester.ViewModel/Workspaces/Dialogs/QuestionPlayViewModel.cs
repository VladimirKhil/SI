using SIEngine.Core;
using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Workspaces.Dialogs.Play;
using Utils.Commands;

namespace SIQuester.ViewModel.Workspaces.Dialogs;

/// <summary>
/// Defines a view model for question player.
/// </summary>
public sealed class QuestionPlayViewModel : WorkspaceViewModel, IQuestionEnginePlayHandler
{
    private readonly QuestionEngine _questionEngine;
    private readonly QDocument _qDocument;

    private bool _isFinished;

    public override string Header => Properties.Resources.QuestionPlay;

    private ContentInfo[] _content = Array.Empty<ContentInfo>();

    /// <summary>
    /// Current question content.
    /// </summary>
    public ContentInfo[] Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;

                try
                {
                    OnPropertyChanged();
                }
                catch (NotImplementedException exc) when (exc.Message.Contains("The Source property cannot be set to null"))
                {
                    // https://github.com/MicrosoftEdge/WebView2Feedback/issues/1136
                }
            }
        }
    }

    private string? _sound;

    /// <summary>
    /// Current background sound.
    /// </summary>
    public string? Sound
    {
        get => _sound;
        set
        {
            if (_sound != value)
            {
                _sound = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _oral;

    /// <summary>
    /// Current oral text.
    /// </summary>
    public string? Oral
    {
        get => _oral;
        set
        {
            if (_oral != value)
            {
                _oral = value;
                OnPropertyChanged();
            }
        }
    }

    private AnswerOptionViewModel[] _answerOptions = Array.Empty<AnswerOptionViewModel>();

    /// <summary>
    /// Answer options.
    /// </summary>
    public AnswerOptionViewModel[] AnswerOptions
    {
        get => _answerOptions;
        set 
        {
            if (_answerOptions != value)
            {
                _answerOptions = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isAnswer;

    /// <summary>
    /// Is the playing in the answer stage.
    /// </summary>
    public bool IsAnswer
    {
        get => _isAnswer;
        set
        {
            if (_isAnswer != value)
            {
                _isAnswer = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Plays the next question fragment.
    /// </summary>
    public SimpleCommand Play { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="QuestionPlayViewModel" /> class.
    /// </summary>
    /// <param name="question">Question to play.</param>
    /// <param name="document">Document that holds the question media content.</param>
    public QuestionPlayViewModel(QuestionViewModel question, QDocument document)
    {
        var questionClone = question.Model.Clone();
        questionClone.Upgrade();

        _questionEngine = new QuestionEngine(
            questionClone,
            new QuestionEngineOptions
            {
                FalseStarts = FalseStartMode.Enabled,
                ShowSimpleRightAnswers = true,
                DefaultTypeName = QuestionTypes.Simple
            },
            this);

        _qDocument = document;

        Play = new SimpleCommand(Play_Executed);
    }

    public void Play_Executed(object? arg)
    {
        if (_isFinished)
        {
            return;
        }

        if (!_questionEngine.CanNext)
        {
            _isFinished = true;
            Play.CanBeExecuted = false;
            Sound = null;
            Content = Array.Empty<ContentInfo>();
            AnswerOptions = Array.Empty<AnswerOptionViewModel>();
            return;
        }

        IsAnswer = false;
        _isFinished = !_questionEngine.PlayNext();
    }

    public void OnQuestionContent(IReadOnlyCollection<ContentItem> content)
    {
        var screenContent = new List<ContentInfo>();

        foreach (var contentItem in content)
        {
            switch (contentItem.Placement)
            {
                case ContentPlacements.Replic:
                    Oral = contentItem.Value;
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
                    Sound = contentItem.IsRef ? _qDocument.Audio.Wrap(contentItem.Value).Uri : contentItem.Value;
                    break;

                default:
                    break;
            }
        }

        if (screenContent.Count > 0)
        {
            Content = screenContent.ToArray();
        }
    }

    public void OnAskAnswer(string mode)
    {
        IsAnswer = true;
        Sound = null;
    }

    public bool OnButtonPressStart() => false;

    public bool OnSetAnswerer(string mode, string? select, string? stakeVisibility) => false;

    public bool OnSetPrice(string mode, NumberSet? availableRange) => false;

    public bool OnSetTheme(string themeName) => false;

    public bool OnAccept() => false;

    public void OnQuestionStart(bool buttonsRequired)
    {
        
    }

    public void OnContentStart(IEnumerable<ContentItem> contentItems)
    {
        
    }

    public void OnSimpleRightAnswerStart()
    {
        
    }

    public void OnAskAnswerStop()
    {
        
    }

    public bool OnAnnouncePrice(NumberSet? availableRange) => false;

    public bool OnAnswerOptions(AnswerOption[] answerOptions)
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
                    options.Add(new AnswerOptionViewModel(option.Label, new ContentInfo(ContentType.Image, option.Content.IsRef ? _qDocument.Images.Wrap(option.Content.Value).Uri : option.Content.Value)));
                    break;

                default:
                    break;
            }
        }

        AnswerOptions = options.ToArray();
        return false;
    }

    public bool OnRightAnswerOption(string rightOptionLabel)
    {
        foreach (var option in AnswerOptions)
        {
            if (option.Label == rightOptionLabel)
            {
                option.IsSelected = true;
                break;
            }
        }

        return true;
    }
}
