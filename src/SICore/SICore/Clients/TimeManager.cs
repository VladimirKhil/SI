namespace SICore
{
    public sealed class TimeManager
    {
        private readonly ITimeProvider _data = null;

        public int this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return _data.RoundTime;
                    case 1:
                        return _data.PressingTime;
                    default:
                        return _data.ThinkingTime;
                }
            }
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

        public TimeManager(ITimeProvider data)
        {
            _data = data;
        }
    }
}
