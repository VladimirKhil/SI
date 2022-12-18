namespace SIQuester.Model;

/// <summary>
/// Параметры нестандартного пакета
/// </summary>
public sealed class NonStandartPackageParams
{
    /// <summary>
    /// Число стандартных раундов
    /// </summary>
    public int NumOfRounds { get; set; } = 3;

    /// <summary>
    /// Число тем в стандартном раунде
    /// </summary>
    public int NumOfThemes { get; set; } = 6;

    /// <summary>
    /// Число вопросов в теме
    /// </summary>
    public int NumOfQuestions { get; set; } = 5;

    /// <summary>
    /// Базовая стоимость вопроса в теме 1-го раунда
    /// </summary>
    public int NumOfPoints { get; set; } = 100;

    /// <summary>
    /// Имеется ли в пакете финальный раунд
    /// </summary>
    public bool HasFinal { get; set; } = true;

    /// <summary>
    /// Число тем в финальном раунде
    /// </summary>
    public int NumOfFinalThemes { get; set; } = 7;
}
