namespace SICore.Models;

internal sealed class TimerInfo
{
    public bool IsEnabled { get; set; }

    public bool IsUserEnabled { get; set; } = true;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int MaxTime { get; set; }

    public int PauseTime { get; set; } = -1;
}
