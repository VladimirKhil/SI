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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
