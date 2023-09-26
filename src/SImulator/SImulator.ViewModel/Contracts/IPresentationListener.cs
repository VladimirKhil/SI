namespace SImulator.ViewModel.Contracts;

/// <summary>
/// Provides callback methods for game UI.
/// </summary>
public interface IPresentationListener
{
    void OnQuestionSelected(int theme, int question);

    void OnAnswerSelected(int answerIndex);

    void OnThemeSelected(int themeIndex);

    void AskNext();

    void AskBack();

    void AskNextRound();

    void AskBackRound();

    void AskStop();

    void OnMediaStart();

    void OnMediaEnd();

    void OnMediaProgress(double progress);

    void OnIntroFinished();

    void OnRoundThemesFinished();
}
