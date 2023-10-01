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

    private ContentTypes _contentType = ContentTypes.None;

    /// <summary>
    /// Current question content type.
    /// </summary>
    public ContentTypes ContentType
    {
        get => _contentType;
        set
        {
            if (_contentType != value)
            {
                _contentType = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _content;

    /// <summary>
    /// Current question content.
    /// </summary>
    public string? Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                OnPropertyChanged();
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

        IsAnswer = false;

        _isFinished = !_questionEngine.PlayNext();

        Play.CanBeExecuted = _questionEngine.CanNext;
    }

    public void OnQuestionContent(IReadOnlyCollection<ContentItem> content)
    {
        foreach (var contentItem in content)
        {
            OnQuestionContentItem(contentItem);
        }
    }

    public void OnQuestionContentItem(ContentItem contentItem)
    {
        switch (contentItem.Placement)
        {
            case ContentPlacements.Replic:
                Oral = contentItem.Value;
                break;

            case ContentPlacements.Screen:
                switch (contentItem.Type)
                {
                    case AtomTypes.Text:
                        Content = contentItem.Value;
                        ContentType = ContentTypes.Text;
                        break;

                    case AtomTypes.Image:
                        Content = contentItem.IsRef ? _qDocument.Images.Wrap(contentItem.Value).Uri : contentItem.Value;
                        ContentType = ContentTypes.Image;
                        break;

                    case AtomTypes.Video:
                        Content = contentItem.IsRef ? _qDocument.Video.Wrap(contentItem.Value).Uri : contentItem.Value;
                        ContentType = ContentTypes.Video;
                        break;

                    case AtomTypes.Html:
                        Content = contentItem.Value;
                        ContentType = ContentTypes.Html;
                        break;

                    default:
                        break;
                }
                break;

            case ContentPlacements.Background:
                Sound = contentItem.IsRef ? _qDocument.Audio.Wrap(contentItem.Value).Uri : contentItem.Value;

                if (ContentType == ContentTypes.None)
                {
                    ContentType = ContentTypes.Audio;
                }
                break;

            default:
                break;
        }
    }

    public void OnAskAnswer(string mode)
    {
        IsAnswer = true;
    }

    public void OnButtonPressStart()
    {
        
    }

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

    public bool OnAnswerOptions(AnswerOption[] answerOptions) => false;

    public bool OnRightAnswerOption(string rightOptionLabel) => false;
}
