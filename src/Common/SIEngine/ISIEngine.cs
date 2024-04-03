namespace SIEngine;

/// <summary>
/// Implements SIGame engine allowing to play any SIGame package (<see cref="SIDocument" />).
/// </summary>
public interface ISIEngine
{
    GameStage Stage { get; }

    bool CanMoveBack { get; }

    void MoveNext();

    void SelectTheme(int publicThemeIndex);

    int OnReady(out bool more);
}
