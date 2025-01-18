namespace SIData;

// TODO: merge with GameStages

/// <summary>
/// Defines well-known game stages.
/// </summary>
public enum GameStage
{
    /// <summary>
    /// Before game start.
    /// </summary>
    Before,

    /// <summary>
    /// Game start.
    /// </summary>
    Begin,

    /// <summary>
    /// Standard round.
    /// </summary>
    Round,

    /// <summary>
    /// Final round.
    /// </summary>
    [Obsolete]
    Final,

    /// <summary>
    /// After game finish.
    /// </summary>
    After
}
