namespace SICore;

/// <summary>
/// Логика участника игры (человека или компьютера)
/// </summary>
public interface ILogic
{
    void AddLog(string s);

    Data Data { get; }
}
