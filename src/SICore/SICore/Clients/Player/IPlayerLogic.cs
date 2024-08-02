namespace SICore;

/// <summary>
/// Defines a player behavior.
/// </summary>
public interface IPlayerLogic : IPersonLogic
{
    /// <summary>
    /// Окончание размышлений
    /// </summary>
    void EndThink();

    /// <summary>
    /// Reacts to sending answer request.
    /// </summary>
    void Answer();

    /// <summary>
    /// Validates other player's ansswer.
    /// </summary>
    void IsRight(bool voteForRight, string answer);

    void Report();

    /// <summary>
    /// Игрок получил или потерял деньги
    /// </summary>
    void PersonAnswered(int playerIndex, bool isRight);

    void StartThink();

    /// <summary>
    /// Получена часть вопроса
    /// </summary>
    void OnPlayerAtom(string[] mparams);

    void OnTheme(string[] mparams) { }

    void OnChoice(string[] mparams) { }

    void SelectPlayer() { }
}
