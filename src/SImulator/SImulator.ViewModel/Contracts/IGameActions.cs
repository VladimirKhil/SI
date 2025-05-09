namespace SImulator.ViewModel.Contracts;

/// <summary>
/// Controls the game process.
/// </summary>
public interface IGameActions : IDisposable
{
    void MoveNext(int delayMs = 100);

    void MoveBack();

    void MoveNextRound();

    void MoveBackRound();

    void IsRightAnswer();
}
