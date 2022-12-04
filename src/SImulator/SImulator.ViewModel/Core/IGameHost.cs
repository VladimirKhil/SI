namespace SImulator.ViewModel.Core;

/// <summary>
/// Provides callback methods for game UI.
/// </summary>
public interface IGameHost
{
    void OnQuestionSelected(int theme, int question);

    void OnThemeSelected(int themeIndex);

    void AskNext();

    void AskBack();

    void AskNextRound();

    void AskBackRound();

    void AskStop();

    void OnReady();

    void OnMediaStart();

    void OnMediaEnd();

    void OnMediaProgress(double progress);

    void OnIntroFinished();

    void OnRoundThemesFinished();
}
