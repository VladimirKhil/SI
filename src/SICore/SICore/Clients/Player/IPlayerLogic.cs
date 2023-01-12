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
    /// Надо отвечать
    /// </summary>
    void Answer();

    /// <summary>
    /// Проверка правильности ответа
    /// </summary>
    void IsRight(bool voteForRight);

    void Report();

    void Clear();

    /// <summary>
    /// Игрок получил или потерял деньги
    /// </summary>
    void PersonAnswered(int playerIndex, bool isRight);

    void StartThink();

    /// <summary>
    /// Получена часть вопроса
    /// </summary>
    void OnPlayerAtom(string[] mparams);
}
