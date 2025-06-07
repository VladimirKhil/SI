﻿using SIData;
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

    private GameStage _stage = GameStage.Before;

    /// <summary>
    /// Current game stage.
    /// </summary>
    public GameStage Stage
    {
        get => _stage;
        set { _stage = value; OnPropertyChanged(); }
    }

    public StringBuilder PersonsUpdateHistory { get; } = new();

    protected static string PrintAccount(ViewerAccount viewerAccount) =>
        $"{viewerAccount?.Name}@{viewerAccount?.IsHuman}:{viewerAccount?.IsConnected}";

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
