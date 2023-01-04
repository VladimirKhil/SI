namespace SICore;

/// <summary>
/// Ведущий
/// </summary>
public interface IShowman : IPerson
{
    /// <summary>
    /// Выбор начинающего раунд
    /// </summary>
    void StarterChoose();

    /// <summary>
    /// Кто следующим делает ставку
    /// </summary>
    void FirstStake();

    /// <summary>
    /// Верен ли ответ
    /// </summary>
    void IsRight();

    /// <summary>
    /// Кто следующим убирает тему
    /// </summary>
    void FirstDelete();

    void ClearSelections(bool full = false);

    void ManageTable() { }
}
