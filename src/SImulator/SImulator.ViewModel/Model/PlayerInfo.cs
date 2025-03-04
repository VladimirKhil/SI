﻿using SIUI.ViewModel;
using System.Runtime.Serialization;

namespace SImulator.ViewModel.Model;

/// <summary>
/// Defines extended player info.
/// </summary>
[DataContract]
public sealed class PlayerInfo : SimplePlayerInfo
{
    /// <summary>
    /// Unique player identifier.
    /// </summary>
    public string? Id { get; set; }

    private int _right = 0;

    [DataMember]
    public int Right
    {
        get => _right;
        set { _right = value; OnPropertyChanged(); }
    }

    private int _wrong = 0;

    [DataMember]
    public int Wrong
    {
        get => _wrong;
        set { _wrong = value; OnPropertyChanged(); }
    }

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Button blocking time.
    /// </summary>
    internal DateTime? BlockedTime { get; set; }

    private string _answer = "";

    /// <summary>
    /// Player text answer.
    /// </summary>
    public string Answer
    {
        get => _answer;
        set { if (_answer != value) { _answer = value; OnPropertyChanged(); } }
    }

    private bool _isPreliminaryAnswer;

    /// <summary>
    /// Is current answer is a preliminary one.
    /// </summary>
    public bool IsPreliminaryAnswer
    {
        get => _isPreliminaryAnswer;
        set { if (_isPreliminaryAnswer != value) { _isPreliminaryAnswer = value; OnPropertyChanged(); } }
    }

    private bool _isConnected;

    /// <summary>
    /// Is the player connected.
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        set { if (_isConnected != value) { _isConnected = value; OnPropertyChanged(); } }
    }

    private int _stake = 0;

    /// <summary>
    /// Players stake.
    /// </summary>
    public int Stake
    {
        get => _stake;
        set { if (_stake != value) { _stake = value; OnPropertyChanged(); } }
    }
}
