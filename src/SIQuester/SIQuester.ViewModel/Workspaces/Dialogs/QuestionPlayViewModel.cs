using SIEngine.Core;
using SIPackages;
using SIPackages.Core;
using SIQuester.ViewModel.Workspaces.Dialogs.Play;

namespace SIQuester.ViewModel.Workspaces.Dialogs;

/// <summary>
/// Defines a view model for question player.
/// </summary>
public sealed class QuestionPlayViewModel : WorkspaceViewModel, IQuestionPlayHandler
{
    private readonly QuestionProcessor _questionProcessor;

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

    private string _content;

    /// <summary>
    /// Current question content.
    /// </summary>
    public string Content
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

    private string _sound;

    /// <summary>
    /// Current background sound.
    /// </summary>
    public string Sound
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

    private string _oral;

    /// <summary>
    /// Current oral text.
    /// </summary>
    public string Oral
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
    public QuestionPlayViewModel(Question question, QDocument document)
    {
        _questionProcessor = new QuestionProcessor(question, new MediaSource(document), this);

        Play = new SimpleCommand(Play_Executed);
    }

    public void Play_Executed(object arg)
    {
        if (_isFinished)
        {
            return;
        }

        IsAnswer = false;

        _isFinished = !_questionProcessor.PlayNext();

        if (_isFinished)
        {
            Play.CanBeExecuted = false;
        }
    }

    public void OnText(string text, IMedia backgroundSound)
    {
        Content = text;
        Sound = backgroundSound?.Uri;
        ContentType = ContentTypes.Text;
    }

    public void OnOral(string oralText)
    {
        Oral = oralText;
    }

    public void OnImage(IMedia image, IMedia backgroundSound)
    {
        Content = image.Uri;
        Sound = backgroundSound?.Uri;
        ContentType = ContentTypes.Image;
    }

    public void OnSound(IMedia sound)
    {
        Sound = sound.Uri;
        ContentType = ContentTypes.Audio;
    }

    public void OnVideo(IMedia video)
    {
        Content = video.Uri;
        ContentType = ContentTypes.Video;
    }

    public void OnUnsupportedAtom(Atom atom)
    {
        Content = string.Format(Properties.Resources.UnsupportedFragment, atom.Text);
        ContentType = ContentTypes.Text;
    }

    public void AskAnswer()
    {
        IsAnswer = true;
        Oral = null;
    }

    /// <inheritdoc cref="IMediaSource" />
    private class MediaSource : IMediaSource
    {
        private readonly QDocument _document;

        /// <summary>
        /// Initializes a new instance of <see cref="MediaSource" /> class.
        /// </summary>
        /// <param name="document"></param>
        public MediaSource(QDocument document) => _document = document;

        public IMedia GetMedia(Atom atom) => _document.Wrap(atom);
    }
}
