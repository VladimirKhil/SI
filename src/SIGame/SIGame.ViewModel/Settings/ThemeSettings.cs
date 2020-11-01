using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using SIUI.ViewModel.Core;

namespace SIGame.ViewModel
{
    /// <summary>
    /// Настройки темы
    /// </summary>
    public sealed class ThemeSettings: INotifyPropertyChanged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Settings uiSettings = new Settings();

        /// <summary>
        /// Настройки отображения табло
        /// </summary>
        public Settings UISettings
        {
            get { return uiSettings; }
            set { uiSettings = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string customMainBackgroundUri = null;

        /// <summary>
        /// Настроенное главное фоновое изображение
        /// </summary>
        public string CustomMainBackgroundUri
        {
            get { return customMainBackgroundUri; }
            set { customMainBackgroundUri = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string customBackgroundUri = null;

        /// <summary>
        /// Настроенное фоновое изображение
        /// </summary>
        public string CustomBackgroundUri
        {
            get { return customBackgroundUri; }
            set { customBackgroundUri = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string soundMainMenuUri = null;

        /// <summary>
        /// Мелодия главного меню
        /// </summary>
        public string SoundMainMenuUri
        {
            get { return soundMainMenuUri; }
            set { soundMainMenuUri = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string soundBeginRoundUri = null;

        /// <summary>
        /// Мелодия начала раунда
        /// </summary>
        public string SoundBeginRoundUri
        {
            get { return soundBeginRoundUri; }
            set { soundBeginRoundUri = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string soundRoundThemesUri = null;

        /// <summary>
        /// Мелодия тем раунда
        /// </summary>
        public string SoundRoundThemesUri
        {
            get { return soundRoundThemesUri; }
            set { soundRoundThemesUri = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string soundNoAnswerUri = null;

        /// <summary>
        /// Мелодия окончания времени на нажатие кнопки
        /// </summary>
        public string SoundNoAnswerUri
        {
            get { return soundNoAnswerUri; }
            set { soundNoAnswerUri = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string soundQuestionStakeUri = null;

        /// <summary>
        /// Мелодия вопроса со ставкой
        /// </summary>
        public string SoundQuestionStakeUri
        {
            get { return soundQuestionStakeUri; }
            set { soundQuestionStakeUri = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string soundQuestionGiveUri = null;

        /// <summary>
        /// Мелодия вопроса с передачей
        /// </summary>
        public string SoundQuestionGiveUri
        {
            get { return soundQuestionGiveUri; }
            set { soundQuestionGiveUri = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string soundQuestionNoRiskUri = null;

        /// <summary>
        /// Мелодия вопроса без риска
        /// </summary>
        public string SoundQuestionNoRiskUri
        {
            get { return soundQuestionNoRiskUri; }
            set { soundQuestionNoRiskUri = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string soundFinalThinkUri = null;

        /// <summary>
        /// Мелодия размышления в финале
        /// </summary>
        public string SoundFinalThinkUri
        {
            get { return soundFinalThinkUri; }
            set { soundFinalThinkUri = value; OnPropertyChanged(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string soundTimeoutUri = null;

        /// <summary>
        /// Мелодия окончания времени раунда
        /// </summary>
        public string SoundTimeoutUri
        {
            get { return soundTimeoutUri; }
            set { soundTimeoutUri = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void Initialize(ThemeSettings themeSettings)
        {
            uiSettings.Initialize(themeSettings.uiSettings);

            CustomMainBackgroundUri = themeSettings.CustomMainBackgroundUri;
            CustomBackgroundUri = themeSettings.CustomBackgroundUri;
            SoundMainMenuUri = themeSettings.SoundMainMenuUri;
            SoundBeginRoundUri = themeSettings.SoundBeginRoundUri;
            SoundRoundThemesUri = themeSettings.SoundRoundThemesUri;
            SoundNoAnswerUri = themeSettings.SoundNoAnswerUri;
            SoundQuestionStakeUri = themeSettings.SoundQuestionStakeUri;
            SoundQuestionGiveUri = themeSettings.SoundQuestionGiveUri;
            SoundQuestionNoRiskUri = themeSettings.SoundQuestionNoRiskUri;
            SoundFinalThinkUri = themeSettings.SoundFinalThinkUri;
            SoundTimeoutUri = themeSettings.SoundTimeoutUri;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
