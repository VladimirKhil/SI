using SImulator.ViewModel.Model;
using SImulator.ViewModel.PlatformSpecific;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;
using System.Windows.Input;
using System.Xml.Serialization;
using Utils.Commands;

namespace SImulator.ViewModel;

public sealed class AppSettingsViewModel
{
    public AppSettings Model { get; private set; }

    public SettingsViewModel SIUISettings { get; private set; }

    public GameModes[] Modes { get; } = new GameModes[] { GameModes.Tv, GameModes.Sport };

    [XmlIgnore]
    public ICommand Reset { get; private set; }

    public AppSettingsViewModel(AppSettings settings)
    {
        Model = settings;
        SIUISettings = new SettingsViewModel(settings.SIUISettings);

        Reset = new SimpleCommand(Reset_Executed);
    }

    internal void Reset_Executed(object? arg)
    {
        var defaultSettings = new AppSettings();
        PlatformManager.Instance.InitSettings(defaultSettings);

        var defaultUISettings = new Settings();
        var currentSettings = SIUISettings;

        var design = arg == null || arg.ToString() == "1";
        var rules = arg == null || arg.ToString() == "2";
        var buttons = arg == null || arg.ToString() == "4";
        var sounds = arg == null || arg.ToString() == "8";

        if (design)
        {
            currentSettings.QuestionLineSpacing = defaultUISettings.QuestionLineSpacing;
            currentSettings.Model.TableColorString = defaultUISettings.TableColorString;
            currentSettings.Model.TableBackColorString = defaultUISettings.TableBackColorString;
            currentSettings.Model.TableGridColorString = defaultUISettings.TableGridColorString;
            currentSettings.Model.AnswererColorString = defaultUISettings.AnswererColorString;
            currentSettings.TableFontFamily = defaultUISettings.TableFontFamily;
            Model.VideoUrl = defaultSettings.VideoUrl;
            currentSettings.Model.LogoUri = defaultUISettings.LogoUri;
            currentSettings.Model.BackgroundImageUri = defaultUISettings.BackgroundImageUri;
            currentSettings.Model.BackgroundVideoUri = defaultUISettings.BackgroundVideoUri;
            Model.ShowRight = defaultSettings.ShowRight;
            Model.ShowPlayers = defaultSettings.ShowPlayers;
            Model.ShowTableCaption= defaultSettings.ShowTableCaption;
            Model.ShowTextNoFalstart = defaultSettings.ShowTextNoFalstart;
        }

        if (rules)
        {            
            Model.GameMode = defaultSettings.GameMode;

            Model.RoundTime = defaultSettings.RoundTime;
            Model.ThinkingTime = defaultSettings.ThinkingTime;
            Model.ThinkingTime2 = defaultSettings.ThinkingTime2;
            Model.SpecialQuestionThinkingTime = defaultSettings.SpecialQuestionThinkingTime;
            Model.FinalQuestionThinkingTime = defaultSettings.FinalQuestionThinkingTime;
            Model.QuestionReadingSpeed = defaultSettings.QuestionReadingSpeed;
            
            Model.BlockingTime = defaultSettings.BlockingTime;
            Model.PlayersView = defaultSettings.PlayersView;

            Model.DropStatsOnBack = defaultSettings.DropStatsOnBack;
            Model.FalseStart = defaultSettings.FalseStart;
            Model.EndQuestionOnRightAnswer = defaultSettings.EndQuestionOnRightAnswer;
            Model.SignalsAfterTimer = defaultSettings.SignalsAfterTimer;
            Model.UsePlayersKeys = defaultSettings.UsePlayersKeys;
            Model.SubstractOnWrong = defaultSettings.SubstractOnWrong;
            Model.PlaySpecials = defaultSettings.PlaySpecials;
            Model.FalseStartMultimedia = defaultSettings.FalseStartMultimedia;

            Model.SaveLogs = defaultSettings.SaveLogs;
            Model.AutomaticGame = defaultSettings.AutomaticGame;

            Model.SpecialsAliases.StakeQuestionAlias = defaultSettings.SpecialsAliases.StakeQuestionAlias;
            Model.SpecialsAliases.SecretQuestionAlias = defaultSettings.SpecialsAliases.SecretQuestionAlias;
            Model.SpecialsAliases.NoRiskQuestionAlias = defaultSettings.SpecialsAliases.NoRiskQuestionAlias;
        }

        if (buttons)
        {
            currentSettings.Model.KeyboardControl = defaultUISettings.KeyboardControl;
        }

        if (sounds)
        {
            Model.PlaySounds = defaultSettings.PlaySounds;
            Model.Sounds.BeginGame = defaultSettings.Sounds.BeginGame;
            Model.Sounds.GameThemes = defaultSettings.Sounds.GameThemes;
            Model.Sounds.RoundBegin = defaultSettings.Sounds.RoundBegin;
            Model.Sounds.RoundThemes = defaultSettings.Sounds.RoundThemes;
            Model.Sounds.QuestionSelected = defaultSettings.Sounds.QuestionSelected;
            Model.Sounds.PlayerPressed = defaultSettings.Sounds.PlayerPressed;
            Model.Sounds.NoRiskQuestion = defaultSettings.Sounds.NoRiskQuestion;
            Model.Sounds.SecretQuestion = defaultSettings.Sounds.SecretQuestion;
            Model.Sounds.StakeQuestion = defaultSettings.Sounds.StakeQuestion;
            Model.Sounds.StakeForAllQuestion = defaultSettings.Sounds.StakeForAllQuestion;
            Model.Sounds.ForAllQuestion = defaultSettings.Sounds.ForAllQuestion;
            Model.Sounds.AnswerRight = defaultSettings.Sounds.AnswerRight;
            Model.Sounds.AnswerWrong = defaultSettings.Sounds.AnswerWrong;
            Model.Sounds.NoAnswer = defaultSettings.Sounds.NoAnswer;
            Model.Sounds.RoundTimeout = defaultSettings.Sounds.RoundTimeout;
            Model.Sounds.FinalDelete = defaultSettings.Sounds.FinalDelete;
            Model.Sounds.FinalThink = defaultSettings.Sounds.FinalThink;
        }
    }
}
