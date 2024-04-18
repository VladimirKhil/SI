namespace SIEngine;

/// <summary>
/// Implements SIGame engine allowing to play any SIGame package.
/// </summary>
public interface ISIEngine
{
    GameStage Stage { get; }

    bool CanMoveBack { get; }

    void MoveNext();
}
