using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SICore;

/// <summary>
/// Defines common data for player and showman.
/// </summary>
public sealed class PersonData : INotifyPropertyChanged
{
    public ICommand SendCatCost { get; set; }

    public ICommand SendFinalStake { get; set; }

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

    /// <summary>
    /// Имя игрока, чей ответ валидируется
    /// </summary>
    public string ValidatorName { get; set; }

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
