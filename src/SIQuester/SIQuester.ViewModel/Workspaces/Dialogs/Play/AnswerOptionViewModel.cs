using SIEngine.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIQuester.ViewModel.Workspaces.Dialogs.Play;

/// <summary>
/// Defines answer option view model.
/// </summary>
/// <param name="Label">Option label.</param>
/// <param name="Content">Option content.</param>
public sealed record AnswerOptionViewModel(string Label, ContentInfo Content) : INotifyPropertyChanged
{
	private bool _isSelected;

    /// <summary>
    /// Is option selected.
    /// </summary>
	public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
