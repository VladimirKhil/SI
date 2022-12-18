using SIData;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIGame.ViewModel;

/// <summary>
/// Настройка интервала времени
/// </summary>
public sealed class TimeSetting : ICloneable, INotifyPropertyChanged
{
    private readonly TimeSettings _timeSettings;

    private readonly TimeSettingsTypes _timeSettingsType;

    /// <summary>
    /// Название настройки
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Значение настройки
    /// </summary>
    public int Value
    {
        get => _timeSettings.All[_timeSettingsType];
        set { _timeSettings.All[_timeSettingsType] = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Максимально возможное значение
    /// </summary>
    public int Maximum { get; set; }

    /// <summary>
    /// Значение по умолчанию
    /// </summary>
    public int DefaultValue { get; private set; }

    public TimeSetting(string name, TimeSettings timeSettings, TimeSettingsTypes timeSettingsType, int value, int maximum)
    {
        Name = name;
        _timeSettings = timeSettings;
        _timeSettingsType = timeSettingsType;
        Maximum = maximum;

        DefaultValue = value;
    }

    public object Clone() => new TimeSetting(Name, _timeSettings, _timeSettingsType, DefaultValue, Maximum);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler? PropertyChanged;
}
