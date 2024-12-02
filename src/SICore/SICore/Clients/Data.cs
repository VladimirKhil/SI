using SICore.Contracts;
using SIData;
using SIUI.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace SICore;

// TODO: fully split between the game and the clients

/// <summary>
/// Represents common client data.
/// </summary>
public abstract class Data : INotifyPropertyChanged
{
    /// <summary>
    /// Game host.
    /// </summary>
    public IGameHost Host { get; }

    /// <summary>
    /// Game table info.
    /// </summary>
    public TableInfo TInfo { get; } = new();

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

    public StringBuilder EventLog { get; } = new();

    public StringBuilder PersonsUpdateHistory { get; } = new();

    public Data(IGameHost gameManager)
    {
        Host = gameManager;
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
