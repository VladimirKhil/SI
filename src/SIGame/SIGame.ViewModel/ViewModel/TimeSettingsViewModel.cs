using SIData;

namespace SIGame.ViewModel;

public sealed class TimeSettingsViewModel : ViewModel<TimeSettings>
{
    public Dictionary<TimeSettingsTypes, TimeSetting> All { get; private set; }

    public TimeSettingsViewModel()
    {

    }

    public TimeSettingsViewModel(TimeSettings model)
        : base(model)
    {

    }

    protected override void Initialize()
    {
        base.Initialize();

        All = new Dictionary<TimeSettingsTypes, TimeSetting>
        {
            [TimeSettingsTypes.ChoosingQuestion] = new TimeSetting(Properties.Resources.TimeSettings_ChoosingQuestion, _model, TimeSettingsTypes.ChoosingQuestion, 30, 120),
            [TimeSettingsTypes.ThinkingOnQuestion] = new TimeSetting(Properties.Resources.TimeSettings_ThinkingOnQuestion, _model, TimeSettingsTypes.ThinkingOnQuestion, 5, 120),
            [TimeSettingsTypes.PrintingAnswer] = new TimeSetting(Properties.Resources.TimeSettings_PrintingAnswer, _model, TimeSettingsTypes.PrintingAnswer, 25, 120),
            [TimeSettingsTypes.GivingCat] = new TimeSetting(Properties.Resources.TimeSettings_GivingCat, _model, TimeSettingsTypes.GivingCat, 30, 120),
            [TimeSettingsTypes.MakingStake] = new TimeSetting(Properties.Resources.TimeSettings_MakingStake, _model, TimeSettingsTypes.MakingStake, 30, 120),
            [TimeSettingsTypes.ThinkingOnSpecial] = new TimeSetting(Properties.Resources.TimeSettings_ThinkingOnSpecial, _model, TimeSettingsTypes.ThinkingOnSpecial, 25, 120),
            [TimeSettingsTypes.RightAnswer] = new TimeSetting(Properties.Resources.TimeSettings_RightAnswer, _model, TimeSettingsTypes.RightAnswer, 2, 10),
            [TimeSettingsTypes.Round] = new TimeSetting(Properties.Resources.TimeSettings_Round, _model, TimeSettingsTypes.Round, TimeSettings.DefaultTimeOfRound, 10800),
            [TimeSettingsTypes.ChoosingFinalTheme] = new TimeSetting(Properties.Resources.TimeSettings_ChoosingFinalTheme, _model, TimeSettingsTypes.ChoosingFinalTheme, 30, 120),
            [TimeSettingsTypes.FinalThinking] = new TimeSetting(Properties.Resources.TimeSettings_FinalThinking, _model, TimeSettingsTypes.FinalThinking, 45, 120),
            [TimeSettingsTypes.ShowmanDecisions] = new TimeSetting(Properties.Resources.TimeSettings_ShowmanDecisions, _model, TimeSettingsTypes.ShowmanDecisions, 30, 300),
            [TimeSettingsTypes.MediaDelay] = new TimeSetting(Properties.Resources.TimeSettings_MediaDelay, _model, TimeSettingsTypes.MediaDelay, 0, 10)
        };
    }
}
