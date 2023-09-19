namespace SIPackages.Core;

/// <summary>
/// Provides well-known question type parameters.
/// </summary>
public static class QuestionTypeParams
{
    /// <summary>
    /// Question special theme.
    /// </summary>
    public const string Cat_Theme = "theme";

    /// <summary>
    /// Question special (hidden) price.
    /// </summary>
    public const string Cat_Cost = "cost";

    /// <summary>
    /// Можно ли отдать "Вопрос с секретом" себе
    /// </summary>
    public const string BagCat_Self = "self";

    /// <summary>
    /// Можно
    /// </summary>
    public const string BagCat_Self_Value_True = "true";

    /// <summary>
    /// Нельзя
    /// </summary>
    public const string BagCat_Self_Value_False = "false";

    /// <summary>
    /// Когда становятся известны данные "Вопроса с секретом"
    /// </summary>
    public const string BagCat_Knows = "knows";

    /// <summary>
    /// До отдачи
    /// </summary>
    public const string BagCat_Knows_Value_Before = "before";

    /// <summary>
    /// После отдачи
    /// </summary>
    public const string BagCat_Knows_Value_After = "after";

    /// <summary>
    /// Никогда: игроку просто начисляются деньги
    /// </summary>
    public const string BagCat_Knows_Value_Never = "never";
}
