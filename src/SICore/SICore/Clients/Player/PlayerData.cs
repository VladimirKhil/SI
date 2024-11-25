using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SICore;

/// <summary>
/// Defines player data.
/// </summary>
public sealed class PlayerData : INotifyPropertyChanged
{
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
    /// Defines time stamp when game buttons have been activated.
    /// </summary>
    public DateTimeOffset? TryStartTime { get; set; }

    public event Action? PressButton;
    public event Action? PressNextButton;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPressButton() => PressButton?.Invoke();

    public void OnPressNextButton() => PressNextButton?.Invoke();

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
