using SIData;

namespace SIGame.ViewModel;

/// <summary>
/// Аккаунт живого игрока
/// </summary>
public sealed class HumanAccount : Account
{
    private DateTime? _birthDate;

    /// <summary>
    /// Дата рождения
    /// </summary>
    public DateTime? BirthDate
    {
        get => _birthDate;
        set
        {
            if (_birthDate != value)
            {
                _birthDate = value;
                OnPropertyChanged();
            }
        }
    }

    public HumanAccount() => IsHuman = true;

    public HumanAccount(HumanAccount account)
        : base(account)
    {
        IsHuman = true;
        _birthDate = account._birthDate;
    }
}
