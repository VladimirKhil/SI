using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SICore;

/// <summary>
/// Defines player data.
/// </summary>
public sealed class PlayerData : INotifyPropertyChanged
{
    private CustomCommand _pressGameButton;

    public CustomCommand PressGameButton
    {
        get => _pressGameButton;
        set
        {
            if (_pressGameButton != value)
            {
                _pressGameButton = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand SendAnswerVersion { get; set; }

    public ICommand SendAnswer { get; set; }

    private CustomCommand _apellate;

    public CustomCommand Apellate
    {
        get => _apellate;
        set
        {
            if (_apellate != value)
            {
                _apellate = value;
                OnPropertyChanged();
            }
        }
    }

    private CustomCommand _pass;

    public CustomCommand Pass
    {
        get => _pass;
        set
        {
            if (_pass != value)
            {
                _pass = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Знает ли ответ
    /// </summary>
    internal bool KnowsAnswer { get; set; } = false;

    /// <summary>
    /// Уверен ли в ответе
    /// </summary>
    internal bool IsSure { get; set; } = false;

    /// <summary>
    /// Готов ли жать на кнопку
    /// </summary>
    internal bool ReadyToPress { get; set; } = false;

    private int _realBrave = 0;

    /// <summary>
    /// Текущая величина смелости
    /// </summary>
    internal int RealBrave { get => _realBrave; set { _realBrave = Math.Max(0, value); } }

    /// <summary>
    /// Скорость изменения смелости
    /// </summary>
    internal int DeltaBrave { get; set; } = 0;

    /// <summary>
    /// Текущая скорость реакции
    /// </summary>
    internal int RealSpeed { get; set; } = 0;

    /// <summary>
    /// Продолжается ли чтение вопроса
    /// </summary>
    internal bool IsQuestionInProgress { get; set; }

    /// <summary>
    /// Отчёт об игре
    /// </summary>
    public SIReport Report { get; set; } = new SIReport();

    private int _apellationCount = int.MaxValue;

    public int ApellationCount
    {
        get => _apellationCount;
        set { _apellationCount = value; OnPropertyChanged(); }
    }

    private bool _myTry;

    /// <summary>
    /// Можно жать на кнопку (чтобы при игре без фальстартов компьютерные игроки соображали помедленнее)
    /// </summary>
    public bool MyTry
    {
        get => _myTry;
        set { _myTry = value; OnPropertyChanged(); }
    }

    public event Action? PressButton;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPressButton() => PressButton?.Invoke();

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
