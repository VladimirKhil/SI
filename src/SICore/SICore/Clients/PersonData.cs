using SICore.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SICore;

/// <summary>
/// Defines common data for player and showman.
/// </summary>
public sealed class PersonData : INotifyPropertyChanged
{
    private StakeInfo _stakeInfo = null;

    public StakeInfo StakeInfo
    {
        get => _stakeInfo;
        set
        {
            if (_stakeInfo != value)
            {
                _stakeInfo = value;
                OnPropertyChanged();
            }
        }
    }

    // TODO: remove this property
    public bool[] Var { get; set; } = new bool[4] { false, false, false, false };

    private string[] _right = Array.Empty<string>();

    private string[] _wrong = Array.Empty<string>();

    /// <summary>
    /// Верные ответы
    /// </summary>
    public string[] Right
    {
        get => _right;
        set
        {
            _right = value;
            OnPropertyChanged();
        }
    }
    /// <summary>
    /// Неверные ответы
    /// </summary>
    public string[] Wrong
    {
        get => _wrong;
        set
        {
            _wrong = value;
            OnPropertyChanged();
        }
    }

    private bool _showExtraRightButtons;

    /// <summary>
    /// Show additional buttons for accepting right answer with different reward.
    /// </summary>
    public bool ShowExtraRightButtons
    {
        get => _showExtraRightButtons;
        set
        {
            if (_showExtraRightButtons != value)
            {
                _showExtraRightButtons = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
