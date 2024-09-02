namespace SIGame.ViewModel.Models;

/// <summary>
/// Dialog modes.
/// </summary>
public enum DialogModes
{
    /// <summary>
    /// No dialog.
    /// </summary>
    None,

    /// <summary>
    /// Answer validation.
    /// </summary>
    AnswerValidation,

    /// <summary>
    /// Person score update.
    /// </summary>
    ChangeSum,

    /// <summary>
    /// Player answer.
    /// </summary>
    Answer,

    /// <summary>
    /// Setting question price.
    /// </summary>
    CatCost,

    /// <summary>
    /// Question stake selection.
    /// </summary>
    Stake,

    /// <summary>
    /// Question stake selection.
    /// </summary>
    StakeNew,

    /// <summary>
    /// Question final stake selection.
    /// </summary>
    FinalStake,

    /// <summary>
    /// Game report.
    /// </summary>
    Report,
}
