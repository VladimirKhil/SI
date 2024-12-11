namespace SICore;

/// <summary>
/// Provides common behavior for player and showman.
/// </summary>
[Obsolete]
public interface IPersonLogic
{
    /// <summary>
    /// Отдать вопрос с секретом
    /// </summary>
    [Obsolete]
    void Cat();

    /// <summary>
    /// Нужно сделать ставку
    /// </summary>
    [Obsolete]
    void Stake();

    /// <summary>
    /// Ставка в финале
    /// </summary>
    [Obsolete]
    void FinalStake() { }

    /// <summary>
    /// Определение стоимости Вопроса с секретом
    /// </summary>
    [Obsolete]
    void CatCost();
}
