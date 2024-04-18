namespace SImulator.ViewModel.Contracts;

/// <summary>
/// Provides callback methods for game UI.
/// </summary>
public interface IPresentationListener
{
    void OnAnswerSelected(int answerIndex);

    void AskNext();

    void AskBack();

    void AskNextRound();

    void AskBackRound();

    void AskStop();

    void OnMediaStart();

    void OnMediaEnd();

    void OnMediaProgress(double progress);

    void OnRoundThemesFinished();
}
