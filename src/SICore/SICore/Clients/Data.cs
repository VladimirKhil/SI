using SIUI.Model;
using System.Text;

namespace SICore;

// TODO: fully split between the game and the clients

/// <summary>
/// Represents common client data.
/// </summary>
public abstract class Data
{
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

    public StringBuilder PersonsUpdateHistory { get; } = new();

    protected static string PrintAccount(ViewerAccount viewerAccount) =>
        $"{viewerAccount?.Name}@{viewerAccount?.IsHuman}:{viewerAccount?.IsConnected}";
}
