using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIUI.ViewModel;

/// <summary>
/// Defines a displayable item view model.
/// </summary>
public sealed class ItemViewModel : INotifyPropertyChanged
{
    private ItemState _state = ItemState.Normal;

    /// <summary>
    /// Item state.
    /// </summary>
    public ItemState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isVisible = false;

    /// <summary>
    /// Is item visible.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Item label.
    /// </summary>
    public string Label { get; set; } = "";

    private ContentViewModel _content = new(ContentType.Void, "");

    /// <summary>
    /// Item content.
    /// </summary>
    public ContentViewModel Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
