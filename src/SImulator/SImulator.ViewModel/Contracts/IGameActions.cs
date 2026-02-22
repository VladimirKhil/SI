using SImulator.ViewModel.Model;

namespace SImulator.ViewModel.Contracts;

/// <summary>
/// Controls the game process.
/// </summary>
public interface IGameActions : IDisposable
{
    void MoveNext(int delayMs = 100);

    void ShowThemes(string[] themeNames);

    void MoveBack();

    void MoveNextRound();

    void MoveBackRound();

    void AddPlayer();

    void RemovePlayerAt(int index);

    void OnRightAnswer();

    void IsRightAnswer();

    void IsWrongAnswer();

    void SelectPlayer(int playerIndex);

    void ConnectPlayer(PlayerInfo player) { }

    void PlayerPressed(PlayerInfo player) { }
}
