using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows.Input;
using Utils.Commands;

namespace SIUI.ViewModel;

/// <summary>
/// Defines well-known displayable content types.
/// </summary>
public enum ContentType
{
    Void,
    Text,
    Image,
    Video,
    Html,
}

/// <summary>
/// Defines table item state.
/// </summary>
public enum ItemState
{
    Normal,
    Active,
    Right,
    Wrong
}

/// <summary>
/// Defines game table layout mode.
/// </summary>
public enum LayoutMode
{
    Simple,
    AnswerOptions,
}

/// <summary>
/// Defines a game table stage.
/// </summary>
[DataContract]
public enum TableStage
{
    [EnumMember]
    Void,
    [EnumMember]
    Sign,
    [EnumMember]
    GameThemes,
    [EnumMember]
    Round,
    [EnumMember]
    RoundTable,
    [EnumMember]
    Theme,
    [EnumMember]
    QuestionPrice,
    [EnumMember]
    Question,
    [EnumMember]
    Special,
    [EnumMember]
    Final
}

/// <summary>
/// Defines content item view model.
/// </summary>
public sealed record ContentViewModel(ContentType Type, string Value);

/// <summary>
/// Defines a displayable item view model.
/// </summary>
public sealed class ItemViewModel : INotifyPropertyChanged
{
    private ItemState _state = ItemState.Normal;
    private string _label = "";
    private ContentViewModel _content = new(ContentType.Void, "");

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

    public string Label
    {
        get => _label;
        set
        {
            if (_label != value)
            {
                _label = value;
                OnPropertyChanged();
            }
        }
    }

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

/// <summary>
/// Defines answer options view model.
/// </summary>
public sealed class AnswerOptionsViewModel
{
    public ItemViewModel[] Options { get; set; } = Array.Empty<ItemViewModel>();
}

/// <summary>
/// Defines question information view model.
/// </summary>
public sealed class QuestionInfoViewModel : INotifyPropertyChanged
{
    public const int InvalidPrice = -1;

    private int _price = InvalidPrice;

    public int Price
    {
        get => _price;
        set
        {
            if (_price != value)
            {
                _price = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// Defines theme information view model.
/// </summary>
public sealed class ThemeInfoViewModel : INotifyPropertyChanged
{
    private string _name = "";

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<QuestionInfoViewModel> Questions { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// Defines game table view model.
/// </summary>
public sealed class TableInfoViewModel : INotifyPropertyChanged
{
    private TableStage _tStage = TableStage.Void;
    private string _text = "";
    private LayoutMode _layoutMode = LayoutMode.Simple;

    public TableStage TStage
    {
        get => _tStage;
        set
        {
            if (_tStage != value)
            {
                _tStage = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged();
            }
        }
    }

    public LayoutMode LayoutMode
    {
        get => _layoutMode;
        set
        {
            if (_layoutMode != value)
            {
                _layoutMode = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<ThemeInfoViewModel> RoundInfo { get; } = new();

    public AnswerOptionsViewModel AnswerOptions { get; } = new();

    public ICommand SelectQuestion { get; }

    public ICommand SelectTheme { get; }

    public ICommand SelectAnswer { get; }

    public event Action<QuestionInfoViewModel>? QuestionSelected;

    public event Action<ThemeInfoViewModel>? ThemeSelected;

    public event Action<ItemViewModel>? AnswerSelected;

    public TableInfoViewModel()
    {
        SelectQuestion = new SimpleCommand(arg =>
        {
            if (arg is QuestionInfoViewModel questionInfo)
            {
                QuestionSelected?.Invoke(questionInfo);
            }
        });

        SelectTheme = new SimpleCommand(arg =>
        {
            if (arg is ThemeInfoViewModel themeInfo)
            {
                ThemeSelected?.Invoke(themeInfo);
            }
        });

        SelectAnswer = new SimpleCommand(arg =>
        {
            if (arg is ItemViewModel itemViewModel)
            {
                AnswerSelected?.Invoke(itemViewModel);
            }
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
