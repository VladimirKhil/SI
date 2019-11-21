﻿using SIEngine;
using SImulator.Model;
using SImulator.ViewModel.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SImulator.ViewModel.ViewModel
{
    /// <summary>
    /// Настройки для движка СИ
    /// </summary>
    internal sealed class EngineSettingsProvider : IEngineSettingsProvider
    {
        private readonly AppSettings _appSettings;

        public EngineSettingsProvider(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public bool IsPressMode(bool isMultimediaQuestion) => _appSettings.FalseStart && (!isMultimediaQuestion || _appSettings.FalseStartMultimedia) && _appSettings.UsePlayersKeys != PlayerKeysModes.None;

        public bool ShowRight => _appSettings.ShowRight;

        public bool ShowScore => _appSettings.SIUISettings.ShowScore;

        public bool AutomaticGame => _appSettings.AutomaticGame;

        public bool PlaySpecials => _appSettings.PlaySpecials;

        public int ThinkingTime => _appSettings.ThinkingTime;
    }
}
