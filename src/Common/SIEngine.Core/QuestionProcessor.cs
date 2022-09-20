using SIPackages;
using SIPackages.Core;
using System.Text;

namespace SIEngine.Core
{
    /// <summary>
    /// Performs SI question playing.
    /// </summary>
    public sealed class QuestionProcessor
    {
        private readonly Question _question;
        private readonly IMediaSource _mediaSource;
        private readonly IQuestionPlayHandler _questionPlayHandler;

        private int _atomIndex = 0;
        private bool _useAnswerMarker = false;
        private QuestionProcessorStates _state = QuestionProcessorStates.Question;

        private Atom? ActiveAtom => _atomIndex < _question.Scenario.Count ? _question.Scenario[_atomIndex] : null;

        /// <summary>
        /// Initializes a new instance of <see cref="QuestionProcessor" /> class.
        /// </summary>
        /// <param name="question">Question to play.</param>
        /// <param name="mediaSource">Atom media source.</param>
        /// <param name="questionPlayHandler">Handles question play stages.</param>
        public QuestionProcessor(
            Question question,
            IMediaSource mediaSource,
            IQuestionPlayHandler questionPlayHandler)
        {
            _question = question;
            _mediaSource = mediaSource;
            _questionPlayHandler = questionPlayHandler;
        }

        /// <summary>
        /// Moves the player to the next stage.
        /// </summary>
        /// <returns>Can the player be moved futher.</returns>
        /// <exception cref="InvalidOperationException">Player comes to an invalid state.</exception>
        public bool PlayNext()
        {
            switch (_state)
            {
                case QuestionProcessorStates.Question:
                    var proceed = PlayQuestionAtom();

                    if (!proceed)
                    {
                        _state = QuestionProcessorStates.AskAnswer;
                    }
                    break;

                case QuestionProcessorStates.AskAnswer:
                    _questionPlayHandler.AskAnswer();
                    _state = QuestionProcessorStates.Answer;
                    break;

                case QuestionProcessorStates.Answer:
                    if (!_useAnswerMarker)
                    {
                        _questionPlayHandler.OnText(_question.Right.FirstOrDefault() ?? "", null);
                        _state = QuestionProcessorStates.None;
                    }

                    var proceedAnswer = PlayQuestionAtom();

                    if (!proceedAnswer)
                    {
                        _state = QuestionProcessorStates.None;
                    }
                    break;

                default:
                    throw new InvalidOperationException();
            }

            return _state != QuestionProcessorStates.None;
        }

        /// <summary>
        /// Plays next question atom.
        /// </summary>
        /// <returns>
        /// Should the play proceed later.
        /// </returns>
        internal bool PlayQuestionAtom()
        {
            var activeAtom = ActiveAtom;
            
            if (activeAtom == null)
            {
                return false;
            }

            switch (activeAtom.Type)
            {
                case AtomTypes.Text:
                    {
                        var text = CollectText();
                        var sound = GetBackgroundSound(activeAtom);
                        _questionPlayHandler.OnText(text, sound);
                        _atomIndex++;
                        break;
                    }

                case AtomTypes.Oral:
                    var oralText = CollectText(AtomTypes.Oral);
                    _questionPlayHandler.OnOral(oralText);
                    _atomIndex++;

                    break;

                case AtomTypes.Image:
                case AtomTypes.Audio:
                case AtomTypes.Video:
                    {
                        // Multimedia content
                        var media = GetMedia(activeAtom);

                        if (media == null)
                        {
                            break;
                        }

                        var isSound = activeAtom.Type == AtomTypes.Audio;
                        var isImage = activeAtom.Type == AtomTypes.Image;

                        if (isImage)
                        {
                            var sound = GetBackgroundSound(activeAtom);
                            _questionPlayHandler.OnImage(media, sound);
                        }
                        else
                        {
                            if (isSound)
                            {
                                (IMedia? image, string? text) = GetBackgroundImageOrText(activeAtom);

                                if (image != null || text != null)
                                {
                                    if (image != null)
                                    {
                                        _questionPlayHandler.OnImage(image, media);
                                    }
                                    else
                                    {
                                        _questionPlayHandler.OnText(text!, media);
                                    }
                                }
                                else
                                {
                                    _questionPlayHandler.OnSound(media);
                                }
                            }
                            else
                            {
                                _questionPlayHandler.OnVideo(media);
                            }
                        }

                        _atomIndex++;
                        break;
                    }

                case AtomTypes.Marker:
                    _atomIndex++;

                    if (_atomIndex < _question.Scenario.Count)
                    {
                        _useAnswerMarker = true; // Skipping other atoms because they belong to the answer
                    }

                    return false;

                default:
                    _questionPlayHandler.OnUnsupportedAtom(activeAtom);
                    _atomIndex++; // Other types are not processed
                    break;
            }

            if (_atomIndex == _question.Scenario.Count)
            {
                return false;
            }

            if (ActiveAtom?.Type == AtomTypes.Marker)
            {
                if (_atomIndex + 1 < _question.Scenario.Count)
                {
                    _atomIndex++;
                    _useAnswerMarker = true;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Collects text of sequentially placed text atoms.
        /// </summary>
        /// <returns>Collected text.</returns>
        private string CollectText(string atomType = AtomTypes.Text)
        {
            var text = new StringBuilder();

            while (ActiveAtom?.Type == atomType)
            {
                if (text.Length > 0)
                {
                    text.AppendLine();
                }

                text.Append(ActiveAtom.Text);

                _atomIndex++;
            }

            _atomIndex--;

            return text.ToString();
        }

        /// <summary>
        /// Tries to get background sound from current atom.
        /// </summary>
        private IMedia? GetBackgroundSound(Atom atom)
        {
            if (atom.AtomTime == -1 && _atomIndex + 1 < _question.Scenario.Count) // Join atom with next
            {
                var nextAtom = _question.Scenario[_atomIndex + 1];

                if (nextAtom.Type == AtomTypes.Audio)
                {
                    _atomIndex++;
                    return GetMedia(nextAtom);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets background image or text for current audio atom.
        /// </summary>
        private (IMedia?, string?) GetBackgroundImageOrText(Atom atom)
        {
            if (atom.AtomTime == -1 && _atomIndex + 1 < _question.Scenario.Count) // Join atom with next
            {
                var nextAtom = _question.Scenario[_atomIndex + 1];

                if (nextAtom.Type == AtomTypes.Image)
                {
                    _atomIndex++;
                    var media = GetMedia(nextAtom);

                    if (media != null)
                    {
                        return (media, null);
                    }
                }
                else if (nextAtom.Type == AtomTypes.Text)
                {
                    _atomIndex++;
                    return (null, CollectText());
                }
            }

            return (null, null);
        }

        private IMedia? GetMedia(Atom atom)
        {
            var media = _mediaSource.GetMedia(atom);

            if (media == null)
            {
                _atomIndex++;
            }

            return media;
        }
    }
}