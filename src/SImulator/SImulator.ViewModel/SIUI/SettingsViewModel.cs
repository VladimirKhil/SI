using SIUI.ViewModel.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIUI.ViewModel;

/// <summary>
/// Defines UI settings view model.
/// </summary>
public sealed class SettingsViewModel : INotifyPropertyChanged
{
    public Settings Model { get; }

    public string TableFontFamily
    {
        get => Model.TableFontFamily;
        set
        {
            if (Model.TableFontFamily != value)
            {
                Model.TableFontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    public double QuestionLineSpacing
    {
        get => Model.QuestionLineSpacing;
        set
        {
            if (Model.QuestionLineSpacing != value)
            {
                Model.QuestionLineSpacing = value;
                OnPropertyChanged();
            }
        }
    }

    public SettingsViewModel()
        : this(new Settings())
    {
    }

    public SettingsViewModel(Settings settings) => Model = settings;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
