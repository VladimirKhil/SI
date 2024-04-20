namespace SICore.Clients.Other;

/// <summary>
/// Represents player statistic information.
/// </summary>
public sealed class PlayerStatistic
{
    public int RightAnswerCount { get; set; }

    public int WrongAnswerCount { get; set; }

    public int RightTotal { get; set; }

    public int WrongTotal { get; set; }
}
