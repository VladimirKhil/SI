namespace SIPackages.Core;

/// <summary>
/// Defines well-known step parameter values.
/// </summary>
public static class StepParameterValues
{
    /// <summary>
    /// Defines a fallback right answer to use when a primary answer ref is missing.
    /// </summary>
    /// <remarks>
    /// When a reference equals to this value and question parameter with corresponding name is missing,
    /// question's first right answer is used instead.
    /// </remarks>
    public const string FallbackStepIdRef_Right = "right";

    /// <summary>
    /// Button AskAnswer mode.
    /// </summary>
    public const string AskAnswerMode_Button = "button";

    /// <summary>
    /// Direct AskAnswer mode.
    /// </summary>
    public const string AskAnswerMode_Direct = "direct";

    /// <summary>
    /// Stake SetAnswerer mode.
    /// </summary>
    public const string SetAnswererMode_Stake = "stake";

    /// <summary>
    /// Current SetAnswerer mode.
    /// </summary>
    public const string SetAnswererMode_Current = "current";

    /// <summary>
    /// By current SetAnswerer mode.
    /// </summary>
    public const string SetAnswererMode_ByCurrent = "byCurrent";

    /// <summary>
    /// All possible SetAnswerer selector (everybody who can make stake will play).
    /// </summary>
    public const string SetAnswererSelect_AllPossible = "allPossible";

    /// <summary>
    /// Highest SetAnswerer selector (player with highest stake will play).
    /// </summary>
    public const string SetAnswererSelect_Highest = "highest";

    /// <summary>
    /// Any SetAnswerer selector.
    /// </summary>
    public const string SetAnswererSelect_Any = "any";

    /// <summary>
    /// Except current SetAnswerer selector.
    /// </summary>
    public const string SetAnswererSelect_ExceptCurrent = "exceptCurrent";

    /// <summary>
    /// Visible SetAnswerer stakes (player stakes are visible).
    /// </summary>
    public const string SetAnswererStakeVisibility_Visible = "visible";

    /// <summary>
    /// Hidden SetAnswerer stakes (player stakes are hidden).
    /// </summary>
    public const string SetAnswererStakeVisibility_Hidden = "hidden";

    /// <summary>
    /// No risk SetPrice mode.
    /// </summary>
    public const string SetPriceMode_NoRisk = "noRisk";

    /// <summary>
    /// Select SetPrice mode.
    /// </summary>
    public const string SetPriceMode_Select = "select";

    /// <summary>
    /// SetAnswerType text type (default value).
    /// </summary>
    public const string SetAnswerTypeType_Text = "text";

    /// <summary>
    /// SetAnswerType select type.
    /// </summary>
    public const string SetAnswerTypeType_Select = "select";
}
