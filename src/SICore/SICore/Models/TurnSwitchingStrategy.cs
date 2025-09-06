namespace SICore.Models;

internal enum TurnSwitchingStrategy
{
    /// <summary>
    /// Never switch turn.
    /// </summary>
    Never,
    
    /// <summary>
    /// Switch turn if the player answered correctly by pressing the button.
    /// </summary>
    ByRightAnswerOnButton,

    /// <summary>
    /// Switch turn sequentially.
    /// </summary>
    Sequentially,
}
