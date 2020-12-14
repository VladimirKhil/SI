using Notions;
using SICore.BusinessLogic;
using SICore.Clients.Game;
using SICore.Connections;
using SICore.Results;
using SICore.Special;
using SICore.Utils;
using SIData;
using SIPackages;
using SIPackages.Core;
using SIPackages.Providers;
using SIUI.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using R = SICore.Properties.Resources;

namespace SICore
{
    /// <summary>
    /// Логика игры
    /// </summary>
    public sealed class GameLogic : Logic<GameData>, SIEngine.IEngineSettingsProvider
    {
        private const string OfObjectPropertyFormat = "{0} {1}: {2}";

        private const int MaxAnswerLength = 250;

        private readonly Game _actor;
        private readonly GameActions _gameActions;
        private readonly ILocalizer LO;

        public SIEngine.EngineBase Engine { get; private set; }

        public GameLogic(Game game, GameData data, GameActions gameActions, ILocalizer localizer)
            : base(data)
        {
            _actor = game;
            _gameActions = gameActions;
            LO = localizer;
        }

        internal void Run(SIDocument document)
        {
            Engine = _data.Settings.AppSettings.GameMode == SIEngine.GameModes.Tv ?
                (SIEngine.EngineBase)new SIEngine.TvEngine(document, this) :
                new SIEngine.SportEngine(document, this);

            Engine.Package += Engine_Package;
            Engine.GameThemes += Engine_GameThemes;
            Engine.Round += Engine_Round;
            Engine.RoundThemes += Engine_RoundThemes;
            Engine.Theme += Engine_Theme;
            Engine.Question += Engine_Question; // Вопрос в упрощённой версии
            Engine.QuestionSelected += Engine_QuestionSelected; // Вопрос в классической версии

            Engine.QuestionText += Engine_QuestionText;
            Engine.QuestionOral += Engine_QuestionOral;
            Engine.QuestionImage += Engine_QuestionImage;
            Engine.QuestionSound += Engine_QuestionSound;
            Engine.QuestionVideo += Engine_QuestionVideo;
            Engine.QuestionOther += Engine_QuestionOther;
            Engine.QuestionAtom += Engine_QuestionAtom;
            Engine.WaitTry += Engine_WaitTry;

            Engine.SimpleAnswer += Engine_SimpleAnswer;
            Engine.RightAnswer += Engine_RightAnswer;
            Engine.EndQuestion += Engine_EndQuestion;
            Engine.NextQuestion += Engine_NextQuestion;
            Engine.RoundEmpty += Engine_RoundEmpty;
            Engine.RoundTimeout += Engine_RoundTimeout;

            Engine.FinalThemes += Engine_FinalThemes;
            Engine.WaitDelete += Engine_WaitDelete;
            Engine.ThemeSelected += Engine_ThemeDeleted;
            Engine.PrepareFinalQuestion += Engine_PrepareFinalQuestion;

            Engine.EndGame += Engine_EndGame;

            _data.PackageDoc = document;
            _data.GameResultInfo.PackageName = Engine.PackageName;
            _data.GameResultInfo.PackageID = document.Package.ID;

            if (_data.Settings.IsAutomatic)
            {
                // Необходимо запустить игру автоматически
                ScheduleExecution(Tasks.AutoGame, Constants.AutomaticGameStartDuration);
                _data.TimerStartTime[2] = DateTime.UtcNow;

                _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Go, Constants.AutomaticGameStartDuration, -2);
            }
        }

