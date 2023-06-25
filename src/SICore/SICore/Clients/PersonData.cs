using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SICore;

/// <summary>
/// Defines common data for player and showman.
/// </summary>
public sealed class PersonData : INotifyPropertyChanged
{
    public CustomCommand SendPass { get; set; }

    public CustomCommand SendStake { get; set; }

    public CustomCommand SendVabank { get; set; }

    public CustomCommand SendNominal { get; set; }

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

    internal bool[] Var { get; set; } = new bool[4] { false, false, false, false };

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

    private string _answer = "";

    /// <summary>
    /// Ответ игрока
    /// </summary>
    public string Answer
    {
        get => _answer;
        set { _answer = value; OnPropertyChanged(); }
    }

    private bool _areAnswersShown = true;

    public bool AreAnswersShown
    {
        get => _areAnswersShown;
        set
        {
            if (_areAnswersShown != value)
            {
                _areAnswersShown = value;
                OnPropertyChanged();
            }
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


    private ICommand? _isRight = null;

    public ICommand? IsRight
    {
        get => _isRight;
        set { _isRight = value; OnPropertyChanged(); }
    }

    private ICommand _isWrong = null;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand IsWrong
    {
        get => _isWrong;
        set { _isWrong = value; OnPropertyChanged(); }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
