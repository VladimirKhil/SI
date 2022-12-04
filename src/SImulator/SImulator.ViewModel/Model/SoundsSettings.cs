using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SImulator.ViewModel.Model;

public sealed class SoundsSettings : INotifyPropertyChanged
{
    private string _beginGame = "game_begin.mp3";
    public string BeginGame { get => _beginGame; set { _beginGame = value; OnPropertyChanged(); } }

    private string _gameThemes = "";
    public string GameThemes { get => _gameThemes; set { _gameThemes = value; OnPropertyChanged(); } }

    private string _roundBegin = "round_begin.mp3";
    public string RoundBegin { get => _roundBegin; set { _roundBegin = value; OnPropertyChanged(); } }

    private string _roundThemes = "round_themes.mp3";
    public string RoundThemes { get => _roundThemes; set { _roundThemes = value; OnPropertyChanged(); } }

    private string _questionSelected = "";
    public string QuestionSelected { get => _questionSelected; set { _questionSelected = value; OnPropertyChanged(); } }

    private string _playerPressed = "";
    public string PlayerPressed { get => _playerPressed; set { _playerPressed = value; OnPropertyChanged(); } }

    private string _secretQuestion = "question_secret.mp3";
    public string SecretQuestion { get => _secretQuestion; set { _secretQuestion = value; OnPropertyChanged(); } }

    private string _stakeQuestion = "question_stake.mp3";
    public string StakeQuestion { get => _stakeQuestion; set { _stakeQuestion = value; OnPropertyChanged(); } }

    private string _noRiskQuestion = "question_norisk.mp3";
    public string NoRiskQuestion { get => _noRiskQuestion; set { _noRiskQuestion = value; OnPropertyChanged(); } }

    private string _answerRight = "";
    public string AnswerRight { get => _answerRight; set { _answerRight = value; OnPropertyChanged(); } }

    private string _answerWrong = "";
    public string AnswerWrong { get => _answerWrong; set { _answerWrong = value; OnPropertyChanged(); } }

    private string _noAnswer = "question_noanswers.mp3";
    public string NoAnswer { get => _noAnswer; set { _noAnswer = value; OnPropertyChanged(); } }

    private string _roundTimeout = "round_timeout.mp3";
    public string RoundTimeout { get => _roundTimeout; set { _roundTimeout = value; OnPropertyChanged(); } }

    private string _finalDelete = "final_delete.mp3";
    public string FinalDelete { get => _finalDelete; set { _finalDelete = value; OnPropertyChanged(); } }

    private string _finalThink = "final_think.mp3";
    public string FinalThink { get => _finalThink; set { _finalThink = value; OnPropertyChanged(); } }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
