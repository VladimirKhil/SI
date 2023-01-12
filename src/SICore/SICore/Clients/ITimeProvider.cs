namespace SICore;

public interface ITimeProvider
{
    int RoundTime { get; set; }

    int PressingTime { get; set; }

    int ThinkingTime { get; set; }
}