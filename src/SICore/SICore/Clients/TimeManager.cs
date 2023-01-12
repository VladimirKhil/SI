namespace SICore;

public sealed class TimeManager
{
    private readonly ITimeProvider _data = null;

    public int this[int i]
    {
        get => i switch
        {
            0 => _data.RoundTime,
            1 => _data.PressingTime,
            _ => _data.ThinkingTime,
        };
        set
        {
            switch (i)
            {
                case 0:
                    _data.RoundTime = value;
                    break;

                case 1:
                    _data.PressingTime = value;
                    break;

                default:
                    _data.ThinkingTime = value;
                    break;
            }
        }
    }

    public TimeManager(ITimeProvider data) => _data = data;
}
