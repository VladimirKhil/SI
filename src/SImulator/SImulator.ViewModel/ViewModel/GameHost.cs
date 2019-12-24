using SIEngine;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.PlatformSpecific;
using SImulator.ViewModel.Properties;
using SIUI.ViewModel.Core;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SImulator.ViewModel
{
    public class GameHost: IExtendedGameHost
    {
        private readonly EngineBase _engine;

        public bool IsMediaEnded { get; set; }

        public ICommand Next { get; set; }
        public ICommand Back { get; set; }
        public ICommand NextRound { get; set; }
        public ICommand PreviousRound { get; set; }

        public ICommand Stop { get; set; }

        public event Action<int> ThemeDeleted;
        public event Action MediaStart;
        public event Action MediaEnd;
        public event Action<double> MediaProgress;
        public event Action RoundThemesFinished;

        public GameHost(EngineBase engine)
        {
            _engine = engine;
        }

        public void OnQuestionSelected(int theme, int question)
        {
            try
            {
                ((TvEngine)_engine).SelectQuestion(theme, question);
            }
            catch (Exception exc) when (exc is TimeoutException || exc is CommunicationException)
            {
                PlatformManager.Instance.ShowMessage($"{Resources.ConnectionError}: {exc.Message}");
            }
        }

        public void OnThemeSelected(int themeIndex)
        {
            try
            {
                ((TvEngine)_engine).SelectTheme(themeIndex);
            }
            catch (TimeoutException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (CommunicationException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
        }

        private readonly object _moveLock = new object();

        public void AskNext()
        {
            lock (_moveLock)
            {
                if (Next?.CanExecute(null) == true)
                    Next.Execute(null);
            }
        }

        public void AskBack()
        {
            lock (_moveLock)
            {
                if (_engine.CanMoveBack)
                    Back?.Execute(null);
            }
        }

        public void AskNextRound()
        {
            lock (_moveLock)
            {
                if (NextRound?.CanExecute(null) == true)
                    NextRound.Execute(null);
            }
        }

        public void AskBackRound()
        {
            lock (_moveLock)
            {
                if (PreviousRound?.CanExecute(null) == true)
                    PreviousRound.Execute(null);
            }
        }

        public void AskStop()
        {
            if (TaskScheduler.Current != UI.Scheduler)
            {
                Task.Factory.StartNew(AskStop, CancellationToken.None, TaskCreationOptions.None, UI.Scheduler);
                return;
            }

            if (Stop?.CanExecute(null) == true)
                Stop.Execute(null);
        }

        public void OnReady()
        {
            try
            {
                var result = ((TvEngine)_engine).OnReady(out bool more);
                if (result > -1)
                    ThemeDeleted?.Invoke(result);
            }
            catch (TimeoutException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
            catch (CommunicationException exc)
            {
                PlatformManager.Instance.ShowMessage(string.Format("Ошибка связи: {0}", exc.Message));
            }
        }

        public void OnMediaStart()
        {
            MediaStart?.Invoke();
            IsMediaEnded = false;
        }

        public void OnMediaEnd()
        {
            if (_engine.IsIntro())
                return;

            IsMediaEnded = true;
            MediaEnd?.Invoke();
        }

        public void OnMediaProgress(double progress)
        {
            MediaProgress?.Invoke(progress);
        }

        public void OnIntroFinished()
        {
            _engine.OnIntroFinished();
        }

        public void OnRoundThemesFinished()
        {
            RoundThemesFinished?.Invoke();
        }
    }
}
