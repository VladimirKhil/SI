namespace SICore.Connections
{
    public interface IConnectionLogger
    {
        void Log(int gameId, string message);
    }
}
