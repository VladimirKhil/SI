using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SICore;

/// <summary>
/// Defines a showman data.
/// </summary>
public sealed class ShowmanData : INotifyPropertyChanged
{
    private ICommand? _changeSums;

    /// <summary>
    /// Change players score.
    /// </summary>
    public ICommand? ChangeSums
    {
        get => _changeSums;
        set
        {
            if (_changeSums != value)
            {
                _changeSums = value;
                OnPropertyChanged();
            }
        }
    }

    private ICommand? _changeActivePlayer;

    /// <summary>
    /// Change active player.
    /// </summary>
    public ICommand? ChangeActivePlayer
    {
        get => _changeActivePlayer;
        set
        {
            if (_changeActivePlayer != value)
            {
                _changeActivePlayer = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Послать сообщение об изменении суммы
    /// </summary>
    public ICommand ChangeSums2 { get; set; }

    private ICommand _manage;

    /// <summary>
    /// Управление игрой
    /// </summary>
    public ICommand Manage
    {
        get => _manage;
        set
        {
            if (_manage != value)
            {
                _manage = value;
                OnPropertyChanged();
            }
        }
    }

    private CustomCommand? _manageTable;

    /// <summary>
    /// Manage game table command.
    /// </summary>
    public CustomCommand? ManageTable
    {
        get => _manageTable;
        set
        {
            if (_manageTable != value)
            {
                _manageTable = value;
                OnPropertyChanged();
            }
        }
    }

    private Pair _selectedPlayer = null;

    /// <summary>
    /// Выбранный игрок
    /// </summary>
    public Pair SelectedPlayer
    {
        get => _selectedPlayer;
        set
        {
            if (_selectedPlayer != value)
            {
                _selectedPlayer = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