        private void Engine_Package(Package package)
        {
            _data.Package = package;

            if (_data.Package.Info.Comments.Text.StartsWith("{random}"))
            {
                _data.GameResultInfo.PackageName += Environment.NewLine + _data.Package.Info.Comments.Text.Substring(8);
            }

            _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.PackageId, package.ID));

            ProcessPackage(package, 1);
        }

        private void Engine_GameThemes(string[] gameThemes)
        {
            _gameActions.ShowmanReplic(GetRandomString(LO[nameof(R.GameThemes)]));

            var msg = new StringBuilder();
            msg.Append(Messages.GameThemes).Append(Message.ArgsSeparatorChar).Append(string.Join(Message.ArgsSeparator, gameThemes));

            _gameActions.SendMessage(msg.ToString());

            ScheduleExecution(Tasks.MoveNext, 11 + 150 * gameThemes.Length / 18);
        }

        private void Engine_Round(Round round)
        {
            _data.AllowAppellation = false;
            _data.Round = round;
            ProcessRound(round, 1);

            if (_data.Settings.AppSettings.GameMode == SIEngine.GameModes.Sport)
            {
                RunRoundTimer();
            }
        }

        private void InitThemes(Theme[] themes)
        {
            _data.TInfo.RoundInfo.Clear();

            foreach (var theme in themes)
            {
                _data.TInfo.RoundInfo.Add(new ThemeInfo { Name = theme.Name });
            }

            _gameActions.ShowmanReplic(GetRandomString(LO[nameof(R.RoundThemes)]));

            lock (_data.TabloInformStageLock)
            {
                _gameActions.InformRoundThemes();
                _data.TabloInformStage = 1;
            }
        }

        private void Engine_RoundThemes(Theme[] themes)
        {
            if (themes.Length == 0)
            {
                throw new ArgumentException("themes.Length == 0", nameof(themes));
            }

            InitThemes(themes);

            // Заполним изначальную таблицу вопросов
            _data.ThemeInfo = new bool[themes.Length];

            var maxQuests = themes.Max(t => t.Questions.Count);
            for (var i = 0; i < themes.Length; i++)
            {
                var questionsCount = themes[i].Questions.Count;
                _data.TInfo.RoundInfo[i].Questions.Clear();

                for (var j = 0; j < maxQuests; j++)
                {
                    _data.TInfo.RoundInfo[i].Questions.Add(new QuestionInfo { Price = j < questionsCount ? themes[i].Questions[j].Price : QuestionHelper.InvalidQuestionPrice });
                }
            }

            lock (_data.TabloInformStageLock)
            {
                _gameActions.InformTablo();
                _data.TabloInformStage = 2;
            }

            _data.IsQuestionPlaying = false;
            ScheduleExecution(Tasks.AskFirst, 19 * _data.TInfo.RoundInfo.Count + ClientData.Rand.Next(10));
        }

        /// <summary>
        /// Число активных вопросов в раунде
        /// </summary>
        private int GetRoundActiveQuestionsCount() => _data.TInfo.RoundInfo.Sum(theme => theme.Questions.Count(QuestionHelper.IsActive));

        private void Engine_Theme(Theme theme)
        {
            _data.Theme = theme;
            ProcessTheme(theme, -1);
        }

        private void Engine_Question(Question question)
        {
            _data.Question = question;
            _data.IsQuestionPlaying = true;
            _data.IsAnswer = false;
            _data.CurPriceRight = _data.CurPriceWrong = question.Price;
            _data.IsPlayingMedia = false;
            _data.IsPlayingMediaPaused = false;

            _gameActions.ShowmanReplic($"{_data.Theme.Name}, {question.Price}");
            _gameActions.SendMessageWithArgs(Messages.Question, question.Price);

            _data.QuestionHistory.Clear();

            if (_data.Settings.AppSettings.HintShowman)
            {
                var rightAnswers = question.GetRightAnswers();
                var rightAnswer = rightAnswers.FirstOrDefault() ?? LO[nameof(R.NotSet)];
                _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Hint, rightAnswer), _data.ShowMan.Name);
            }

            ScheduleExecution(Tasks.QuestionType, 10, 1, force: true);
        }

        private void Engine_QuestionSelected(int themeIndex, int questionIndex, Theme theme, Question question)
        {
            _gameActions.SendMessageWithArgs(Messages.Choice, themeIndex, questionIndex);

            if (_data.Settings.AppSettings.HintShowman)
            {
                var rightAnswers = question.GetRightAnswers();
                var rightAnswer = rightAnswers.FirstOrDefault() ?? LO[nameof(R.NotSet)];
                _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Hint, rightAnswer), _data.ShowMan.Name);
            }

            var s = new StringBuilder(theme.Name).Append(", ").Append(question.Price);
            _gameActions.PlayerReplic(_data.ChooserIndex, s.ToString());

            _data.Theme = theme;
            _data.Question = question;

            _data.CurPriceRight = _data.CurPriceWrong = question.Price;

            _data.IsAnswer = false;

            _data.QuestionHistory.Clear();

            // Если информация о теме ещё не выводилась
            if (_data.Settings.AppSettings.GameMode == SIEngine.GameModes.Tv && !_data.ThemeInfo[themeIndex])
            {
                _data.ThemeInfo[themeIndex] = true;
                ScheduleExecution(Tasks.Theme, 10, 1, true);
            }
            else
            {
                ScheduleExecution(Tasks.QuestionType, 10, 1, true);
            }
        }

        private void Engine_QuestionText(string text, IMedia sound)
        {
            _data.QLength = text.Length;

            _data.IsPartial = IsPartial();

            if (_data.IsPartial)
            {
                _gameActions.SendMessageWithArgs(Messages.TextShape, Regex.Replace(text, "[^\r\n\t\f ]", "и"));

                _data.Text = text;
                _data.TextLength = 0;
                ExecuteTask((int)Tasks.PrintPartial, 0);
            }
            else
            {
                _gameActions.SendMessageWithArgs(Messages.Atom, AtomTypes.Text, text);
                _gameActions.SystemReplic(text);
            }

            if (sound != null)
            {
                ShareMedia(sound, AtomTypes.Audio, true);
            }
        }

        /// <summary>
        /// Будет ли вопрос выводиться по частям
        /// </summary>
        /// <returns></returns>
        private bool IsPartial() =>
            _data.Round != null
                && _data.Round.Type != RoundTypes.Final
                && _data.Type?.Name == QuestionTypes.Simple
                && _data.Settings != null
                && !_data.Settings.AppSettings.FalseStart
                && _data.Settings.AppSettings.PartialText
                && !_data.IsAnswer;

        private void Engine_QuestionOral(string oralText)
        {
            _data.IsPartial = false;
            _gameActions.SendMessageWithArgs(Messages.Atom, AtomTypes.Oral, oralText);
            _gameActions.ShowmanReplic(oralText);
            _data.QLength = oralText.Length;
        }

        private void ShareMedia(IMedia link, string atomType, bool isBackground = false)
        {
            try
            {
                var msg = new StringBuilder();
                msg.Append(isBackground ? Messages.Atom_Second : Messages.Atom).Append(Message.ArgsSeparatorChar).Append(atomType).Append(Message.ArgsSeparatorChar);

                if (link.GetStream == null) // Внешняя ссылка
                {
                    if (Uri.TryCreate(link.Uri, UriKind.Absolute, out _))
                    {
                        msg.Append(MessageParams.Atom_Uri).Append(Message.ArgsSeparatorChar).Append(link.Uri);
                        _gameActions.SendMessage(msg.ToString());
                    }
                    else
                    {
                        _gameActions.SendMessageWithArgs(isBackground ? Messages.Atom_Second : Messages.Atom, AtomTypes.Text, string.Format(LO[nameof(R.MediaNotFound)], link.Uri));
                    }
                }
                else
                {
                    var filename = link.Uri;

                    var mediaCategory = atomType == AtomTypes.Image ? SIDocument.ImagesStorageName
                        : (atomType == AtomTypes.Audio ? SIDocument.AudioStorageName
                        : (atomType == AtomTypes.Video ? SIDocument.VideoStorageName : atomType));

                    var uri = _data.Share.CreateURI(filename, link.GetStream, mediaCategory);
                    var localUri = ClientData.DocumentPath != null ? Path.Combine(ClientData.DocumentPath, mediaCategory, Uri.UnescapeDataString(filename)) : null;

                    if (localUri != null && !File.Exists(localUri))
                    {
                        localUri = null;
                    }

                    _data.OpenedFiles.Add(filename);

                    foreach (var person in _data.AllPersons.Keys)
                    {
                        var msg2 = new StringBuilder(msg.ToString());
                        if (_gameActions.Client.CurrentServer.Contains(person))
                        {
                            msg2.Append(MessageParams.Atom_Uri).Append(Message.ArgsSeparatorChar).Append(localUri ?? uri);
                        }
                        else
                        {
                            msg2.Append(MessageParams.Atom_Uri).Append(Message.ArgsSeparatorChar).Append(uri.Replace("http://localhost", "http://" + Constants.GameHost));
                        }

                        _gameActions.SendMessage(msg2.ToString(), person);
                    }
                }
            }
            catch (Exception exc)
            {
                ClientData.BackLink.OnError(exc);
            }
        }

        private void Engine_QuestionImage(IMedia image, IMedia sound)
        {
            _data.IsPartial = false;
            ShareMedia(image, AtomTypes.Image);
            if (sound != null)
            {
                ShareMedia(sound, AtomTypes.Audio, true);
            }
        }

        private void Engine_QuestionSound(IMedia sound)
        {
            _data.IsPartial = false;
            ShareMedia(sound, AtomTypes.Audio);
        }

        private void Engine_QuestionVideo(IMedia video)
        {
            _data.IsPartial = false;
            ShareMedia(video, AtomTypes.Video);
        }

        private void Engine_QuestionOther(Atom atom)
        {
            // Не поддерживается
        }

        private void Engine_QuestionAtom(Atom atom)
        {
            if (_data.IsAnswer)
            {
                var last = _data.QuestionHistory.LastOrDefault();
                if (last == null || !last.IsRight) // Верного ответа не было
                {
                    var answer = _data.Question.Right.FirstOrDefault();
                    var printedAnswer = answer != null ? $"{LO[nameof(R.RightAnswer)]}: {answer}" : LO[nameof(R.RightAnswerInOnTheScreen)];

                    _gameActions.ShowmanReplic(printedAnswer);
                }

                _data.IsAnswer = false;
            }

            var atomTime = DetectAtomTime(atom);

            _data.AtomTime = atomTime;
            _data.AtomStart = DateTime.UtcNow;
            _data.AtomType = atom.Type;
            _data.CanMarkQuestion = true;

            _data.IsPlayingMedia = atom.Type == AtomTypes.Video || atom.Type == AtomTypes.Audio;
            _data.IsPlayingMediaPaused = false;

            var isPartial = _data.IsPartial && atom.Type == AtomTypes.Text;
            if (isPartial)
            {
                return;
            }

            ScheduleExecution(Tasks.MoveNext, atomTime, force: true);
            _data.TimeThinking = 0.0;
        }

        private int DetectAtomTime(Atom atom)
        {
            if (atom.AtomTime > 0)
            {
                return atom.AtomTime * 10;
            }

            if (atom.Type == AtomTypes.Text || atom.Type == AtomTypes.Oral)
            {
                var readingSpeed = Math.Max(1, _data.Settings.AppSettings.ReadingSpeed);
                return Math.Max(1, 10 * _data.QLength / readingSpeed);
            }

            if (atom.Type == AtomTypes.Video || atom.Type == AtomTypes.Audio)
            {
                _data.HaveViewedAtom = _data.Viewers.Count
                    + _data.Players.Where(pa => pa.IsHuman && pa.IsConnected).Count()
                    + (_data.ShowMan.IsHuman && _data.ShowMan.IsConnected ? 1 : 0);

                // TODO: если всё так тормозит, что за 2 минуты аудио/видео ни у кого не прогружается,
                // что происходит при игре с фальстартами и без?
                return 1200;
            }
            
            return 50 + _data.Settings.AppSettings.TimeSettings.TimeForMediaDelay * 10;
        }

        private void Engine_WaitTry(Question question, bool final)
        {
            if (!final)
            {
                _data.IsQuestionFinished = true;
                _data.IsPlayingMedia = false;
                _data.IsPlayingMediaPaused = false;

                if (!_data.IsQuestionPlaying)
                    ScheduleExecution(Tasks.MoveNext, 1, force: true);
                else if (_data.Type.Name != QuestionTypes.Simple)
                    ScheduleExecution(Tasks.AskAnswer, 1, force: true);
                else
                    ScheduleExecution(Tasks.AskToTry, 1 + (_data.Settings.AppSettings.Managed ? 0 : ClientData.Rand.Next(10)), force: true); // Добавим случайное смещение, чтобы было трудно заранее (до появления рамки) нажать на кнопку
            }
            else
            {
                ScheduleExecution(Tasks.AskAnswer, 1, force: true);
                _gameActions.SendMessageWithArgs(Messages.FinalThink, _data.Settings.AppSettings.TimeSettings.TimeForFinalThinking);
            }
        }

        private void Engine_SimpleAnswer(string answer)
        {
            if (answer == null)
                answer = LO[nameof(R.AnswerNotSet)];

            answer = answer.LeaveFirst(MaxAnswerLength);

            if (_data.AnnounceAnswer)
            {
                var s = $"{LO[nameof(R.RightAnswer)]}: {answer}";
                _gameActions.Print(_gameActions.Showman() + ReplicManager.OldReplic(s));
            }

            _gameActions.SendMessageWithArgs(Messages.RightAnswer, AtomTypes.Text, answer);

            _data.AllowAppellation = true;
            var answerTime = _data.Settings.AppSettings.TimeSettings.TimeForRightAnswer;
            ScheduleExecution(Tasks.QuestSourComm, (answerTime == 0 ? 2 : answerTime) * 10, 1);
        }

        private void Engine_RightAnswer()
        {
            _data.IsAnswer = true;

            if (!_data.IsRoundEnding)
            {
                var roundDuration = DateTime.UtcNow.Subtract(_data.TimerStartTime[0]).TotalMilliseconds / 100;
                if (_data.Stage == GameStage.Round && _data.Round.Type != RoundTypes.Final
                    && roundDuration >= _data.Settings.AppSettings.TimeSettings.TimeOfRound * 10)
                {
                    // Завершение раунда по времени
                    _gameActions.SendMessageWithArgs(Messages.Timer, 0, "STOP");

                    Engine.SetTimeout();
                }
            }
        }

        private void Engine_NextQuestion()
        {
            if (_data.OpenedFiles.Count > 0)
            {
                _data.Share.StopURI(_data.OpenedFiles);
                _data.OpenedFiles.Clear();
            }

            if (_data.Settings.AppSettings.GameMode == SIEngine.GameModes.Tv)
            {
                var activeQuestionsCount = GetRoundActiveQuestionsCount();

                if (activeQuestionsCount == 0)
                {
                    throw new Exception($"{nameof(activeQuestionsCount)} == 0! {((SIEngine.TvEngine)Engine).LeftQuestionsCount}");
                }

                ScheduleExecution(Tasks.AskToChoose, 4, force: true);
            }
            else
            {
                ScheduleExecution(Tasks.MoveNext, 10, force: true);
            }

            foreach (var player in _data.Players)
            {
                if (player.PingPenalty > 0)
                {
                    player.PingPenalty--;
                }
            }

            _data.CanMarkQuestion = false;
            _data.AnswererIndex = -1;
        }

        private void Engine_EndQuestion(int themeIndex, int questionIndex)
        {
            _data.TInfo.RoundInfo[themeIndex].Questions[questionIndex].Price = QuestionHelper.InvalidQuestionPrice;
        }

        private void FinishRound(bool move = true)
        {
            if (_data.OpenedFiles.Count > 0)
            {
                _data.Share.StopURI(_data.OpenedFiles);
                _data.OpenedFiles.Clear();
            }

            _data.IsQuestionPlaying = false;

            _gameActions.InformSums();
            _gameActions.AnnounceSums();
            _gameActions.SendMessage(Messages.Stop); // Timers STOP

            _data.IsThinking = false;

            _data.IsWaiting = false;
            _data.Decision = DecisionType.None;

            _data.IsRoundEnding = true;
            if (move)
            {
                ScheduleExecution(Tasks.MoveNext, 40);
            }
            else
            {
                // Раунд был завершён вручную. Нам нужно максимально корректно свернуть текущие задачи
                ClearOldTasks();
            }
        }

        void Engine_RoundEmpty()
        {
            _gameActions.ShowmanReplic(GetRandomString(LO[nameof(R.AllQuestions)]));
            FinishRound();
        }

        void Engine_RoundTimeout()
        {
            _gameActions.SendMessage(Messages.Timeout);
            _gameActions.ShowmanReplic(GetRandomString(LO[nameof(R.AllTime)]));
            FinishRound();
        }

        private void Engine_FinalThemes(Theme[] themes)
        {
            InitThemes(themes);

            _data.ThemeDeleters = new ThemeDeletersEnumerator(_data.Players, _data.TInfo.RoundInfo.Count(t => t.Name != null));
            _data.ThemeDeleters.Reset(true);

            ScheduleExecution(Tasks.MoveNext, 20 + ClientData.Rand.Next(10));
        }

        private void Engine_WaitDelete() => ExecuteTask((int)Tasks.AskToDelete, 0);

        private void Engine_ThemeDeleted(int themeIndex)
        {
            if (themeIndex < 0 || themeIndex >= _data.TInfo.RoundInfo.Count)
            {
                var errorMessage = new StringBuilder(themeIndex.ToString()).Append(' ')
                    .Append(string.Join("|", _data.TInfo.RoundInfo.Select(t => $"({t.Name != QuestionHelper.InvalidThemeName} {t.Questions.Count})"))).Append(' ')
                    .Append(_data.ThemeIndex).Append(' ')
                    .Append(string.Join(",", ((SIEngine.TvEngine)Engine).FinalMap));
                throw new ArgumentException(errorMessage.ToString(), nameof(themeIndex));
            }

            _gameActions.SendMessageWithArgs(Messages.Out, themeIndex);

            var playerIndex = _data.ThemeDeleters.Current.PlayerIndex;
            var themeName = _data.TInfo.RoundInfo[themeIndex].Name;
            _gameActions.PlayerReplic(playerIndex, themeName);
        }

        private async void Engine_PrepareFinalQuestion(Theme theme, Question question)
        {
            await Task.Delay(1500);
            _data.ThemeIndex = _data.Round.Themes.IndexOf(theme);

            _data.Theme = theme;
            var str = GetRandomString(LO[nameof(R.PlayTheme)]);
            str += " " + theme.Name;
            _gameActions.ShowmanReplic(str);

            ScheduleExecution(Tasks.Theme, 10, 1);
        }

        private void Engine_EndGame()
        {
            // Очищаем табло
            _gameActions.SendMessage(Messages.Stop);
            _gameActions.SystemReplic($"{LO[nameof(R.GameResults)]}: ");

            for (var i = 0; i < _data.Players.Count; i++)
            {
                _gameActions.SystemReplic($"{_data.Players[i].Name}: {Notion.FormatNumber(_data.Players[i].Sum)}");
                _data.GameResultInfo.Results.Add(new PersonResult { Name = _data.Players[i].Name, Sum = _data.Players[i].Sum });
            }

            ScheduleExecution(Tasks.Winner, 15 + ClientData.Rand.Next(10));
        }

        protected override void Dispose(bool disposing)
        {
            var locked = Monitor.TryEnter(ClientData.TaskLock, TimeSpan.FromSeconds(5.0));
            if (!locked)
            {
                ClientData.BackLink.OnError(new Exception($"Cannot lock {nameof(ClientData.TaskLock)}!"));
            }

            if (_data.AcceptedReports > 0)
            {
                _data.AcceptedReports = 0;
                _data.BackLink.SaveReport(_data.GameResultInfo);
            }

            try
            {
                if (Engine != null)
                {
                    Engine.Dispose();
                    Engine = null;
                }

                base.Dispose(disposing);
            }
            finally
            {
                if (locked)
                {
                    Monitor.Exit(ClientData.TaskLock);
                }
            }
        }

        internal override void Stop(StopReason reason)
        {
            if (_stopReason != StopReason.None)
            {
                return;
            }

            if (reason == StopReason.Decision)
            {
                ClientData.IsWaiting = false; // Чтобы не обрабатывать повторные сообщения
            }

            _stopReason = reason;
            ExecuteImmediate();
        }

        internal void CancelStop() => _stopReason = StopReason.None;

        /// <summary>
        /// Решение принято
        /// </summary>
        private bool OnDecision()
        {
            switch (_data.Decision)
            {
                case DecisionType.StarterChoosing:
                    return OnDecisionStarterChoosing();

                case DecisionType.QuestionChoosing:

                    #region QuestionChoosing

                    if (_data.ThemeIndex != -1 && _data.QuestionIndex != -1)
                    {
                        _data.AllowAppellation = false;
                        StopWaiting();
                        ((SIEngine.TvEngine)Engine).SelectQuestion(_data.ThemeIndex, _data.QuestionIndex);
                        return true;
                    }

                    break;

                    #endregion

                case DecisionType.Answering:
                    return OnDecisionAnswering();

                case DecisionType.AnswerValidating:
                    return OnDecisionAnswerValidating();

                case DecisionType.CatGiving:

                    #region CatGiving

                    if (_data.Answerer != null)
                    {
                        StopWaiting();

                        var s = _data.ChooserIndex == _data.AnswererIndex ?
                            LO[nameof(R.ToMyself)] : _data.Answerer.Name;
                        _gameActions.PlayerReplic(_data.ChooserIndex, s);

                        _data.ChooserIndex = _data.AnswererIndex;
                        _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex, "+");
                        ScheduleExecution(Tasks.CatInfo, 10);
                        return true;
                    }

                    break;

                    #endregion

                case DecisionType.CatCostSetting:

                    #region CatCostSetting

                    if (_data.CurPriceRight != -1)
                    {
                        StopWaiting();

                        _data.CurPriceWrong = _data.CurPriceRight;
                        _gameActions.PlayerReplic(_data.AnswererIndex, _data.CurPriceRight.ToString());

                        ScheduleExecution(Tasks.AskCatCost, 20);
                        return true;
                    }

                    break;

                    #endregion

                case DecisionType.NextPersonStakeMaking:
                    return OnDecisionNextPersonStakeMaking();

                case DecisionType.AuctionStakeMaking:
                    return OnDecisionAuctionStakeMaking();

                case DecisionType.NextPersonFinalThemeDeleting:

                    #region NextPersonFinalThemeDeleting

                    if (_data.ThemeDeleters.Current.PlayerIndex != -1)
                    {
                        StopWaiting();

                        _gameActions.ShowmanReplic(LO[nameof(R.ThemeDeletes)] + " " + _data.Players[_data.ThemeDeleters.Current.PlayerIndex].Name);

                        _data.ThemeDeleters.MoveBack();

                        ScheduleExecution(Tasks.AskToDelete, 1);
                        return true;
                    }
                    break;

                    #endregion

                case DecisionType.FinalThemeDeleting:
                    return OnFinalThemeDeleting();

                case DecisionType.FinalStakeMaking:

                    #region FinalStakeMaking

                    if (_data.NumOfStakers == 0)
                    {
                        StopWaiting();

                        _gameActions.ShowmanReplic(LO[nameof(R.ThankYou)]);

                        var questionsCount = _data.Theme.Questions.Count;
                        _data.QuestionIndex = ClientData.Rand.Next(questionsCount); // А то в отчётах об игре встречаются глюки
                        _data.Question = _data.Theme.Questions[_data.QuestionIndex];

                        ScheduleExecution(Tasks.QuestionType, 10, 1);
                        return true;
                    }
                    break;

                #endregion

                case DecisionType.AppellationDecision:
                    return OnAppellationDecision();
            }

            return false;
        }

        private bool OnAppellationDecision()
        {
            StopWaiting();
            ScheduleExecution(Tasks.CheckAppellation, 20);

            return true;
        }

        private bool OnFinalThemeDeleting()
        {
            if (_data.ThemeIndex == -1)
            {
                return false;
            }

            StopWaiting();
            ((SIEngine.TvEngine)Engine).SelectTheme(_data.ThemeIndex);

            var innerThemeIndex = ((SIEngine.TvEngine)Engine).OnReady(out bool more);
            if (innerThemeIndex > -1)
            {
                // innerThemeIndex может не совпадать с _data.ThemeIndex. См. TvEngine.SelectTheme()
                _data.TInfo.RoundInfo[_data.ThemeIndex].Name = null;
                if (more)
                {
                    ScheduleExecution(Tasks.MoveNext, 10);
                }
            }

            return true;
        }

        private bool OnDecisionAuctionStakeMaking()
        {
            if (!_data.StakeType.HasValue)
            {
                return false;
            }

            StopWaiting();

            if (_data.OrderIndex == -1)
            {
                throw new ArgumentException($"{nameof(_data.OrderIndex)} == -1! {_data.OrderHistory}", nameof(_data.OrderIndex));
            }

            var playerIndex = _data.Order[_data.OrderIndex];

            if (playerIndex < 0 || playerIndex >= _data.Players.Count)
            {
                throw new ArgumentException($"{nameof(playerIndex)} == ${playerIndex} but it must be in [0; ${_data.Players.Count - 1}]! ${_data.OrderHistory}", nameof(playerIndex));
            }

            var stakeMaking = string.Join(",", _data.Players.Select(p => p.StakeMaking));
            var stakeSum = _data.StakeType == StakeMode.Sum ? _data.StakeSum.ToString() : "";
            _data.OrderHistory.Append($"Stake received: {playerIndex} {_data.StakeType.Value} {stakeSum} {stakeMaking}").AppendLine();

            if (_data.StakeType == StakeMode.Nominal)
            {
                _gameActions.PlayerReplic(playerIndex, LO[nameof(R.Nominal)]);
                _data.Stake = _data.CurPriceRight;
                _data.StakerIndex = playerIndex;
            }
            else if (_data.StakeType == StakeMode.Sum)
            {
                _gameActions.PlayerReplic(playerIndex, Notion.FormatNumber(_data.StakeSum));
                _data.Stake = _data.StakeSum;
                _data.StakerIndex = playerIndex;
            }
            else if (_data.StakeType == StakeMode.Pass)
            {
                _gameActions.PlayerReplic(playerIndex, LO[nameof(R.Pass)]);
                _data.Players[playerIndex].StakeMaking = false;
            }
            else
            {
                _gameActions.PlayerReplic(playerIndex, LO[nameof(R.VaBank)]);
                _data.Stake = _data.Players[playerIndex].Sum;
                _data.StakerIndex = playerIndex;
                _data.AllIn = true;
            }

            var printedStakeType = _data.StakeType == StakeMode.Nominal ? StakeMode.Sum : _data.StakeType;

            var stakeMessage = new MessageBuilder(Messages.PersonStake, playerIndex, (int)printedStakeType);

            if (printedStakeType == StakeMode.Sum)
            {
                stakeMessage.Add(_data.Stake);
            }

            _gameActions.SendMessage(stakeMessage.Build());

            if (_data.StakeType != StakeMode.Pass)
            {
                for (var i = 0; i < _data.Players.Count; i++)
                {
                    var player = _data.Players[i];
                    if (i != _data.StakerIndex && player.StakeMaking && player.Sum <= _data.Stake)
                    {
                        player.StakeMaking = false;
                        _gameActions.SendMessageWithArgs(Messages.PersonStake, i, 2);
                    }
                }
            }

            var stakeMaking2 = string.Join(",", _data.Players.Select(p => p.StakeMaking));
            _data.OrderHistory.Append($"Stake making updated: {stakeMaking2}").AppendLine();

            var stakersCount = _data.Players.Count(p => p.StakeMaking);

            if (stakersCount == 1) // Игрок определился
            {
                for (var i = 0; i < _data.Players.Count; i++)
                {
                    if (_data.Players[i].StakeMaking)
                    {
                        _data.StakerIndex = i;
                    }
                }

                ScheduleExecution(Tasks.PrintAuctPlayer, 10);
                return true;
            }
            else if (stakersCount == 0)
            {
                AddHistory("Skipping question");
                Engine.SkipQuestion();
                ScheduleExecution(Tasks.MoveNext, 10);

                return true;
            }

            ScheduleExecution(Tasks.AskStake, 5);
            return true;
        }

        private bool OnDecisionAnswerValidating()
        {
            if (!_data.ShowmanDecision)
            {
                return false;
            }

            if (_data.Answerer == null)
            {
                throw new Exception("_data.Answerer == null");
            }

            StopWaiting();

            var answerResult = new AnswerResult { PlayerIndex = _data.AnswererIndex };
            _data.QuestionHistory.Add(answerResult);

            if (_data.Answerer.AnswerIsRight)
            {
                answerResult.IsRight = true;
                var showmanReplic = IsFinalRound() || _data.Question.Type.Name == QuestionTypes.Auction ? nameof(R.Bravo) : nameof(R.Right);
                
                var s = new StringBuilder(GetRandomString(LO[showmanReplic]));

                var canonicalAnswer = _data.Question.GetRightAnswers().FirstOrDefault();
                var isAnswerCanonical = canonicalAnswer != null && _data.Answerer.Answer.Simplify().Contains(canonicalAnswer.Simplify());

                if (!IsFinalRound())
                {
                    if (canonicalAnswer != null && !isAnswerCanonical)
                    {
                        s.AppendFormat(" [{0}]", canonicalAnswer);
                    }

                    s.AppendFormat(" (+{0})", _data.CurPriceRight.ToString().FormatNumber());
                    _gameActions.ShowmanReplic(s.ToString());

                    s = new StringBuilder(Messages.Person)
                        .Append(Message.ArgsSeparatorChar)
                        .Append('+')
                        .Append(Message.ArgsSeparatorChar)
                        .Append(_data.AnswererIndex)
                        .Append(Message.ArgsSeparatorChar).Append(_data.CurPriceRight);

                    _gameActions.SendMessage(s.ToString());

                    _data.Answerer.Sum += _data.CurPriceRight;
                    _data.ChooserIndex = _data.AnswererIndex;
                    _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex);
                    _gameActions.InformSums();

                    _data.IsQuestionPlaying = false;
                    _data.AnnounceAnswer = false;

                    _data.IsThinking = false;
                    _gameActions.SendMessageWithArgs(Messages.Timer, 1, "STOP");

                    if (!ClientData.Settings.AppSettings.FalseStart && !ClientData.IsQuestionFinished)
                    {
                        Engine.MoveToAnswer();
                    }

                    ScheduleExecution(Tasks.MoveNext, 1, force: true);
                }
                else
                {
                    _gameActions.ShowmanReplic(s.ToString());

                    if (isAnswerCanonical)
                    {
                        _data.AnnounceAnswer = false;
                    }

                    _data.PlayerIsRight = true;
                    ScheduleExecution(Tasks.AnnounceStake, 15);
                }
            }
            else
            {
                var s = new StringBuilder();
                if (_data.Answerer.Answer != LO[nameof(R.IDontKnow)])
                {
                    s.Append(GetRandomString(LO[nameof(R.Wrong)]));
                }

                if (_data.Settings.AppSettings.IgnoreWrong)
                {
                    _data.CurPriceWrong = 0;
                }

                if (!IsFinalRound())
                {
                    s.AppendFormat(" (-{0})", Notion.FormatNumber(_data.CurPriceWrong));
                    _gameActions.ShowmanReplic(s.ToString());

                    s = new StringBuilder(Messages.Person)
                        .Append(Message.ArgsSeparatorChar)
                        .Append('-')
                        .Append(Message.ArgsSeparatorChar)
                        .Append(_data.AnswererIndex)
                        .Append(Message.ArgsSeparatorChar)
                        .Append(_data.CurPriceWrong);

                    _gameActions.SendMessage(s.ToString());

                    _data.Answerer.Sum -= _data.CurPriceWrong;
                    _data.Answerer.CanPress = false;
                    _gameActions.InformSums();

                    if (_data.Answerer.IsHuman)
                    {
                        _data.GameResultInfo.WrongVersions.Add(new AnswerInfo
                        {
                            Round = Engine.RoundIndex,
                            Theme = _data.ThemeIndex,
                            Question = _data.QuestionIndex,
                            Answer = _data.Answerer.Answer
                        });
                    }

                    ScheduleExecution(Tasks.ContinueQuestion, 1);
                }
                else
                {
                    _gameActions.ShowmanReplic(s.ToString());
                    _data.PlayerIsRight = false;

                    ScheduleExecution(Tasks.AnnounceStake, 15);
                }
            }

            return true;
        }

        /// <summary>
        /// Продолжить отыгрыш вопроса
        /// </summary>
        public void ContinueQuestion()
        {
            if (IsSpecialQuestion())
            {
                ScheduleExecution(Tasks.WaitTry, 20);
                return;
            }

            var canAnybodyPress = _data.Players.Any(player => player.CanPress);

            if (!canAnybodyPress)
            {
                ScheduleExecution(Tasks.WaitTry, 20, force: true);
                return;
            }

            if (!ClientData.Settings.AppSettings.FalseStart)
            {
                _gameActions.SendMessageWithArgs(Messages.Try, MessageParams.Try_NotFinished);
            }

            if (ClientData.Settings.AppSettings.FalseStart || ClientData.IsQuestionFinished)
            {
                if (!ClientData.Settings.AppSettings.FalseStart)
                {
                    _gameActions.SendMessage(Messages.Resume); // Для возобновления мультимедиа
                }

                ScheduleExecution(Tasks.AskToTry, 10, force: true);
                return;
            }

            // Возобновить чтение вопроса
            if (_data.IsPartial && _data.AtomType == AtomTypes.Text)
            {
                ScheduleExecution(Tasks.PrintPartial, 5, force: true);
            }
            else
            {
                _data.IsPlayingMedia = _data.IsPlayingMediaPaused;
                _gameActions.SendMessage(Messages.Resume);
                ScheduleExecution(Tasks.MoveNext, _data.AtomTime, force: true);
            }

            SendTryToPlayers();

            _data.Decision = DecisionType.Pressing;
        }

        private bool IsSpecialQuestion()
        {
            var questTypeName = _data.Question.Type.Name;
            return questTypeName == QuestionTypes.Cat
                || questTypeName == QuestionTypes.BagCat
                || questTypeName == QuestionTypes.Auction
                || questTypeName == QuestionTypes.Sponsored;
        }

        private bool OnDecisionNextPersonStakeMaking()
        {
            var playerIndex = _data.Order[_data.OrderIndex];
            if (playerIndex == -1)
            {
                return false;
            }

            if (playerIndex >= _data.Players.Count)
            {
                throw new ArgumentException($"{nameof(playerIndex)} {playerIndex} must be in [0;{_data.Players.Count - 1}]");
            }

            StopWaiting();

            var s = $"{LO[nameof(R.StakeMakes)]} {_data.Players[playerIndex].Name}";
            _gameActions.ShowmanReplic(s);

            _data.OrderIndex--;
            ScheduleExecution(Tasks.AskStake, 10);
            return true;
        }

        private bool OnDecisionStarterChoosing()
        {
            if (_data.ChooserIndex == -1)
            {
                return false;
            }

            StopWaiting();

            var msg = string.Format(GetRandomString(LO[nameof(R.InformChooser)]), _data.Chooser.Name);
            _gameActions.ShowmanReplic(msg);

            _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex);

            var activeQuestionsCount = GetRoundActiveQuestionsCount();

            if (activeQuestionsCount == 0)
            {
                throw new Exception($"{nameof(activeQuestionsCount)} == 0! {((SIEngine.TvEngine)Engine).LeftQuestionsCount}");
            }

            ScheduleExecution(Tasks.AskToChoose, 20);
            RunRoundTimer();

            return true;
        }

        private void RunRoundTimer()
        {
            _data.TimerStartTime[0] = DateTime.UtcNow;
            _gameActions.SendMessageWithArgs(Messages.Timer, 0, MessageParams.Timer_Go, _data.Settings.AppSettings.TimeSettings.TimeOfRound * 10);
        }

        private bool OnDecisionAnswering()
        {
            if (!IsFinalRound())
            {
                if (_data.Answerer == null || string.IsNullOrEmpty(_data.Answerer.Answer))
                {
                    return false;
                }

                StopWaiting();

                _gameActions.PlayerReplic(_data.AnswererIndex, _data.Answerer.Answer);

                if (_data.IsOralNow)
                {
                    AskRight();
                }
                else
                {
                    ScheduleExecution(Tasks.AskRight, 15, force: true);
                }

                return true;
            }

            StopWaiting();

            var s = GetRandomString(LO[nameof(R.LetsSee)]);
            _gameActions.ShowmanReplic(s);

            _data.ThemeDeleters.Reset(false);
            ScheduleExecution(Tasks.Announce, 15);

            return true;
        }

        public bool IsFinalRound() => _data.Round?.Type == RoundTypes.Final;

        public void StopWaiting()
        {
            _data.IsWaiting = false;
            _data.Decision = DecisionType.None;

            _gameActions.SendMessageWithArgs(Messages.Timer, 2, "STOP");
        }

        private readonly Queue<string> _tasksHistory = new Queue<string>();

        public bool IsRunning { get; set; }

        internal void ScheduleExecution(Tasks task, double taskTime, int arg = 0, bool force = false)
        {
            SetTask((int)task, arg);
            if (_data.Settings.AppSettings.Managed && !force && _data.AllPersons.ContainsKey(_data.HostName))
            {
                IsRunning = false;
                return;
            }

            IsRunning = true;
            RunTaskTimer(taskTime);
        }

        internal string PrintOldTasks() => string.Join("|", OldTasks.Select(t => $"{(Tasks)t.Item1}:{t.Item2}"));

        /// <summary>
        /// Выполнение текущей задачи
        /// </summary>
        override protected void ExecuteTask(int taskId, int arg)
        {
            var task = (Tasks)taskId;

            lock (ClientData.TaskLock)
            {
                try
                {
                    if (Engine == null) // disposed
                        return;

                    if (_stopReason != StopReason.None)
                    {
                        var stop = true;

                        var stopReasonDetails = _stopReason == StopReason.Move ? _data.MoveDirection.ToString()
                            : (_stopReason == StopReason.Decision ? ClientData.Decision.ToString() : "");

                        _tasksHistory.Enqueue($"StopReason {_stopReason} {stopReasonDetails}");

                        // Прекратить обычное выполнение
                        switch (_stopReason)
                        {
                            case StopReason.Pause:
                                AddHistory($"Pause PauseExecution {task} {arg} {PrintOldTasks()}");
                                PauseExecution((int)task, arg);
                                break;

                            case StopReason.Decision:
                                stop = OnDecision();
                                break;

                            case StopReason.Answer:
                                ScheduleExecution(Tasks.AskAnswer, 7, force: true);
                                break;

                            case StopReason.Appellation:
                                if (task == Tasks.WaitChoose)
                                {
                                    task = Tasks.AskToChoose;
                                }

                                AddHistory($"Appellation PauseExecution {task} {arg} {PrintOldTasks()}");
                                PauseExecution((int)task, arg);
                                ScheduleExecution(Tasks.PrintAppellation, 10);
                                break;

                            case StopReason.Move:
                                switch (_data.MoveDirection)
                                {
                                    case -2:
                                        if (Engine.CanMoveBackRound)
                                        {
                                            FinishRound(false);
                                            stop = Engine.MoveBackRound();
                                            if (stop)
                                            {
                                                _gameActions.SpecialReplic(LO[nameof(R.ShowmanSwitchedToPreviousRound)]);
                                            }
                                            else
                                            {
                                                _stopReason = StopReason.None;
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            stop = false;
                                        }

                                        break;

                                    case -1:
                                        if (Engine.CanMoveBack)
                                        {
                                            Engine.MoveBack();
                                        }
                                        else
                                        {
                                            stop = false;
                                        }
                                        break;

                                    case 1:
                                        // Просто выполняем текущую задачу, дополнительной обработки делать не надо
                                        stop = false;
                                        break;

                                    case 2:
                                        if (Engine.CanMoveNextRound)
                                        {
                                            FinishRound(false);
                                            stop = Engine.MoveNextRound();
                                            if (stop)
                                            {
                                                _gameActions.SpecialReplic(LO[nameof(R.ShowmanSwitchedToNextRound)]);
                                            }
                                            else
                                            {
                                                _stopReason = StopReason.None;
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            stop = false;
                                        }
                                        break;

                                    default:
                                        stop = false;
                                        break;
                                }

                                if (stop)
                                {
                                    ScheduleExecution(Tasks.MoveNext, 10);
                                }

                                break;

                            case StopReason.Wait:
                                ScheduleExecution(Tasks.AskAnswerDeferred, _data.Penalty, force: true);
                                break;
                        }

                        _stopReason = StopReason.None;
                        if (stop)
                        {
                            return;
                        }
                    }

                    AddHistory(task.ToString());

                    var msg = new StringBuilder();
                    switch (task)
                    {
                        case Tasks.MoveNext:
                            MoveNext();
                            break;

                        case Tasks.StartGame:
                            StartGame(arg);
                            break;

                        case Tasks.Package:
                            ProcessPackage(_data.Package, arg);
                            break;

                        case Tasks.Round:
                            ProcessRound(_data.Round, arg);
                            break;

                        case Tasks.AskFirst:
                            AskFirst();
                            break;

                        case Tasks.WaitFirst:
                            WaitFirst();
                            break;

                        case Tasks.AskToChoose:
                            AskToChoose();
                            break;

                        case Tasks.WaitChoose:
                            WaitChoose();
                            break;

                        case Tasks.Theme:
                            ProcessTheme(_data.Theme, arg);
                            break;

                        case Tasks.QuestionType:
                            OnQuestionType(arg);
                            break;

                        case Tasks.PrintAuct:
                            PrintStakeQuestion();
                            break;

                        case Tasks.AskStake:
                            AskStake(true);
                            break;

                        case Tasks.WaitNext:
                        case Tasks.WaitNextToDelete:
                            WaitNext(task);
                            break;

                        case Tasks.WaitStake:
                            WaitStake();
                            break;

                        case Tasks.PrintAuctPlayer:
                            PrintAuctPlayer();
                            break;

                        case Tasks.PrintCat:
                            PrintCat(arg);
                            break;

                        case Tasks.AskCat:
                            AskCat();
                            break;

                        case Tasks.WaitCat:
                            WaitCat();
                            break;

                        case Tasks.CatInfo:
                            CatInfo();
                            break;

                        case Tasks.AskCatCost:
                            AskCatCost();
                            break;

                        case Tasks.WaitCatCost:
                            WaitCatCost();
                            break;

                        case Tasks.PrintSponsored:
                            PrintSponsored(arg);
                            break;

                        case Tasks.PrintQue:
                            PrintQue();
                            break;

                        case Tasks.PrintPartial:
                            PrintPartial();
                            break;

                        case Tasks.AskToTry:
                            AskToTry();
                            break;

                        case Tasks.WaitTry:
                            WaitTry();
                            break;

                        case Tasks.AskAnswer:
                            AskAnswer();
                            break;

                        case Tasks.AskAnswerDeferred:
                            AskAnswerDeferred();
                            break;

                        case Tasks.WaitAnswer:
                            WaitAnswer();
                            break;

                        case Tasks.AskRight:
                            AskRight();
                            break;

                        case Tasks.WaitRight:
                            #region WaitRight

                            _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);

                            if (_data.Answerer == null)
                            {
                                ScheduleExecution(Tasks.MoveNext, 10);
                                break;
                            }

                            var isRight = false;

                            foreach (var s in _data.Question.GetRightAnswers())
                            {
                                isRight = AnswerChecker.IsAnswerRight(_data.Answerer.Answer, s);
                                if (isRight)
                                {
                                    break;
                                }
                            }

                            _data.Answerer.AnswerIsRight = isRight;

                            _data.ShowmanDecision = true;
                            OnDecision();

                            #endregion
                            break;

                        case Tasks.ContinueQuestion:
                            ContinueQuestion();
                            break;

                        case Tasks.QuestSourComm:
                            QuestionSourcesAndComments(task, arg);
                            break;

                        case Tasks.PrintAppellation:
                            PrintAppellation();
                            break;

                        case Tasks.WaitAppellationDecision:
                            WaitAppellationDecision();
                            break;

                        case Tasks.CheckAppellation:
                            CheckAppellation();
                            break;

                        case Tasks.PrintFinal:
                            PrintFinal(arg);
                            break;

                        case Tasks.AskToDelete:
                            AskToDelete();
                            break;

                        case Tasks.WaitDelete:
                            WaitDelete();
                            break;

                        case Tasks.AskFinalStake:
                            AskFinalStake();
                            break;

                        case Tasks.WaitFinalStake:
                            WaitFinalStake();
                            break;

                        case Tasks.Announce:
                            Announce();
                            break;

                        case Tasks.AnnounceStake:
                            AnnounceStake();
                            break;

                        case Tasks.WaitReport:
                            WaitReport();
                            break;

                        case Tasks.Winner:
                            Winner();
                            break;

                        case Tasks.GoodLuck:
                            #region GoodLuck

                            _gameActions.ShowmanReplic(LO[nameof(R.GoodLuck)]);

                            _data.Stage = GameStage.After;
                            _actor.OnStageChanged(GameStages.Finished, LO[nameof(R.StageFinished)]);
                            _gameActions.InformStage();

                            _data.ReportsCount = _data.Players.Count;
                            ScheduleExecution(Tasks.WaitReport, 10 * 60 * 5); // 5 минут
                            WaitFor(DecisionType.Reporting, 10 * 60 * 5, -3);

                            _data.AcceptedReports = 0;

                            var reportString = _data.GameResultInfo.ToString(_data.PackageDoc, LO);

                            foreach (var item in _data.Players)
                            {
                                _gameActions.SendMessage(Messages.Report + Message.ArgsSeparatorChar + reportString, item.Name);
                            }

                            #endregion
                            break;

                        case Tasks.AutoGame:
                            AutoGame();
                            break;
                    }
                }
                catch (Exception exc)
                {
                    _data.BackLink.SendError(new Exception($"Task: {task}, param: {arg}, history: {string.Join(", ", _tasksHistory)}", exc));
                    ScheduleExecution(Tasks.NoTask, 10);
                    ClientData.MoveNextBlocked = true;
                    _gameActions.SpecialReplic("Game ERROR");
                }
            }
        }

        private void WaitCatCost()
        {
            if (_data.AnswererIndex == -1)
            {
                throw new ArgumentException($"{nameof(_data.AnswererIndex)} == -1", nameof(_data.AnswererIndex));
            }

            _gameActions.SendMessage(Messages.Cancel, _data.Answerer.Name);
            if (_data.IsOralNow)
            {
                _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);
            }

            _data.CurPriceRight = _data.CatInfo.Minimum;
            _data.CurPriceWrong = _data.CatInfo.Minimum;

            OnDecision();
        }

        private void WaitAppellationDecision()
        {
            foreach (var item in _data.Players)
            {
                _gameActions.SendMessage(Messages.Cancel, item.Name);
            }

            _data.AppellationAnswersReceivedCount = _data.Players.Count - 1;
            OnDecision();
        }

        private void MoveNext()
        {
            Engine?.MoveNext();
            ClientData.MoveNextBlocked = false;
        }

        private void PrintAuctPlayer()
        {
            if (_data.StakerIndex == -1)
            {
                throw new ArgumentException($"{nameof(PrintAuctPlayer)}: {nameof(_data.StakerIndex)} == -1 {_data.OrderHistory}", nameof(_data.StakerIndex));
            }

            _data.ChooserIndex = _data.StakerIndex;
            _data.AnswererIndex = _data.StakerIndex;
            _data.CurPriceRight = _data.Stake;
            _data.CurPriceWrong = _data.Stake;

            _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex, "+");

            var msg = new StringBuilder()
                .AppendFormat("{0} {1} {2} {3}", Notion.RandomString(LO[nameof(R.NowPlays)]), _data.Players[_data.StakerIndex].Name, LO[nameof(R.With)], Notion.FormatNumber(_data.Stake));
            _gameActions.ShowmanReplic(msg.ToString());

            ScheduleExecution(Tasks.PrintQue, 15 + ClientData.Rand.Next(10));
        }

        private void WaitNext(Tasks task)
        {
            _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);
            _gameActions.SendMessageWithArgs(Messages.Timer, 2, "STOP");

            var playerIndex = task == Tasks.WaitNext ? _data.Order[_data.OrderIndex] : _data.ThemeDeleters.Current.PlayerIndex;

            if (playerIndex == -1) // Ведущий не принял решения
            {
                var candidates = _data.Players.Where(p => p.Flag).ToArray();
                var index = ClientData.Rand.Next(candidates.Length);
                var newPlayerIndex = _data.Players.IndexOf(candidates[index]);
                if (task == Tasks.WaitNext)
                {
                    _data.Order[_data.OrderIndex] = newPlayerIndex;
                    CheckOrder(_data.OrderIndex);
                }
                else
                {
                    _data.ThemeDeleters.Current.SetIndex(newPlayerIndex);
                }
            }

            OnDecision();
        }

        private void WaitStake()
        {
            if (_data.OrderIndex == -1)
            {
                throw new ArgumentException($"{nameof(_data.OrderIndex)} == -1: {_data.OrderHistory}");
            }

            var playerIndex = _data.Order[_data.OrderIndex];

            if (playerIndex < 0 || playerIndex >= _data.Players.Count)
            {
                throw new ArgumentException($"{nameof(playerIndex)} {playerIndex} must be in [0; {_data.Players.Count - 1}]: {_data.OrderHistory}");
            }

            _gameActions.SendMessage(Messages.Cancel, _data.Players[playerIndex].Name);
            if (_data.IsOralNow)
            {
                _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);
            }

            _gameActions.SendMessageWithArgs(Messages.Timer, 2, "STOP");
            _data.StakeType = _data.StakeVariants[0] ? StakeMode.Nominal : StakeMode.Pass;

            OnDecision();
        }

        private void AskCatCost()
        {
            if (_data.AnswererIndex == -1)
            {
                throw new ArgumentException($"{nameof(_data.AnswererIndex)} == -1", nameof(_data.AnswererIndex));
            }

            if (_data.CurPriceRight == -1)
            {
                _gameActions.ShowmanReplic($"{_data.Answerer.Name}, {LO[nameof(R.PleaseChooseCatCost)]}");
                var s = string.Join(Message.ArgsSeparator, Messages.CatCost, _data.CatInfo.Minimum, _data.CatInfo.Maximum, _data.CatInfo.Step);

                var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForShowmanDecisions * 10;

                _data.IsOralNow = _data.IsOral && _data.Answerer.IsHuman;
                if (_data.IsOralNow)
                {
                    _gameActions.SendMessage(s, _data.ShowMan.Name);
                }
                else
                {
                    _gameActions.SendMessage(s, _data.Answerer.Name);
                    if (!_data.Answerer.IsConnected)
                    {
                        waitTime = 20;
                    }
                }

                ScheduleExecution(Tasks.WaitCatCost, waitTime);
                WaitFor(DecisionType.CatCostSetting, waitTime, _data.AnswererIndex);
            }
            else if (_data.Type[QuestionTypeParams.BagCat_Knows] == QuestionTypeParams.BagCat_Knows_Value_Never)
            {
                _gameActions.ShowmanReplic(LO[nameof(R.EasyCat)]);
                _gameActions.SendMessageWithArgs(Messages.Person, '+', _data.AnswererIndex, _data.CurPriceRight);

                _data.Answerer.Sum += _data.CurPriceRight;
                _data.ChooserIndex = _data.AnswererIndex;
                _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex);
                _gameActions.InformSums();

                Engine.SkipQuestion();
                ScheduleExecution(Tasks.MoveNext, 20, 1);
            }
            else
            {
                _data.CurPriceWrong = _data.CurPriceRight;
                _gameActions.SendMessageWithArgs(Messages.PersonStake, _data.AnswererIndex, 1, _data.CurPriceRight);
                _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex, "+");
                ScheduleExecution(Tasks.PrintQue, 2);
            }
        }

        private void WaitFirst()
        {
            _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);

            if (_data.ChooserIndex == -1)
            {
                _data.ChooserIndex = SelectRandom(_data.Players, p => p.Flag);
            }

            OnDecision();
        }

        private void WaitAnswer()
        {
            if (_data.Round == null)
            {
                throw new ArgumentNullException(nameof(_data.Round));
            }

            if (!IsFinalRound())
            {
                if (_data.Answerer == null)
                {
                    ScheduleExecution(Tasks.MoveNext, 10);
                    return;
                }

                _gameActions.SendMessage(Messages.Cancel, _data.Answerer.Name);

                _data.Answerer.Answer = LO[nameof(R.IDontKnow)];
                _data.Answerer.AnswerIsWrong = true;
            }
            else
            {
                for (var i = 0; i < _data.Players.Count; i++)
                {
                    if (_data.Players[i].InGame && string.IsNullOrEmpty(_data.Players[i].Answer))
                    {
                        _data.Players[i].Answer = LO[nameof(R.IDontKnow)];
                        _data.Players[i].AnswerIsWrong = true;

                        _gameActions.SendMessage(Messages.Cancel, _data.Players[i].Name);
                    }
                }

                _data.IsWaiting = true;
            }

            OnDecision();
        }

        private void WaitCat()
        {
            _gameActions.SendMessage(Messages.Cancel, _data.Chooser.Name);
            if (_data.IsOralNow)
                _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);

            _data.AnswererIndex = SelectRandomOnIndex(_data.Players, index => index != _data.ChooserIndex);

            OnDecision();
        }

        private void PrintQue()
        {
            if (!IsFinalRound())
            {
                var isSpecial = IsSpecialQuestion();

                foreach (var player in _data.Players)
                {
                    player.CanPress = !isSpecial;
                }

                _data.IsDeferringAnswer = false;
                _data.IsQuestionFinished = false;

                if (!_data.Settings.AppSettings.FalseStart)
                {
                    var type = _data.Question.Type.Name;
                    if (type == QuestionTypes.Simple)
                    {
                        _data.Decision = DecisionType.Pressing;
                        _gameActions.SendMessageWithArgs(Messages.Try, MessageParams.Try_NotFinished);

                        SendTryToPlayers();
                    }
                }
            }

            _data.IsAnswer = false;

            ScheduleExecution(Tasks.MoveNext, 1, 1, true);
        }

        private void WaitTry()
        {
            _data.IsThinking = false;
            _data.Decision = DecisionType.None;

            if (!IsSpecialQuestion())
            {
                _gameActions.SendMessageWithArgs(Messages.EndTry, MessageParams.EndTry_All); // Timer 1 STOP
            }

            _data.AnnounceAnswer = true;
            ScheduleExecution(Tasks.MoveNext, 1, force: true);

            _data.IsQuestionPlaying = false;
        }

        private void WaitFinalStake()
        {
            _gameActions.SendMessageWithArgs(Messages.Timer, 2, "STOP");

            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.Players[i].InGame && _data.Players[i].FinalStake == -1)
                {
                    _gameActions.SendMessage(Messages.Cancel, _data.Players[i].Name);
                    _data.Players[i].FinalStake = 1;

                    _gameActions.SendMessageWithArgs(Messages.PersonFinalStake, i);
                }
            }

            _data.NumOfStakers = 0;
            OnDecision();
        }

        private void WaitReport()
        {
            foreach (var item in _data.Players)
            {
                _gameActions.SendMessage(Messages.Cancel, item.Name);
            }

            if (_data.AcceptedReports > 0)
            {
                _data.AcceptedReports = 0;
                _data.BackLink.SaveReport(_data.GameResultInfo);
            }

            StopWaiting();
        }

        private void AnnounceStake()
        {
            if (_data.AnswererIndex == -1)
            {
                throw new ArgumentException($"{nameof(_data.AnswererIndex)} == -1", nameof(_data.AnswererIndex));
            }

            var msg = new StringBuilder();
            msg.AppendFormat("{0}: {1}", LO[nameof(R.Stake)], Notion.FormatNumber(_data.Answerer.FinalStake));
            _gameActions.ShowmanReplic(msg.ToString());

            msg = new StringBuilder(Messages.Person).Append(Message.ArgsSeparatorChar);
            if (_data.PlayerIsRight)
            {
                msg.Append('+');
                _data.Answerer.Sum += _data.Answerer.FinalStake;
            }
            else
            {
                msg.Append('-');
                _data.Answerer.Sum -= _data.Answerer.FinalStake;
            }

            msg.Append(Message.ArgsSeparatorChar).Append(_data.AnswererIndex);
            msg.Append(Message.ArgsSeparatorChar).Append(_data.Answerer.FinalStake);
            _gameActions.SendMessage(msg.ToString());
            _gameActions.InformSums();

            _gameActions.SendMessageWithArgs(Messages.PersonStake, _data.AnswererIndex, 1, _data.Answerer.FinalStake);

            ScheduleExecution(Tasks.Announce, 15);
        }

        private void AskFinalStake()
        {
            var s = GetRandomString(LO[nameof(R.MakeStake)]);
            _gameActions.ShowmanReplic(s);

            _data.NumOfStakers = 0;
            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.Players[i].InGame)
                {
                    _data.Players[i].FinalStake = -1;
                    _data.NumOfStakers++;
                    _gameActions.SendMessage(Messages.FinalStake, _data.Players[i].Name);
                }
            }

            var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForMakingStake * 10;
            ScheduleExecution(Tasks.WaitFinalStake, waitTime);
            WaitFor(DecisionType.FinalStakeMaking, waitTime, -2);
        }

        private void WaitDelete()
        {
            _gameActions.SendMessage(Messages.Cancel, _data.ActivePlayer.Name);
            if (_data.IsOralNow)
                _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);

            _gameActions.SendMessageWithArgs(Messages.Timer, 2, "STOP");

            _data.ThemeIndex = SelectRandom(_data.TInfo.RoundInfo, item => item.Name != null);

            OnDecision();
        }

        private void PrintFinal(int arg)
        {
            if (arg == 1)
            {
                var s = new StringBuilder(Messages.FinalRound);
                var playFinal = false;
                for (var i = 0; i < _data.Players.Count; i++)
                {
                    if (_data.Players[i].Sum <= 0)
                    {
                        _data.Players[i].InGame = false;
                        s.Append("\n-");
                    }
                    else
                    {
                        playFinal = true;
                        _data.Players[i].InGame = true;
                        s.Append("\n+");
                    }
                }

                _gameActions.SendMessage(s.ToString());
                if (!playFinal)
                {
                    _gameActions.ShowmanReplic(LO[nameof(R.NobodyInFinal)]);
                    if (Engine.MoveNextRound())
                    {
                        ScheduleExecution(Tasks.MoveNext, 15 + ClientData.Rand.Next(10), 1);
                    }

                    return;
                }

                _data.AnnounceAnswer = true; // инициализация
                ScheduleExecution(Tasks.PrintFinal, 1, 2, true);
            }
            else if (arg < 2 + _data.Players.Count)
            {
                if (_data.Players[arg - 2].InGame)
                {
                    ScheduleExecution(Tasks.PrintFinal, 1, arg + 1, true);
                }
                else
                {
                    _gameActions.ShowmanReplic(_data.Players[arg - 2].Name + " " + LO[nameof(R.NotInFinal)]);
                    ScheduleExecution(Tasks.PrintFinal, 15, arg + 1);
                }
            }
            else
            {
                _gameActions.InformStage(name: _data.Round.Name);
                _gameActions.ShowmanReplic($"{GetRandomString(LO[nameof(R.WeBeginRound)])} {_data.Round.Name}!");

                ScheduleExecution(Tasks.Round, 10, 2);
            }
        }

        private void Winner()
        {
            var big = _data.Players.Max(player => player.Sum);
            var winnersCount = _data.Players.Count(player => player.Sum == big);

            if (winnersCount == 1)
            {
                for (var i = 0; i < _data.Players.Count; i++)
                {
                    if (_data.Players[i].Sum == big)
                    {
                        var s = new StringBuilder(_data.Players[i].Name).Append(", ");
                        s.Append(GetRandomString(LO[nameof(R.YouWin)]));
                        _gameActions.ShowmanReplic(s.ToString());
                        _gameActions.SendMessageWithArgs(Messages.Winner, i);
                        break;
                    }
                }
            }
            else
            {
                _gameActions.ShowmanReplic(LO[nameof(R.NoWinner)]);
                _gameActions.SendMessageWithArgs(Messages.Winner, -1);
            }

            ScheduleExecution(Tasks.GoodLuck, 20 + ClientData.Rand.Next(10));
        }

        private void PrintSponsored(int arg)
        {
            var adShown = false;

            if (arg == 1)
            {
                _data.CurPriceRight *= 2;
                _data.CurPriceWrong = 0;
                _data.AnswererIndex = _data.ChooserIndex;
                _gameActions.ShowmanReplic(LO[nameof(R.SponsoredQuestion)]);
            }
            else if (arg == 2)
            {
                // Покажем рекламу
                var ad = ClientData.BackLink.GetAd(LO.Culture.TwoLetterISOLanguageName, out int adId);
                if (!string.IsNullOrEmpty(ad))
                {
                    var res = new StringBuilder(LO[nameof(R.Ads)] + ": ").Append(ad);

                    _gameActions.ShowmanReplic(res.ToString());
                    _gameActions.SpecialReplic(res.ToString());

                    _gameActions.SendMessageWithArgs(Messages.Ads, ad);
#if !DEBUG
                    ClientData.MoveNextBlocked = true;
#endif
                    _actor.OnAdShown(adId);
                    adShown = true;
                }
                else
                    arg++;
            }

            if (arg < 3)
            {
                ScheduleExecution(Tasks.PrintSponsored, adShown ? 60 : 10, arg + 1);
            }
            else
            {
                ClientData.MoveNextBlocked = false;
                _gameActions.ShowmanReplic(_data.Chooser.Name + ", " + string.Format(LO[nameof(R.SponsoredQuestionInfo)], Notion.FormatNumber(_data.CurPriceRight)));
                _gameActions.SendMessageWithArgs(Messages.PersonStake, _data.AnswererIndex, 1, _data.CurPriceRight, _data.CurPriceWrong);
                _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex, "+");
                ScheduleExecution(Tasks.PrintQue, 10);
            }
        }

        internal void AddHistory(string message)
        {
            if (_tasksHistory.Count > 500)
            {
                _tasksHistory.Dequeue();
            }

            _tasksHistory.Enqueue(message);
        }

        private void CatInfo()
        {
            var isCat = _data.Type.Name == QuestionTypes.Cat;
            var isBagCat = _data.Type.Name == QuestionTypes.BagCat;

            var taskTime = 5;

            if (isBagCat)
            {
                var knows = _data.Type[QuestionTypeParams.BagCat_Knows];
                if (knows != QuestionTypeParams.BagCat_Knows_Value_Before)
                {
                    isCat = true;
                }
            }

            if (isCat)
            {
                PrintSecretQuestionInfo();
                taskTime += 25;
            }

            if (isBagCat)
            {
                ScheduleExecution(Tasks.AskCatCost, taskTime);
            }
            else
            {
                _gameActions.SendMessageWithArgs(Messages.PersonStake, _data.AnswererIndex, 1, _data.CurPriceRight);
                ScheduleExecution(Tasks.PrintQue, taskTime);
            }
        }

        private void PrintStakeQuestion()
        {
            _gameActions.ShowmanReplic(GetRandomString(LO[nameof(R.YouGetAuction)]));

            var nominal = _data.Question.Price;

            _data.Order = new int[_data.Players.Count];
            for (var i = 0; i < _data.Players.Count; i++)
            {
                _data.Players[i].StakeMaking = i == _data.ChooserIndex || _data.Players[i].Sum > nominal;
                _data.Order[i] = -1;
            }

            _data.Stake = _data.StakerIndex = -1;
            _data.Order[0] = _data.ChooserIndex;

            _data.OrderHistory = new StringBuilder("Stake making. Initial state. ")
                .Append("Sums: ")
                .Append(string.Join(",", _data.Players.Select(p => p.Sum)))
                .Append(" Order: ")
                .Append(string.Join(",", _data.Order))
                .Append(" Nominal: ")
                .Append(_data.CurPriceRight)
                .AppendLine();

            _data.AllIn = false;
            _data.OrderIndex = -1;
            ScheduleExecution(Tasks.AskStake, 10);
        }

        private void AskToTry()
        {
            if (_data.Settings.AppSettings.FalseStart)
            {
                _gameActions.SendMessage(Messages.Try);
            }

            SendTryToPlayers();

            var maxTime = _data.Settings.AppSettings.TimeSettings.TimeForThinkingOnQuestion * 10;

            _data.TimerStartTime[1] = DateTime.UtcNow;
            _data.IsThinking = true;
            _gameActions.SendMessageWithArgs(Messages.Timer, 1, "RESUME");
            _data.Decision = DecisionType.Pressing;

            ScheduleExecution(Tasks.WaitTry, Math.Max(maxTime - _data.TimeThinking, 10), force: true);
        }

        private void SendTryToPlayers()
        {
            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.Players[i].CanPress)
                {
                    _gameActions.SendMessage(Messages.YouTry, _data.Players[i].Name);
                }
            }
        }

        private void WaitChoose()
        {
            _gameActions.SendMessage(Messages.Cancel, _data.Chooser.Name);
            if (_data.IsOralNow)
            {
                _gameActions.SendMessage(Messages.Cancel, _data.ShowMan.Name);
            }

            var canChooseTheme = _data.TInfo.RoundInfo.Select(t => t.Questions.Any(QuestionHelper.IsActive)).ToArray();
            var numberOfThemes = canChooseTheme.Where(can => can).Count();

            if (numberOfThemes == 0)
            {
                throw new Exception($"numberOfThemes == 0! GetRoundActiveQuestionsCount: {GetRoundActiveQuestionsCount()}");
            }

            // Случайный выбор номера темы
            var k1 = ClientData.Rand.Next(numberOfThemes);
            var i = -1;

            do if (canChooseTheme[++i]) k1--; while (k1 >= 0);

            var theme = _data.TInfo.RoundInfo[i];
            var numberOfQuestions = theme.Questions.Count(QuestionHelper.IsActive);

            // Случайный выбор номера вопроса
            var k2 = ClientData.Rand.Next(numberOfQuestions);
            var j = -1;
            do if (theme.Questions[++j].IsActive()) k2--; while (k2 >= 0);

            lock (_data.ChoiceLock)
            {
                _data.ThemeIndex = i;
                _data.QuestionIndex = j;
            }

            OnDecision();
        }

        private void OnQuestionType(int arg)
        {
            if (arg == 1)
            {
                if (_data.Question == null)
                    throw new Exception(string.Format(LO[nameof(R.StrangeError)] + " {0} {1}", _data.Round.Type, _data.Settings.AppSettings.GameMode));

                var authors = _data.PackageDoc.GetRealAuthors(_data.Question.Info.Authors);
                if (authors.Length > 0)
                {
                    var res = new StringBuilder();
                    res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PAuthors)], LO[nameof(R.OfQuestion)], string.Join(", ", authors));
                    _gameActions.ShowmanReplic(res.ToString());
                }
                else
                    arg++;
            }

            if (arg == 2)
            {
                if (IsFinalRound())
                {
                    ScheduleExecution(Tasks.PrintQue, 1, force: true);
                    return;
                }

                _data.Type = _data.Question.Type;
                if (_data.Settings.AppSettings.GameMode == SIEngine.GameModes.Sport)
                {
                    var themeChanged = false;
                    string catTheme = null;

                    var isCat = _data.Type.Name == QuestionTypes.Cat || _data.Type.Name == QuestionTypes.BagCat;
                    if (isCat)
                    {
                        var currentTheme = _data.Theme.Name;
                        catTheme = _data.Type[QuestionTypeParams.Cat_Theme];

                        themeChanged = currentTheme != catTheme;
                    }

                    _data.Type.Name = QuestionTypes.Simple;

                    if (themeChanged)
                    {
                        // Нужно сказать, что тема будет другой
                        _gameActions.ShowmanReplic(LO[nameof(R.QuestionWithSpecialTheme)] + ": " + catTheme);
                        ScheduleExecution(Tasks.PrintQue, 20, force: true);
                        return;
                    }
                }

                var s = new StringBuilder(Messages.QType).Append(Message.ArgsSeparatorChar);
                if (_data.Type.Name == QuestionTypes.Auction || _data.Type.Name == QuestionTypes.Cat || _data.Type.Name == QuestionTypes.BagCat || _data.Type.Name == QuestionTypes.Sponsored)
                    s.Append(_data.Type.Name);
                else
                    s.Append(QuestionTypes.Simple);

                _gameActions.SendMessage(s.ToString());

                // Реакция согласно типу вопроса
                if (_data.Type.Name == QuestionTypes.Auction)
                {
                    ScheduleExecution(Tasks.PrintAuct, 8 + ClientData.Rand.Next(10), force: true);
                }
                else if (_data.Type.Name == QuestionTypes.Cat || _data.Type.Name == QuestionTypes.BagCat)
                {
                    ScheduleExecution(Tasks.PrintCat, 8 + ClientData.Rand.Next(10), 1, force: true);
                }
                else if (_data.Type.Name == QuestionTypes.Sponsored)
                {
                    // Вопрос без риска
                    ScheduleExecution(Tasks.PrintSponsored, 8 + ClientData.Rand.Next(10), 1, force: true);
                }
                else if (_data.Type.Name == QuestionTypes.Simple)
                {
                    ScheduleExecution(Tasks.PrintQue, 1, force: true);
                }
                else // Неподдерживаемый игрой тип
                {
                    var sp = new StringBuilder(LO[nameof(R.UnknownType)]).Append(' ');
                    sp.Append(_data.Type.Name);
                    if (_data.Type.Params.Count > 0)
                    {
                        sp.Append(' ').Append(LO[nameof(R.WithParams)]);
                        foreach (var p in _data.Type.Params)
                            sp.Append(' ').Append(p);
                    }

                    _gameActions.SpecialReplic(sp.ToString());
                    _gameActions.SpecialReplic(LO[nameof(R.GameWillResume)]);

                    _gameActions.ShowmanReplic(LO[nameof(R.ManuallyPlayedQuestion)]);

                    Engine.SkipQuestion();
                    ScheduleExecution(Tasks.MoveNext, 150, 1);
                }
                return;
            }

            ScheduleExecution(Tasks.QuestionType, 20, arg + 1);
        }

        private void PrintPartial()
        {
            var text = _data.Text;

            var printingLength = Math.Min(text.Length - _data.TextLength, _data.Settings.AppSettings.ReadingSpeed / 2); // Число символов в секунду
            var subText = text.Substring(_data.TextLength, printingLength);

            _gameActions.SendMessageWithArgs(Messages.Atom, Constants.PartialText, subText);
            _gameActions.SystemReplic(subText);

            _data.TextLength += printingLength;
            if (_data.TextLength < text.Length)
            {
                ScheduleExecution(Tasks.PrintPartial, 5, force: true);
            }
            else
            {
                ScheduleExecution(Tasks.MoveNext, 10, force: true);
                _data.TimeThinking = 0.0;
            }
        }

        private void QuestionSourcesAndComments(Tasks task, int arg)
        {
            if (arg == 1)
            {
                var sources = _data.PackageDoc.GetRealSources(_data.Question.Info.Sources);
                if (sources.Length > 0)
                {
                    var res = new StringBuilder();
                    res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PSources)], LO[nameof(R.OfQuestion)], string.Join(", ", sources));
                    _gameActions.ShowmanReplic(res.ToString());
                }
                else
                {
                    arg++;
                }
            }

            if (arg == 2)
            {
                if (_data.Question.Info.Comments.Text.Length > 0)
                {
                    var res = new StringBuilder();
                    res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PComments)], LO[nameof(R.OfQuestion)], _data.Question.Info.Comments.Text);
                    _gameActions.ShowmanReplic(res.ToString());
                }
                else
                {
                    arg++;
                }
            }

            if (arg < 3)
            {
                ScheduleExecution(task, 20, arg + 1);
            }
            else
            {
                ScheduleExecution(Tasks.MoveNext, 1);
            }
        }

        /// <summary>
        /// Начать игру даже при отсутствии участников (заполнив пустые слоты ботами)
        /// </summary>
        internal void AutoGame()
        {
            // Заполняем пустые слоты ботами
            for (int i = 0; i < _data.Players.Count; i++)
            {
                if (!_data.Players[i].IsConnected)
                {
                    _actor.ChangePersonType(Constants.Player, i.ToString(), null);
                }
            }

            _actor.StartGame();
        }

        internal void Announce()
        {
            if (!_data.ThemeDeleters.MoveNext())
            {
                ScheduleExecution(Tasks.MoveNext, 15, 1, true);
                return;
            }

            var currentDeleter = _data.ThemeDeleters.Current;

            if (currentDeleter == null)
            {
                throw new Exception("currentDeleter == null: " + _data.ThemeDeleters.GetRemoveLog());
            }

            if (currentDeleter.PlayerIndex == -1)
            {
                if (currentDeleter.PossibleIndicies.Count == 0)
                {
                    throw new Exception("currentDeleter.PossibleIndicies.Count == 0: " + _data.ThemeDeleters.GetRemoveLog());
                }

                currentDeleter.SetIndex(currentDeleter.PossibleIndicies.First());
            }

            var playerIndex = currentDeleter.PlayerIndex;
            if (playerIndex < -1 || playerIndex >= _data.Players.Count)
            {
                throw new ArgumentException($"{nameof(playerIndex)}: {_data.ThemeDeleters.GetRemoveLog()}");
            }

            _data.AnswererIndex = playerIndex;

            if (_data.Answerer == null)
            {
                throw new Exception("_data.Answerer == null");
            }

            var msg = new StringBuilder()
                .Append(LO[nameof(R.Answer)]).Append(' ')
                .Append(_data.Answerer.Name).Append(": ")
                .Append(_data.Answerer.Answer);

            _gameActions.ShowmanReplic(msg.ToString());

            ScheduleExecution(Tasks.AskRight, 20, force: true);
        }

        internal void AskAnswerDeferred()
        {
            if (!ClientData.Settings.AppSettings.FalseStart)
            {
                // Прервать чтение вопроса
                if (!ClientData.IsQuestionFinished)
                {
                    var timeDiff = (int)DateTime.UtcNow.Subtract(ClientData.AtomStart).TotalSeconds * 10;
                    ClientData.AtomTime = Math.Max(1, ClientData.AtomTime - timeDiff);
                }
            }

            if (_data.IsThinking)
            {
                var startTime = _data.TimerStartTime[1];
                var currentTime = DateTime.UtcNow;
                ClientData.TimeThinking += currentTime.Subtract(startTime).TotalMilliseconds / 100;
            }

            ClientData.Decision = DecisionType.None;
            ClientData.Answerer.CanPress = false;
            _data.IsThinking = false;
            _gameActions.SendMessageWithArgs(Messages.Timer, 1, "PAUSE", (int)ClientData.TimeThinking);

            _data.IsDeferringAnswer = false;
            _data.IsPlayingMediaPaused = _data.IsPlayingMedia;
            _data.IsPlayingMedia = false;

            ClientData.Decision = DecisionType.None;
            Stop(StopReason.Answer);
        }

        private void StartGame(int arg)
        {
            var nextArg = arg + 1;
            var extraTime = 0;
            switch (arg)
            {
                case 1:
                    _gameActions.ShowmanReplic(LO[nameof(R.ShowmanGreeting)]);
                    var settings = ClientData.Settings.AppSettings;
                    nextArg = !settings.FalseStart || settings.Oral || settings.GameMode == SIEngine.GameModes.Sport || settings.IgnoreWrong || settings.Managed ? 2 : -1;
                    break;

                case 2:
                    _gameActions.ShowmanReplic(LO[nameof(R.GameRules)] + ": " + BuildRulesString(ClientData.Settings.AppSettings));
                    nextArg = -1;
                    extraTime = 20;
                    break;

                default:
                    _gameActions.SpecialReplic(LO[nameof(R.WrongGameState)] + " - " + Tasks.StartGame);
                    break;
            }

            if (nextArg != -1)
                ScheduleExecution(Tasks.StartGame, 10 + extraTime, nextArg);
            else
                ScheduleExecution(Tasks.MoveNext, 10 + extraTime, 0);
        }

        private string BuildRulesString(AppSettingsCore settings)
        {
            var sb = new StringBuilder();

            if (settings.GameMode == SIEngine.GameModes.Sport)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append(LO[nameof(R.TypeSport)]);
            }

            if (!settings.FalseStart)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append(LO[nameof(R.TypeNoFalseStart)]);
            }

            if (settings.Oral)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append(LO[nameof(R.TypeOral)]);
            }

            if (settings.IgnoreWrong)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append(LO[nameof(R.TypeIgnoreWrong)]);
            }

            if (settings.Managed)
            {
                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append(LO[nameof(R.TypeManaged)]);
            }

            return sb.ToString();
        }

        private void AskToChoose()
        {
            _data.IsQuestionPlaying = true;

            _gameActions.InformSums();
            _gameActions.AnnounceSums();
            _gameActions.SendMessage(Messages.ShowTable);

            if (_data.Chooser == null)
            {
                throw new Exception("_data.Chooser == null");
            }

            if (_gameActions.Client.CurrentServer == null)
            {
                throw new Exception("_actor.Client.CurrentServer == null");
            }

            var msg = new StringBuilder(_data.Chooser.Name).Append(", ");
            var activeQuestionsCount = GetRoundActiveQuestionsCount();

            if (activeQuestionsCount == 0)
            {
                throw new Exception($"activeQuestionsCount == 0 {Engine.Stage} {((SIEngine.TvEngine)Engine).LeftQuestionsCount}");
            }

            msg.Append(GetRandomString(LO[activeQuestionsCount > 1 ? nameof(R.ChooseQuest) : nameof(R.LastQuest)]));

            _gameActions.ShowmanReplic(msg.ToString());

            lock (_data.ChoiceLock)
            {
                _data.PrevoiusTheme = _data.ThemeIndex;
                _data.PreviousQuest = _data.QuestionIndex;

                _data.ThemeIndex = -1;
                _data.QuestionIndex = -1;
            }

            _data.UsedWrongVersions.Clear();

            int time;
            if (activeQuestionsCount > 1)
            {
                time = _data.Settings.AppSettings.TimeSettings.TimeForChoosingQuestion * 10;

                var message = $"{Messages.Choose}{Message.ArgsSeparatorChar}1";
                _data.IsOralNow = _data.IsOral && _data.Chooser.IsHuman;

                if (_data.IsOralNow)
                    _gameActions.SendMessage(message, _data.ShowMan.Name);
                else if (!_data.Chooser.IsConnected)
                    time = 20;

                _gameActions.SendMessage(message, _data.Chooser.Name);
            }
            else
            {
                time = 20;
            }

            ScheduleExecution(Tasks.WaitChoose, time);
            WaitFor(DecisionType.QuestionChoosing, time, _data.ChooserIndex);
        }

        private void AskCat()
        {
            var msg = new StringBuilder(Messages.Cat);
            for (var i = 0; i < _data.Players.Count; i++)
                msg.Append(Message.ArgsSeparatorChar).Append(_data.Players[i].Flag ? '+' : '-');

            _data.AnswererIndex = -1;

            var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForGivingACat * 10;

            _data.IsOralNow = _data.IsOral && _data.Chooser.IsHuman;
            if (_data.IsOralNow)
                _gameActions.SendMessage(msg.ToString(), _data.ShowMan.Name);
            else if (!_data.Chooser.IsConnected)
                waitTime = 20;
            
            _gameActions.SendMessage(msg.ToString(), _data.Chooser.Name);

            ScheduleExecution(Tasks.WaitCat, waitTime);
            WaitFor(DecisionType.CatGiving, waitTime, _data.ChooserIndex);
        }

        private void PrintCat(int arg)
        {
            if (arg == 1)
            {
                var s = new StringBuilder(LO[nameof(R.YouReceiveCat)]);
                if (_data.Type[QuestionTypeParams.BagCat_Self] == QuestionTypeParams.BagCat_Self_Value_True)
                {
                    s.Append($". {LO[nameof(R.YouCanKeepCat)]}");
                }

                _gameActions.ShowmanReplic(s.ToString());
                if (_data.Type.Name == QuestionTypes.Cat)
                {
                    arg++;
                }
                else
                {
                    arg++;
                    if (_data.Type[QuestionTypeParams.BagCat_Knows] == QuestionTypeParams.BagCat_Knows_Value_Before)
                    {
                        arg++;
                    }
                }

                ScheduleExecution(Tasks.PrintCat, 15 + ClientData.Rand.Next(10), arg);
            }
            else if (arg == 2)
            {
                // Если имеется один вариант, спрашивать не надо
                for (var i = 0; i < _data.Players.Count; i++)
                {
                    _data.Players[i].Flag = true;
                }

                if (_data.Type[QuestionTypeParams.BagCat_Self] != QuestionTypeParams.BagCat_Self_Value_True)
                {
                    _data.Chooser.Flag = false;
                }

                var variantsCount = _data.Players.Count(player => player.Flag);
                if (variantsCount == 1)
                {
                    for (var i = 0; i < _data.Players.Count; i++)
                    {
                        if (_data.Players[i].Flag)
                        {
                            _data.ChooserIndex = _data.AnswererIndex = i;
                            _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex);
                        }
                    }

                    _gameActions.ShowmanReplic($"{_data.Answerer.Name}, {LO[nameof(R.CatIsYours)]}!");
                    ScheduleExecution(Tasks.CatInfo, 10);
                }
                else
                {
                    _gameActions.ShowmanReplic($"{_data.Chooser.Name}, {LO[nameof(R.GiveCat)]}");
                    ScheduleExecution(Tasks.AskCat, 10 + ClientData.Rand.Next(10), force: true);
                }
            }
            else if (arg == 3)
            {
                PrintSecretQuestionInfo();
                ScheduleExecution(Tasks.PrintCat, 20 + ClientData.Rand.Next(10), 2);
            }
        }

        private void AskFirst()
        {
            var min = _data.Players.Min(player => player.Sum);
            var total = 0;

            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (_data.Players[i].Sum == min)
                {
                    _data.Players[i].Flag = true;
                    total++;
                }
                else
                {
                    _data.Players[i].Flag = false;
                }
            }

            if (total == 1)
            {
                for (var i = 0; i < _data.Players.Count; i++)
                {
                    if (_data.Players[i].Flag)
                    {
                        _data.ChooserIndex = i;
                        break;
                    }
                }

                _data.IsWaiting = true;
                _data.Decision = DecisionType.StarterChoosing;
                OnDecision();
            }
            else
            {
                _data.ChooserIndex = -1;

                var msg = new StringBuilder(Messages.First);
                for (var i = 0; i < _data.Players.Count; i++)
                {
                    msg.Append(Message.ArgsSeparatorChar).Append(_data.Players[i].Flag ? '+' : '-');
                }

                _gameActions.SendMessage(msg.ToString(), _data.ShowMan.Name);

                var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForShowmanDecisions * 10;
                ScheduleExecution(Tasks.WaitFirst, waitTime);
                WaitFor(DecisionType.StarterChoosing, waitTime, -1);
            }
        }

        private void AskRight()
        {
            _data.ShowmanDecision = false;

            if (!_data.Answerer.IsHuman)
            {
                _data.IsWaiting = true;
                _data.Decision = DecisionType.AnswerValidating;

                _data.Answerer.AnswerIsRight = !_data.Answerer.AnswerIsWrong;
                _data.ShowmanDecision = true;

                OnDecision();
            }
            else
            {
                if (!_data.IsOralNow || IsFinalRound())
                {
                    SendAnswersInfoToShowman(_data.Answerer.Answer);
                }

                var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForShowmanDecisions * 10;
                ScheduleExecution(Tasks.WaitRight, waitTime);
                WaitFor(DecisionType.AnswerValidating, waitTime, -1);
            }
        }

        private void SendAnswersInfoToShowman(string answer)
        {
            var msg = new StringBuilder();
            msg.Append(Messages.IsRight).Append(Message.ArgsSeparatorChar).Append(answer);
            foreach (var right in _data.Question.GetRightAnswers())
            {
                msg.Append(Message.ArgsSeparatorChar).Append(right);
            }

            _gameActions.SendMessage(msg.ToString(), _data.ShowMan.Name);

            msg = new StringBuilder(Messages.Wrong);
            foreach (var wrong in _data.Question.Wrong)
            {
                msg.Append(Message.ArgsSeparatorChar).Append(wrong);
            }

            _gameActions.SendMessage(msg.ToString(), _data.ShowMan.Name);

            _gameActions.SendMessage(BuildValidationMessage(_data.Answerer.Name, answer), _data.ShowMan.Name);
            _gameActions.SendMessage(BuildValidationMessageOld(_data.Answerer.Name, answer), _data.ShowMan.Name);
        }

        private string BuildValidationMessage(string name, string answer, bool isCheckingForTheRight = true)
        {
            var rightAnswers = _data.Question.Right;
            var wrongAnswers = _data.Question.Wrong;

            return new MessageBuilder(Messages.Validation, name, answer, isCheckingForTheRight ? '+' : '-', rightAnswers.Count)
                .AddRange(rightAnswers)
                .AddRange(wrongAnswers)
                .Build();
        }

        [Obsolete]
        private string BuildValidationMessageOld(string name, string answer, bool isCheckingForTheRight = true)
        {
            var rightAnswers = _data.Question.GetRightAnswers();
            var wrongAnswers = _data.Question.Wrong;

            return new MessageBuilder("VALIDATIION", name, answer, isCheckingForTheRight ? '+' : '-', rightAnswers.Count)
                .AddRange(rightAnswers)
                .AddRange(wrongAnswers)
                .Build();
        }

        private void AskAnswer()
        {
            var timeSettings = _data.Settings.AppSettings.TimeSettings;
            var msg = new StringBuilder();

            if (IsFinalRound())
            {
                _gameActions.ShowmanReplic(LO[nameof(R.StartThink)]);
                for (var i = 0; i < _data.Players.Count; i++)
                {
                    if (_data.Players[i].InGame)
                    {
                        _data.Players[i].Answer = "";
                        _gameActions.SendMessage(Messages.Answer, _data.Players[i].Name);
                    }
                }

                ScheduleExecution(Tasks.WaitAnswer, timeSettings.TimeForFinalThinking * 10, force: true);
                WaitFor(DecisionType.Answering, timeSettings.TimeForFinalThinking * 10, -2, false);
                return;
            }

            if (_data.Answerer == null)
            {
                ScheduleExecution(Tasks.MoveNext, 10);
                return;
            }

            if (_data.Question.Type.Name == QuestionTypes.Simple)
            {
                _gameActions.SendMessageWithArgs(Messages.EndTry, _data.AnswererIndex);
            }

            msg.Append(Messages.Answer);
            var time1 = _data.Question.Type.Name != QuestionTypes.Simple
                ? timeSettings.TimeForThinkingOnSpecial * 10
                : timeSettings.TimeForPrintingAnswer * 10;

            _data.IsOralNow = _data.IsOral && _data.Answerer.IsHuman;

            if (_data.IsOralNow)
            {
                // Ведущий принимает ответ устно
                SendAnswersInfoToShowman($"({LO[nameof(R.AnswerIsOral)]})");
            }
            else
            {
                _gameActions.SendMessage(msg.ToString(), _data.Answerer.Name);
            }

            _gameActions.ShowmanReplic(_data.Answerer.Name + GetRandomString(LO[nameof(R.YourAnswer)]));

            _data.Answerer.Answer = "";

            ScheduleExecution(Tasks.WaitAnswer, time1);
            WaitFor(DecisionType.Answering, time1, _data.AnswererIndex);
        }

        private void AskToDelete()
        {
            int playerIndex = -1;
            try
            {
                _data.ThemeDeleters.MoveNext();
                var currentDeleter = _data.ThemeDeleters.Current;

                if (currentDeleter.PlayerIndex == -1)
                {
                    var indicies = currentDeleter.PossibleIndicies;

                    if (indicies.Count > 1)
                    {
                        RequestForCurrentDeleter(indicies);
                        return;
                    }
                    else if (indicies.Count == 0)
                    {
                        throw new Exception("indicies.Count == 0: " + _data.ThemeDeleters.GetRemoveLog());
                    }

                    currentDeleter.SetIndex(indicies.First());
                }

                playerIndex = currentDeleter.PlayerIndex;

                if (playerIndex < -1 || playerIndex >= _data.Players.Count)
                {
                    throw new ArgumentException($"{nameof(playerIndex)}: {_data.ThemeDeleters.GetRemoveLog()}");
                }

                _data.ActivePlayer = _data.Players[playerIndex];

                RequestForThemeDelete();

            }
            catch (Exception exc)
            {
                _data.BackLink.SendError(new Exception(string.Format("AskToDelete {0}/{1}/{2}", _data.ThemeDeleters.Current.PlayerIndex, playerIndex, _data.Players.Count), exc));
            }
        }

        private void RequestForThemeDelete()
        {
            var msg = new StringBuilder(_data.ActivePlayer.Name)
                .Append(", ")
                .Append(GetRandomString(LO[nameof(R.DeleteTheme)]));

            _gameActions.ShowmanReplic(msg.ToString());

            var message = string.Join(Message.ArgsSeparator, Messages.Choose, 2);
            _data.IsOralNow = _data.IsOral && _data.ActivePlayer.IsHuman;

            var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForChoosingFinalTheme * 10;
            if (_data.IsOralNow)
            {
                _gameActions.SendMessage(message, _data.ShowMan.Name);
            }
            else if (!_data.ActivePlayer.IsConnected)
            {
                waitTime = 20;
            }

            _gameActions.SendMessage(message, _data.ActivePlayer.Name);

            _data.ThemeIndex = -1;
            ScheduleExecution(Tasks.WaitDelete, waitTime);
            WaitFor(DecisionType.FinalThemeDeleting, waitTime, _data.Players.IndexOf(_data.ActivePlayer));
        }

        private void RequestForCurrentDeleter(ICollection<int> indicies)
        {
            var msg = new StringBuilder(Messages.FirstDelete);
            for (var i = 0; i < _data.Players.Count; i++)
            {
                var good = _data.Players[i].Flag = indicies.Contains(i);
                msg.Append(Message.ArgsSeparatorChar).Append(good ? '+' : '-');
            }

            _gameActions.SendMessage(msg.ToString(), _data.ShowMan.Name);

            var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForShowmanDecisions * 10;
            ScheduleExecution(Tasks.WaitNextToDelete, waitTime);
            WaitFor(DecisionType.NextPersonFinalThemeDeleting, waitTime, -1);
        }

        /// <summary>
        /// Определить следующего ставящего
        /// </summary>
        /// <returns>Стоит ли продолжать выполнение</returns>
        private bool DetectNextStaker()
        {
            var errorLog = new StringBuilder()
                .Append(' ').Append(_data.Stake).Append(' ').Append(_data.OrderIndex)
                .Append(' ').Append(string.Join(":", _data.Order))
                .Append(' ').Append(string.Join(":", _data.Players.Select(p => p.Sum)))
                .Append(' ').Append(string.Join(":", _data.Players.Select(p => p.StakeMaking)))
                .Append(' ').Append(string.Join(":", _data.OrderHistory));

            var stage = 0;

            try
            {
                var candidatesAll = Enumerable.Range(0, _data.Order.Length).Except(_data.Order).ToArray(); // Незадействованные игроки
                if (_data.OrderIndex < _data.Order.Length - 1)
                {
                    // Ещё есть, из кого выбирать

                    // Сначала отбросим тех, у кого недостаточно денег для ставки
                    var candidates = candidatesAll.Where(n => _data.Players[n].StakeMaking);

                    if (candidates.Count() > 1)
                    {
                        // У кандидатов должна быть минимальная сумма
                        var minSum = candidates.Min(n => _data.Players[n].Sum);
                        candidates = candidates.Where(n => _data.Players[n].Sum == minSum);
                    }

                    if (!candidates.Any()) // Никто из оставшихся не может перебить ставку
                    {
                        stage = 1;

                        var ind = _data.OrderIndex;
                        for (var i = 0; i < candidatesAll.Length; i++)
                        {
                            _data.Order[ind + i] = candidatesAll[i];
                            CheckOrder(ind + i);
                            _data.Players[candidatesAll[i]].StakeMaking = false;
                        }

                        stage = 2;

                        var stakersCount = _data.Players.Count(p => p.StakeMaking);
                        if (stakersCount == 1)
                        {
                            // Игрок определён
                            for (var i = 0; i < _data.Players.Count; i++)
                            {
                                if (_data.Players[i].StakeMaking)
                                {
                                    _data.StakerIndex = i;
                                    break;
                                }
                            }

                            _data.ChooserIndex = _data.StakerIndex;
                            _data.AnswererIndex = _data.StakerIndex;
                            _data.CurPriceRight = _data.Stake;
                            _data.CurPriceWrong = _data.Stake;

                            if (_data.AnswererIndex == -1)
                            {
                                _data.BackLink.SendError(new Exception("this.data.AnswererIndex == -1"), true);
                            }

                            _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex, "+");

                            ScheduleExecution(Tasks.PrintQue, 10, 1);
                            return false;
                        }

                        _data.OrderIndex = -1;
                        AskStake(false);
                        return false;
                    }

                    stage = 3;

                    _data.IsWaiting = false;
                    if (candidates.Count() == 1)
                    {
                        _data.Order[_data.OrderIndex] = candidates.First();
                        CheckOrder(_data.OrderIndex);
                    }
                    else
                    {
                        // Без ведущего не обойтись
                        var msg = new StringBuilder(Messages.FirstStake);
                        for (var i = 0; i < _data.Players.Count; i++)
                        {
                            _data.Players[i].Flag = candidates.Contains(i);
                            msg.Append(Message.ArgsSeparatorChar).Append(_data.Players[i].Flag ? '+' : '-');
                        }

                        _gameActions.SendMessage(msg.ToString(), _data.ShowMan.Name);

                        var time = _data.Settings.AppSettings.TimeSettings.TimeForShowmanDecisions * 10;
                        ScheduleExecution(Tasks.WaitNext, time);
                        WaitFor(DecisionType.NextPersonStakeMaking, time, -1);
                        return false;
                    }
                }
                else
                {
                    stage = 4;

                    // Остался последний игрок, выбор очевиден
                    var leftIndex = candidatesAll[0];
                    _data.Order[_data.OrderIndex] = leftIndex;
                    CheckOrder(_data.OrderIndex);
                }

                return true;
            }
            catch (Exception exc)
            {
                errorLog.Append(' ').Append(stage);
                throw new Exception(errorLog.ToString(), exc);
            }
        }

        public void CheckOrder(int index)
        {
            if (index < 0 || index >= _data.Order.Length)
            {
                throw new ArgumentException($"Value {index} must be in [0; {_data.Order.Length}]", nameof(index));
            }

            var checkedValue = _data.Order[index];

            if (checkedValue == -1)
            {
                throw new Exception("_data.Order[index] == -1");
            }

            for (var i = 0; i < _data.Order.Length; i++)
            {
                var value = _data.Order[i];
                if (value == -1 || i == index)
                {
                    continue;
                }

                if (checkedValue == value)
                {
                    throw new Exception($"_data.Order contains at least two occurences of {checkedValue}!");
                }
            }
        }

        private void AskStake(bool canDetectNextStakerGuard)
        {
            int tempStage = 0; // to catch error
            var cost = _data.Question.Price;

            try
            {
                _data.OrderHistory
                    .Append($"AskStake: Order = {string.Join(",", _data.Order)};")
                    .Append($" OrderIndex = {_data.OrderIndex};")
                    .Append($" StakeMaking = {string.Join(",", _data.Players.Select(p => p.StakeMaking))}")
                    .AppendLine();

                IncrementOrderIndex();

                tempStage = 1;

                if (_data.Order[_data.OrderIndex] == -1) // Необходимо определить следующего ставящего
                {
                    if (!canDetectNextStakerGuard)
                    {
                        throw new Exception("!canDetectNextStaker");
                    }

                    if (!DetectNextStaker())
                    {
                        return;
                    }

                    _data.OrderHistory.Append($"NextStaker = {_data.Order[_data.OrderIndex]}").AppendLine();
                }

                var others = _data.Players.Where((p, index) => index != _data.Order[_data.OrderIndex]); // Те, кто сейчас не делают ставку
                if (others.All(p => !p.StakeMaking) && _data.StakerIndex > -1) // Остальные не могут ставить
                {
                    // Нельзя повысить ставку
                    ScheduleExecution(Tasks.PrintAuctPlayer, 10);
                    return;
                }

                tempStage = 2;

                var playerIndex = _data.Order[_data.OrderIndex];

                if (playerIndex < 0 || playerIndex >= _data.Players.Count)
                {
                    throw new ArgumentException($"Bad {nameof(playerIndex)} value {playerIndex}! It must be in [0; {_data.Players.Count - 1}]");
                }

                var activePlayer = _data.Players[playerIndex];
                var playerMoney = activePlayer.Sum;
                if (_data.Stake != -1 && playerMoney <= _data.Stake) // Не может сделать ставку
                {
                    activePlayer.StakeMaking = false;
                    _gameActions.SendMessageWithArgs(Messages.PersonStake, playerIndex, 2);

                    var stakersCount = _data.Players.Count(p => p.StakeMaking);

                    if (stakersCount == 1) // Игрок определился
                    {
                        for (var i = 0; i < _data.Players.Count; i++)
                        {
                            if (_data.Players[i].StakeMaking)
                            {
                                _data.StakerIndex = i;
                            }
                        }

                        ScheduleExecution(Tasks.PrintAuctPlayer, 10);
                        return;
                    }

                    ScheduleExecution(Tasks.AskStake, 5);
                    return;
                }

                tempStage = 3;

                // Теперь определим возможные ставки

                // Только номинал
                if (_data.Stake == -1 && (playerMoney < cost || playerMoney == cost && others.All(p => playerMoney >= p.Sum)))
                {
                    var s = new StringBuilder(activePlayer.Name)
                        .Append(", ").Append(LO[nameof(R.YouCanSayOnly)])
                        .Append(' ').Append(LO[nameof(R.Nominal)]);

                    _gameActions.ShowmanReplic(s.ToString());

                    _data.StakerIndex = playerIndex;
                    _data.Stake = cost;
                    _gameActions.SendMessageWithArgs(Messages.PersonStake, playerIndex, 1, cost);
                    ScheduleExecution(Tasks.AskStake, 5);
                    return;
                }

                // TODO: enum StakeMode, StakeVariants -> HashSet<StakeMode> ?
                _data.StakeVariants[0] = _data.StakerIndex == -1;
                _data.StakeVariants[1] = !_data.AllIn && playerMoney != cost && playerMoney > _data.Stake + 100;
                _data.StakeVariants[2] = !_data.StakeVariants[0];
                _data.StakeVariants[3] = true;

                tempStage = 4;

                _data.ActivePlayer = activePlayer;

                var stakeMsg = new StringBuilder(_data.ActivePlayer.Name)
                    .Append(", ")
                    .Append(GetRandomString(LO[nameof(R.YourStake)]));

                _gameActions.ShowmanReplic(stakeMsg.ToString());

                _data.IsOralNow = _data.IsOral && _data.ActivePlayer.IsHuman;

                stakeMsg = new StringBuilder(Messages.Stake);

                for (var i = 0; i < _data.StakeVariants.Length; i++)
                {
                    stakeMsg.Append(Message.ArgsSeparatorChar).Append(_data.StakeVariants[i] ? '+' : '-');
                }

                var minimumStake = (_data.Stake != -1 ? _data.Stake : cost) + 100;
                var minimumStakeByBase = (int)Math.Ceiling((double)minimumStake / 100) * 100; // TODO: возможность настраивать кратность ставки

                tempStage = 5;

                stakeMsg.Append(Message.ArgsSeparatorChar).Append(minimumStakeByBase);

                var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForMakingStake * 10;
                if (_data.IsOralNow)
                {
                    _gameActions.SendMessage(stakeMsg.ToString(), _data.ActivePlayer.Name);
                    stakeMsg.Append(Message.ArgsSeparatorChar).Append(_data.ActivePlayer.Sum); // Ведущему укажем максимум
                    _gameActions.SendMessage(stakeMsg.ToString(), _data.ShowMan.Name);
                }
                else
                {
                    _gameActions.SendMessage(stakeMsg.ToString(), _data.ActivePlayer.Name);
                    if (!_data.ActivePlayer.IsConnected)
                    {
                        waitTime = 20;
                    }
                }

                _data.StakeType = null;
                _data.StakeSum = -1;
                ScheduleExecution(Tasks.WaitStake, waitTime);
                WaitFor(DecisionType.AuctionStakeMaking, waitTime, _data.Players.IndexOf(_data.ActivePlayer));
            }
            catch (Exception exc)
            {
                var orders = string.Join(",", _data.Order);
                var sums = string.Join(",", _data.Players.Select(p => p.Sum));
                var stakeMaking = string.Join(",", _data.Players.Select(p => p.StakeMaking));
                throw new Exception($"AskStake error {tempStage} {sums} {stakeMaking} {orders} {_data.Stake} {_data.OrderIndex} {_data.Players.Count} {_data.OrderHistory}", exc);
            }
        }

        private void IncrementOrderIndex()
        {
            var breakerGuard = 20; // Temp var

            var initialOrderIndex = _data.OrderIndex == -1 ? _data.Order.Length - 1 : _data.OrderIndex;

            // TODO: Rewrite as for
            do
            {
                _data.OrderIndex++;
                if (_data.OrderIndex == _data.Order.Length)
                {
                    _data.OrderIndex = 0;
                }

                breakerGuard--;

                if (breakerGuard == 0)
                {
                    throw new Exception($"{nameof(breakerGuard)} == {breakerGuard} ({initialOrderIndex})");
                }

            } while (_data.OrderIndex != initialOrderIndex && _data.Order[_data.OrderIndex] != -1 && !_data.Players[_data.Order[_data.OrderIndex]].StakeMaking);

            if (_data.OrderIndex == initialOrderIndex)
            {
                throw new Exception($"{nameof(_data.OrderIndex)} == {nameof(initialOrderIndex)} ({initialOrderIndex})");
            }
        }

        private void PrintAppellation()
        {
            if (_data.AppelaerIndex < 0 || _data.AppelaerIndex >= _data.Players.Count)
            {
                AddHistory($"PrintAppellation resumed ({PrintOldTasks()})");
                ResumeExecution(40);
                return;
            }

            var appelaer = _data.Players[_data.AppelaerIndex];
            var given = LO[appelaer.IsMale ? nameof(R.HeGave) : nameof(R.SheGave)];
            var apellationReplic = string.Format(LO[nameof(R.PleaseCheckApellation)], given);

            string origin = _data.IsAppelationForRightAnswer
                ? LO[nameof(R.IsApellating)]
                : string.Format(LO[nameof(R.IsConsideringWrong)], appelaer.Name);

            _gameActions.ShowmanReplic($"{_data.AppellationSource} {origin}. {apellationReplic}");

            var validationMessage = BuildValidationMessage(appelaer.Name, appelaer.Answer, _data.IsAppelationForRightAnswer);
            var validationMessageOld = BuildValidationMessageOld(appelaer.Name, appelaer.Answer, _data.IsAppelationForRightAnswer);

            for (var i = 0; i < _data.Players.Count; i++)
            {
                if (i == _data.AppelaerIndex)
                {
                    _data.Players[i].Flag = false;
                }
                else
                {
                    _data.Players[i].Flag = true;

                    var msg = new StringBuilder(Messages.IsRight).Append(Message.ArgsSeparatorChar).Append(appelaer.Answer);
                    foreach (var rightAnswer in _data.Question.GetRightAnswers())
                    {
                        msg.AppendFormat("\n{0}", rightAnswer);
                    }

                    _gameActions.SendMessage(msg.ToString(), _data.Players[i].Name);

                    msg = new StringBuilder(Messages.Wrong).Append(Message.ArgsSeparator).Append(_data.IsAppelationForRightAnswer ? "+" : "-");
                    foreach (var wrong in _data.Question.Wrong)
                    {
                        msg.AppendFormat("\n{0}", wrong);
                    }

                    _gameActions.SendMessage(msg.ToString(), _data.Players[i].Name);

                    _gameActions.SendMessage(validationMessage, _data.Players[i].Name);
                    _gameActions.SendMessage(validationMessageOld, _data.Players[i].Name);
                }
            }

            _data.AppellationAnswersReceivedCount = 0;
            _data.AppellationAnswersRightReceivedCount = 0;
            var waitTime = _data.Settings.AppSettings.TimeSettings.TimeForShowmanDecisions * 10;
            ScheduleExecution(Tasks.WaitAppellationDecision, waitTime);
            WaitFor(DecisionType.AppellationDecision, waitTime, -2);
        }

        private void CheckAppellation()
        {
            if (_data.AppelaerIndex < 0 || _data.AppelaerIndex >= _data.Players.Count)
            {
                AddHistory($"CheckAppellation resumed ({PrintOldTasks()})");
                ResumeExecution(40);
                return;
            }

            var appelaer = _data.Players[_data.AppelaerIndex];
            var rightAns = _data.AppellationAnswersRightReceivedCount;
            var dec = !_data.IsAppelationForRightAnswer;

            if (dec && rightAns > 0 || !dec && rightAns < _data.AppellationAnswersReceivedCount)
            {
                _gameActions.ShowmanReplic($"{LO[nameof(R.ApellationDenied)]}!");
            }
            else
            {
                // Осуществляем апелляцию
                _gameActions.ShowmanReplic($"{LO[nameof(R.ApellationAccepted)]}!");

                if (_data.IsAppelationForRightAnswer)
                {
                    int theme = 0, quest = 0;

                    lock (_data.ChoiceLock)
                    {
                        theme = _data.ThemeIndex > -1 ? _data.ThemeIndex : _data.PrevoiusTheme;
                        quest = _data.QuestionIndex > -1 ? _data.QuestionIndex : _data.PreviousQuest;
                    }

                    // Запишем в отчёт апеллированную версию
                    var answerInfo = _data.GameResultInfo.WrongVersions.FirstOrDefault(answer =>
                        answer.Round == Engine.RoundIndex && answer.Theme == theme
                        && answer.Question == quest && answer.Answer == appelaer.Answer);

                    if (!Equals(answerInfo, default(AnswerInfo)))
                    {
                        _data.GameResultInfo.WrongVersions.Remove(answerInfo);
                    }

                    _data.GameResultInfo.ApellatedQuestions.Add(new AnswerInfo
                    {
                        Round = Engine.RoundIndex,
                        Theme = theme,
                        Question = quest,
                        Answer = appelaer.Answer
                    });
                }

                // Изменим суммы
                var change = false;
                for (var i = 0; i < _data.QuestionHistory.Count; i++)
                {
                    var index = _data.QuestionHistory[i].PlayerIndex;
                    if (_data.IsAppelationForRightAnswer && _data.Stage == GameStage.Round && index != _data.AppelaerIndex)
                    {
                        if (!change)
                        {
                            continue;
                        }
                        else
                        {
                            if (_data.QuestionHistory[i].IsRight)
                            {
                                _data.Players[index].Sum -= _data.CurPriceRight;
                            }
                            else
                            {
                                _data.Players[index].Sum += _data.CurPriceWrong;
                            }
                        }
                    }
                    else if (index == _data.AppelaerIndex)
                    {
                        if (_data.Stage == GameStage.Round)
                        {
                            change = true;
                            if (_data.QuestionHistory[i].IsRight)
                            {
                                _data.Players[index].Sum -= _data.CurPriceRight + _data.CurPriceWrong;
                            }
                            else
                            {
                                _data.Players[index].Sum += _data.CurPriceWrong + _data.CurPriceRight;

                                if (Engine.CanMoveBack) // Не начало раунда
                                {
                                    _data.ChooserIndex = index;
                                    _gameActions.SendMessageWithArgs(Messages.SetChooser, ClientData.ChooserIndex);
                                }
                            }
                        }
                        else
                        {
                            if (_data.QuestionHistory[i].IsRight)
                            {
                                _data.Players[index].Sum -= _data.Players[index].FinalStake * 2;
                            }
                            else
                            {
                                _data.Players[index].Sum += _data.Players[index].FinalStake * 2;
                            }
                        }
                    }
                }

                _gameActions.InformSums();
                _gameActions.AnnounceSums();
            }

            AddHistory($"CheckAppellation resumed normally ({PrintOldTasks()})");
            ResumeExecution(40);
        }

        private void ProcessTheme(Theme theme, int arg)
        {
            var informed = false;

            if (arg == -1)
            {
                _gameActions.Print(_gameActions.Showman() + ReplicManager.OldReplic(LO[nameof(R.Theme)] + ": " + theme.Name));
                _gameActions.SendMessageWithArgs(Messages.Theme, theme.Name);
                arg++;
            }

            if (arg == 1)
            {
                var authors = _data.PackageDoc.GetRealAuthors(theme.Info.Authors);
                if (authors.Length > 0)
                {
                    informed = true;
                    var res = new StringBuilder();
                    res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PAuthors)], LO[nameof(R.OfTheme)], string.Join(", ", authors));
                    _gameActions.ShowmanReplic(res.ToString());
                }
                else
                    arg++;
            }

            if (arg == 2)
            {
                var sources = _data.PackageDoc.GetRealSources(theme.Info.Sources);
                if (sources.Length > 0)
                {
                    informed = true;
                    var res = new StringBuilder();
                    res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PSources)], LO[nameof(R.OfTheme)], string.Join(", ", sources));
                    _gameActions.ShowmanReplic(res.ToString());
                }
                else
                    arg++;
            }

            if (arg == 3)
            {
                if (theme.Info.Comments.Text.Length > 0)
                {
                    informed = true;
                    var res = new StringBuilder();
                    res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PComments)], LO[nameof(R.OfTheme)], theme.Info.Comments.Text);
                    _gameActions.ShowmanReplic(res.ToString());
                }
            }

            if (arg < 3)
            {
                ScheduleExecution(Tasks.Theme, 20, arg + 1);
            }
            else if (informed)
            {
                if (_data.Settings.AppSettings.GameMode == SIEngine.GameModes.Sport)
                {
                    ScheduleExecution(Tasks.MoveNext, 20);
                }
                else if (_data.Round.Type != RoundTypes.Final)
                {
                    ScheduleExecution(Tasks.QuestionType, 1, 1);
                }
                else
                {
                    ScheduleExecution(Tasks.AskFinalStake, 20);
                }
            }
            else
            {
                if (_data.Settings.AppSettings.GameMode == SIEngine.GameModes.Sport)
                {
                    ScheduleExecution(Tasks.MoveNext, 1, 0);
                }
                else if (_data.Round.Type != RoundTypes.Final)
                {
                    ScheduleExecution(Tasks.QuestionType, 1, 1);
                }
                else
                {
                    ScheduleExecution(Tasks.AskFinalStake, 1, 0);
                }
            }
        }

        private void WaitFor(DecisionType decision, int time, int person, bool isWaiting = true)
        {
            _data.TimerStartTime[2] = DateTime.UtcNow;

            _data.IsWaiting = isWaiting;
            _data.Decision = decision;

            _gameActions.SendMessageWithArgs(Messages.Timer, 2, MessageParams.Timer_Go, time, person);
        }

        private void ProcessPackage(Package package, int arg)
        {
            var informed = false;
            var isRandomPackage = package.Info.Comments.Text.StartsWith(PackageHelper.RandomIndicator);

            if (arg == 1)
            {
                if (!isRandomPackage)
                {
                    _gameActions.ShowmanReplic(string.Format(OfObjectPropertyFormat, LO[nameof(R.PName)], LO[nameof(R.OfPackage)], package.Name));

                    var msg = new StringBuilder(Messages.PackageLogo).Append(Message.ArgsSeparatorChar);

                    var packageLogo = _data.Package.Logo;
                    if (!string.IsNullOrEmpty(packageLogo) && packageLogo.Length > 0)
                    {
                        var uri = _data.Share.CreateURI(packageLogo.Substring(1), () => _data.PackageDoc.Images.GetFile(packageLogo.Substring(1)), SIDocument.ImagesStorageName);

                        foreach (var person in _data.AllPersons.Keys)
                        {
                            var msg2 = new StringBuilder(msg.ToString());
                            if (_gameActions.Client.CurrentServer.Contains(person))
                            {
                                msg2.Append(uri);
                            }
                            else
                            {
                                msg2.Append(uri.Replace("http://localhost", "http://" + Constants.GameHost));
                            }

                            _gameActions.SendMessage(msg2.ToString(), person);
                        }
                    }
                }
                else
                {
                    arg++;
                }
            }

            if (arg == 2)
            {
                var authors = _data.PackageDoc.GetRealAuthors(package.Info.Authors);
                if (!isRandomPackage && authors.Length > 0)
                {
                    informed = true;
                    var res = new StringBuilder();
                    res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PAuthors)], LO[nameof(R.OfPackage)], string.Join(", ", authors));
                    _gameActions.ShowmanReplic(res.ToString());
                }
                else
                {
                    arg++;
                }
            }

            if (arg == 3)
            {
                var sources = _data.PackageDoc.GetRealSources(package.Info.Sources);
                if (sources.Length > 0)
                {
                    informed = true;
                    var res = new StringBuilder();
                    res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PSources)], LO[nameof(R.OfPackage)], string.Join(", ", sources));
                    _gameActions.ShowmanReplic(res.ToString());
                }
                else
                {
                    arg++;
                }
            }

            if (arg == 4)
            {
                if (package.Info.Comments.Text.Length > 0 && !isRandomPackage)
                {
                    informed = true;
                    var res = new StringBuilder();
                    res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PComments)], LO[nameof(R.OfPackage)], package.Info.Comments.Text);
                    _gameActions.ShowmanReplic(res.ToString());
                }
                else
                {
                    arg++;
                }
            }

            if (arg == 5)
            {
                if (!string.IsNullOrWhiteSpace(package.Restriction))
                {
                    informed = true;
                    var res = new StringBuilder();
                    res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.Restrictions)], LO[nameof(R.OfPackage)], package.Restriction);
                    _gameActions.ShowmanReplic(res.ToString());
                }
                else
                {
                    arg++;
                }
            }

            if (arg == 6)
            {
                if (!string.IsNullOrWhiteSpace(package.Date) && !isRandomPackage)
                {
                    informed = true;
                    var res = new StringBuilder();
                    res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.CreationDate)], LO[nameof(R.OfPackage)], package.Date);
                    _gameActions.ShowmanReplic(res.ToString());
                }
                else
                {
                    arg++;
                }
            }

            if (arg < 6)
            {
                ScheduleExecution(Tasks.Package, 15, arg + 1);
            }
            else if (informed)
            {
                ScheduleExecution(Tasks.MoveNext, 10);
            }
            else
            {
                ScheduleExecution(Tasks.MoveNext, 1);
            }
        }

        private void ProcessRound(Round round, int arg)
        {
            var informed = false;
            var baseTime = arg == 1 ? 30 : 20;

            if (arg == 1)
            {
                _gameActions.InformSums();

                _data.TabloInformStage = 0;
                _data.IsRoundEnding = false;

                if (round.Type == RoundTypes.Final)
                {
                    _data.Stage = GameStage.Final;
                    _actor.OnStageChanged(GameStages.Final, LO[nameof(R.Final)]);
                    ScheduleExecution(Tasks.PrintFinal, 1, 1, true);
                    return;
                }
                else
                {
                    _data.Stage = GameStage.Round;
                    _actor.OnStageChanged(GameStages.Round, round.Name);
                }

                var isRandomPackage = _data.Package.Info.Comments.Text.StartsWith(PackageHelper.RandomIndicator);
                var skipRoundAnnounce = isRandomPackage && (_data.Settings.AppSettings.GameMode == SIEngine.GameModes.Sport
                    && _data.Package.Rounds.Count == 2); // второй раунд - всегда финал

                _gameActions.InformStage(name: skipRoundAnnounce ? "" : round.Name);

                if (!skipRoundAnnounce)
                {
                    _gameActions.ShowmanReplic($"{GetRandomString(LO[nameof(R.WeBeginRound)])} {round.Name}!");
                    _gameActions.SystemReplic(" "); // new line
                    _gameActions.SystemReplic(round.Name);
                }
                else
                {
                    baseTime = 1;
                }

                // Теперь случайные спецвопросы расставляются здесь
                if (_data.Settings.RandomSpecials)
                {
                    RandomizeSpecials(round);
                }
            }
            else if (arg == 2)
            {
                var authors = _data.PackageDoc.GetRealAuthors(round.Info.Authors);
                if (authors.Length > 0)
                {
                    informed = true;
                    var res = new StringBuilder();
                    res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PAuthors)], LO[nameof(R.OfRound)], string.Join(", ", authors));
                    _gameActions.ShowmanReplic(res.ToString());
                }
                else
                {
                    arg++;
                }
            }

            if (arg == 3)
            {
                var sources = _data.PackageDoc.GetRealSources(round.Info.Sources);
                if (sources.Length > 0)
                {
                    informed = true;
                    var res = new StringBuilder();
                    res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PSources)], LO[nameof(R.OfRound)], string.Join(", ", sources));
                    _gameActions.ShowmanReplic(res.ToString());
                }
                else
                {
                    arg++;
                }
            }

            if (arg == 4)
            {
                if (round.Info.Comments.Text.Length > 0)
                {
                    informed = true;
                    var res = new StringBuilder();
                    res.AppendFormat(OfObjectPropertyFormat, LO[nameof(R.PComments)], LO[nameof(R.OfRound)], _data.Package.Info.Comments.Text);
                    _gameActions.ShowmanReplic(res.ToString());
                }
                else
                {
                    arg++;
                }
            }

            var adShown = false;

            if (arg == 5)
            {
                // Покажем рекламу
                try
                {
                    var ad = ClientData.BackLink.GetAd(LO.Culture.TwoLetterISOLanguageName, out int adId);
                    if (!string.IsNullOrEmpty(ad))
                    {
                        informed = true;
                        var res = new StringBuilder(LO[nameof(R.Ads)]).Append(": ").Append(ad);

                        _gameActions.ShowmanReplic(res.ToString());
                        _gameActions.SpecialReplic(res.ToString());

                        _gameActions.SendMessageWithArgs(Messages.Ads, ad);

#if !DEBUG
                        ClientData.MoveNextBlocked = true;
#endif
                        adShown = true;

                        _actor.OnAdShown(adId);
                    }
                    else
                    {
                        arg++;
                    }
                }
                catch (Exception exc)
                {
                    _data.BackLink.SendError(exc);
                    arg++;
                }
            }

            if (arg < 5)
            {
                ScheduleExecution(Tasks.Round, baseTime + ClientData.Rand.Next(10), arg + 1);
            }
            else if (informed)
            {
                ScheduleExecution(Tasks.MoveNext, (adShown ? 60 : 20) + ClientData.Rand.Next(10));
            }
            else
            {
                ScheduleExecution(Tasks.MoveNext, 1);
            }
        }

        private void RandomizeSpecials(Round round)
        {
            var nonUsedNumbers = new List<int>();
            var maxQuestionsInTheme = round.Themes.Max(theme => theme.Questions.Count);
            var leavedSecrets = 1 + ClientData.Rand.Next(3);
            var leavedStakes = 1 + ClientData.Rand.Next(3);
            var leavedNoRisk = ClientData.Rand.Next(2);
            for (var ti = 0; ti < round.Themes.Count; ti++)
            {
                var theme = round.Themes[ti];
                for (var qi = 0; qi < theme.Questions.Count; qi++)
                {
                    var quest = theme.Questions[qi];
                    if (quest.Type.Name != QuestionTypes.Cat && quest.Type.Name != QuestionTypes.BagCat)
                    {
                        nonUsedNumbers.Add(ti * maxQuestionsInTheme + qi);
                        quest.Type.Name = QuestionTypes.Simple;
                    }
                    else
                    {
                        leavedSecrets--;
                    }
                }
            }

            while (nonUsedNumbers.Count > 0 && leavedNoRisk > 0)
            {
                var num = ClientData.Rand.Next(nonUsedNumbers.Count);
                var val = nonUsedNumbers[num];
                nonUsedNumbers.RemoveAt(num);
                leavedNoRisk--;
                var ti = val / maxQuestionsInTheme;
                var qi = val % maxQuestionsInTheme;
                round.Themes[ti].Questions[qi].Type.Name = QuestionTypes.Sponsored;
            }

            while (nonUsedNumbers.Count > 0 && leavedStakes > 0)
            {
                var num = ClientData.Rand.Next(nonUsedNumbers.Count);
                var val = nonUsedNumbers[num];
                nonUsedNumbers.RemoveAt(num);
                leavedStakes--;
                var ti = val / maxQuestionsInTheme;
                var qi = val % maxQuestionsInTheme;
                round.Themes[ti].Questions[qi].Type.Name = QuestionTypes.Auction;
            }

            while (nonUsedNumbers.Count > 0 && leavedSecrets > 0)
            {
                var num = ClientData.Rand.Next(nonUsedNumbers.Count);
                var val = nonUsedNumbers[num];
                nonUsedNumbers.RemoveAt(num);
                leavedSecrets--;
                var ti = val / maxQuestionsInTheme;
                var qi = val % maxQuestionsInTheme;
                var quest = round.Themes[ti].Questions[qi];
                quest.Type.Name = QuestionTypes.BagCat;

                quest.Type[QuestionTypeParams.Cat_Theme] = round.Themes[ti].Name;

                quest.Type[QuestionTypeParams.Cat_Cost] = GenerateRandomSecretQuestionCost();

                int var = ClientData.Rand.Next(2);
                if (var == 0)
                    quest.Type[QuestionTypeParams.BagCat_Self] = QuestionTypeParams.BagCat_Self_Value_True;
                else
                    quest.Type[QuestionTypeParams.BagCat_Self] = QuestionTypeParams.BagCat_Self_Value_False;

                var = ClientData.Rand.Next(100);
                if (var < 30)
                    quest.Type[QuestionTypeParams.BagCat_Knows] = QuestionTypeParams.BagCat_Knows_Value_Before;
                else if (var < 90)
                    quest.Type[QuestionTypeParams.BagCat_Knows] = QuestionTypeParams.BagCat_Knows_Value_After;
                else
                    quest.Type[QuestionTypeParams.BagCat_Knows] = QuestionTypeParams.BagCat_Knows_Value_Never;
            }
        }

        private string GenerateRandomSecretQuestionCost()
        {
            int var = ClientData.Rand.Next(3);
            if (var == 0)
            {
                // 100 - 2000
                return ((ClientData.Rand.Next(20) + 1) * 100).ToString();
            }

            if (var == 1)
            {
                return "0";
            }

            var sumMin = (ClientData.Rand.Next(10) + 1) * 100;
            var sumMax = sumMin + (ClientData.Rand.Next(10) + 1) * 100;
            var maxSteps = (sumMax - sumMin) / 100;

            var possibleSteps = Enumerable.Range(1, maxSteps).Where(step => maxSteps % step == 0).ToArray();
            var stepIndex = ClientData.Rand.Next(possibleSteps.Length);
            var steps = possibleSteps[stepIndex];

            var str = new StringBuilder().AppendFormat("[{0};{1}]", sumMin, sumMax);
            if (steps != maxSteps)
            {
                str.Append('/').Append(steps * 100);
            }

            return str.ToString();
        }

        /// <summary>
        /// Вывести информацию о Вопросе с секретом
        /// </summary>
        private void PrintSecretQuestionInfo()
        {
            var questionTheme = _data.Type[QuestionTypeParams.Cat_Theme];
            
            var s = new StringBuilder(LO[nameof(R.Theme)])
                .Append(": ")
                .Append(string.IsNullOrEmpty(questionTheme) ? _data.Theme.Name : questionTheme)
                .AppendFormat(", {0}: ", LO[nameof(R.Cost)]);

            var cost = _data.Type[QuestionTypeParams.Cat_Cost];

            string add;
            if (int.TryParse(cost, out var questionPrice) && questionPrice > 0)
            {
                _data.CurPriceWrong = _data.CurPriceRight = questionPrice;

                _data.CatInfo = new BagCatInfo
                {
                    Minimum = _data.CurPriceRight,
                    Maximum = _data.CurPriceRight
                };

                add = _data.CurPriceRight.ToString();
            }
            else if (_data.Type.Name == QuestionTypes.Cat)
            {
                _data.CatInfo = new BagCatInfo();
                add = _data.Question.Price.ToString();
                _data.CurPriceRight = _data.Question.Price;
                _data.CurPriceWrong = _data.CurPriceRight;
            }
            else
            {
                _data.CurPriceRight = -1;
                _data.CatInfo = BagCatHelper.ParseCatCost(cost);
                if (_data.CatInfo != null)
                {
                    if (_data.CatInfo.Step == 0)
                    {
                        add = _data.CatInfo.Maximum.ToString();
                        _data.CurPriceRight = _data.CatInfo.Maximum;
                        _data.CurPriceWrong = _data.CurPriceRight;
                    }
                    else if (_data.CatInfo.Step < _data.CatInfo.Maximum - _data.CatInfo.Minimum)
                        add = LO[nameof(R.From)] + " " + Notion.FormatNumber(_data.CatInfo.Minimum) + " " + LO[nameof(R.From)] + " " + Notion.FormatNumber(_data.CatInfo.Maximum) + " " + LO[nameof(R.WithStepOf)] + " " + Notion.FormatNumber(_data.CatInfo.Step) + " (" + LO[nameof(R.YourChoice)] + ")";
                    else
                        add = Notion.FormatNumber(_data.CatInfo.Minimum) + " " + LO[nameof(R.Or)] + " " + Notion.FormatNumber(_data.CatInfo.Maximum) + " (" + LO[nameof(R.YourChoice)] + ")";
                }
                else
                {
                    var catInfo = _data.CatInfo = new BagCatInfo();

                    catInfo.Minimum = _data.Round.Themes[0].Questions[0].Price;
                    catInfo.Maximum = catInfo.Minimum;
                    foreach (var theme in _data.Round.Themes)
                    {
                        foreach (var quest in theme.Questions)
                        {
                            var price = quest.Price;
                            if (price < catInfo.Minimum)
                                catInfo.Minimum = price;

                            if (price > catInfo.Maximum)
                                catInfo.Maximum = price;
                        }
                    }

                    catInfo.Step = catInfo.Maximum - catInfo.Minimum;
                    if (catInfo.Step == 0)
                    {
                        add = catInfo.Maximum.ToString();
                        _data.CurPriceRight = catInfo.Maximum;
                        _data.CurPriceWrong = _data.CurPriceRight;
                    }
                    else
                        add = LO[nameof(R.MinMaxChoice)];
                }
            }

            s.Append(add);

            _gameActions.ShowmanReplic(s.ToString());
        }

        public bool IsPressMode(bool isMultimediaQuestion)
        {
            return true;
        }

        public bool ShowRight => true;

        public bool ShowScore => false;

        public bool AutomaticGame => false;

        public bool PlaySpecials => true;

        public int ThinkingTime => 0;

        public object WorkingLock { get; } = new object();
    }
}
