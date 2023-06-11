using SIData;
using SIUI.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace SICore;

/// <summary>
/// Represents common client data.
/// </summary>
public abstract class Data : ITimeProvider, INotifyPropertyChanged
{
    public IGameManager BackLink { get; }

    /// <summary>
    /// Game table info.
    /// </summary>
    public TableInfo TInfo { get; } = new();

    public object TInfoLock { get; } = new object();

    public int PrevoiusTheme { get; set; } = -1;
    
    public int PreviousQuest { get; set; } = -1;

    /// <summary>
    /// Currently played theme index.
    /// </summary>
    public int ThemeIndex { get; set; } = -1;

    /// <summary>
    /// Currently played question index.
    /// </summary>
    public int QuestionIndex { get; set; } = -1;

    /// <summary>
    /// Объект синхронизации для choiceTheme и choiceQuest
    /// </summary>
    public object ChoiceLock { get; } = new object();

    private GameStage _stage = GameStage.Before;

    /// <summary>
    /// Current game stage.
    /// </summary>
    public GameStage Stage
    {
        get => _stage;
        set { _stage = value; OnPropertyChanged(); }
    }

    private int _roundTime = 0;
    /// <summary>
    /// Время раунда
    /// </summary>
    public int RoundTime
    {
        get => _roundTime;
        set { if (_roundTime != value) { _roundTime = value; OnPropertyChanged(); } }
    }

    private int _pressingTime = 0;

    /// <summary>
    /// Время на нажатие на кнопку
    /// </summary>
    public int PressingTime
    {
        get => _pressingTime;
        set { if (_pressingTime != value) { _pressingTime = value; OnPropertyChanged(); } }
    }

    private int _thinkingTime = 0;

    /// <summary>
    /// Время для принятия решения
    /// </summary>
    public int ThinkingTime
    {
        get => _thinkingTime;
        set { if (_thinkingTime != value) { _thinkingTime = value; OnPropertyChanged(); } }
    }

    internal int CurPriceRight { get; set; }
    
    internal int CurPriceWrong { get; set; }

    /// <summary>
    /// Информация о системных ошибках в игре, которые неплохо бы отправлять автору, но которые не приводят к краху системы
    /// </summary>
    public StringBuilder SystemLog { get; } = new();

    public StringBuilder PersonsUpdateHistory { get; } = new();

    public StringBuilder EventLog { get; } = new();

    public Data(IGameManager gameManager)
    {
        BackLink = gameManager;
    }

    protected static string PrintAccount(ViewerAccount viewerAccount) =>
        $"{viewerAccount?.Name}@{viewerAccount?.IsHuman}:{viewerAccount?.IsConnected}";

    public virtual void OnAddString(string person, string text, LogMode mode)
    {
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
