using SImulator.ViewModel.Model;
using SImulator.ViewModel.Properties;

namespace SImulator.ViewModel;

public sealed class SoundSettingsViewModel
{
    public SoundViewModel[] Items { get; }

    public SoundSettingsViewModel(SoundsSettings sounds)
    {
        Items = new[]
        {
            new SoundViewModel(Resources.GameStart, () => sounds.BeginGame, value => sounds.BeginGame = value),
            new SoundViewModel(Resources.GameThemes, () => sounds.GameThemes, value => sounds.GameThemes = value),
            new SoundViewModel(Resources.RoundStart, () => sounds.RoundBegin, value => sounds.RoundBegin = value),
            new SoundViewModel(Resources.RoundThemes, () => sounds.RoundThemes, value => sounds.RoundThemes = value),
            new SoundViewModel(Resources.QuestionSelection, () => sounds.QuestionSelected, value => sounds.QuestionSelected = value),
            new SoundViewModel(Resources.ButtonPressed, () => sounds.PlayerPressed, value => sounds.PlayerPressed = value),
            new SoundViewModel(Resources.QuestionTypeSecret, () => sounds.SecretQuestion, value => sounds.SecretQuestion = value),
            new SoundViewModel(Resources.QuestionTypeStake, () => sounds.StakeQuestion, value => sounds.StakeQuestion = value),
            new SoundViewModel(Resources.QuestionTypeStakeForAll, () => sounds.StakeForAllQuestion, value => sounds.StakeForAllQuestion = value),
            new SoundViewModel(Resources.NoRiskQuestion, () => sounds.NoRiskQuestion, value => sounds.NoRiskQuestion = value),
            new SoundViewModel(Resources.QuestionTypeForAll, () => sounds.ForAllQuestion, value => sounds.ForAllQuestion = value),
            new SoundViewModel(Resources.RightAnswer, () => sounds.AnswerRight, value => sounds.AnswerRight = value),
            new SoundViewModel(Resources.WrongAnswer, () => sounds.AnswerWrong, value => sounds.AnswerWrong = value),
            new SoundViewModel(Resources.NoAnswer, () => sounds.NoAnswer, value => sounds.NoAnswer = value),
            new SoundViewModel(Resources.RoundTimeout, () => sounds.RoundTimeout, value => sounds.RoundTimeout = value),
            new SoundViewModel(Resources.FinalThemeRemoval, () => sounds.FinalDelete, value => sounds.FinalDelete = value),
            new SoundViewModel(Resources.FinalThink, () => sounds.FinalThink, value => sounds.FinalThink = value)
        };
    }

    internal void Reset(SoundsSettings defaultSounds)
    {
        Items[0].Value = defaultSounds.BeginGame;
        Items[1].Value = defaultSounds.GameThemes;
        Items[2].Value = defaultSounds.RoundBegin;
        Items[3].Value = defaultSounds.RoundThemes;
        Items[4].Value = defaultSounds.QuestionSelected;
        Items[5].Value = defaultSounds.PlayerPressed;
        Items[6].Value = defaultSounds.SecretQuestion;
        Items[7].Value = defaultSounds.StakeQuestion;
        Items[8].Value = defaultSounds.StakeForAllQuestion;
        Items[9].Value = defaultSounds.NoRiskQuestion;
        Items[10].Value = defaultSounds.ForAllQuestion;
        Items[11].Value = defaultSounds.AnswerRight;
        Items[12].Value = defaultSounds.AnswerWrong;
        Items[13].Value = defaultSounds.NoAnswer;
        Items[14].Value = defaultSounds.RoundTimeout;
        Items[15].Value = defaultSounds.FinalDelete;
        Items[16].Value = defaultSounds.FinalThink;
    }
}
