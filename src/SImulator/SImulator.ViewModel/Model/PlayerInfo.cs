using SIUI.ViewModel;
using System.Runtime.Serialization;

namespace SImulator.ViewModel.Model;

/// <summary>
/// Информация об игроке
/// </summary>
[DataContract]
public sealed class PlayerInfo : SimplePlayerInfo
{
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
    /// Момент блокировки кнопки
    /// </summary>
    internal DateTime? BlockedTime { get; set; }

    private bool _isRegistered;

    /// <summary>
    /// Зарегистрирован ли игрок (для веб-доступа к кнопкам)
    /// </summary>
    public bool IsRegistered
    {
        get => _isRegistered;
        set { if (_isRegistered != value) { _isRegistered = value; OnPropertyChanged(); } }
    }

    private bool _waitForRegistration;

    public bool WaitForRegistration
    {
        get => _waitForRegistration;
        set { if (_waitForRegistration != value) { _waitForRegistration = value; OnPropertyChanged(); } }
    }
}
