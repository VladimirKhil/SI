using SIData;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace SIGame.ViewModel;

/// <summary>
/// Игровой аккаунт
/// </summary>
public sealed class GameAccount : SimpleAccount<Account>
{
    private AccountTypes _accountType = AccountTypes.Human;

    public AccountTypes AccountType
    {
        get => _accountType;
        set
        {
            if (_accountType != value)
            {
                _accountType = value;
                OnPropertyChanged();
            }
        }
    }

    [XmlIgnore]
    public bool IsCreator => SelectedAccount == _settings.Human;

    [XmlIgnore]
    public GameSettingsViewModel GameSettings => _settings;

    private readonly GameSettingsViewModel _settings;

    public GameAccount(GameSettingsViewModel settings) => _settings = settings;

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(SelectedAccount))
        {
            OnPropertyChanged(nameof(IsCreator));
        }
    }
}
