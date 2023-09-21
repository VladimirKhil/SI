using SIQuester.Model;
using SIQuester.ViewModel.Contracts.Host;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Utils.Commands;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines SPARD template editor view model.
/// </summary>
public sealed class SpardTemplateViewModel : ModelViewBase
{
    private static string UnicodeDataFormat = "UnicodeText";

    public string Name { get; private set; }

    public bool NonStandartOnly { get; set; }

    public Dictionary<string, EditAlias> Aliases { get; private set; }

    private bool _enabled = true;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                OnPropertyChanged();
            }
        }
    }

    private string _transform = "";

    public string Transform
    {
        get => _transform;
        set { if (_transform != value) { _transform = value; OnPropertyChanged(); } }
    }

    private bool _canChange;

    public bool CanChange
    {
        get => _canChange;
        set { if (_canChange != value) { _canChange = value; OnPropertyChanged(); } }
    }

    private ObservableCollection<string> _variants;

    public ObservableCollection<string> Variants
    {
        get => _variants;
        set { _variants = value; OnPropertyChanged(); }
    }

    public ICommand Cut { get; private set; }

    public ICommand Copy { get; private set; }

    public ICommand Paste { get; private set; }

    public ICommand InsertAlias { get; private set; }

    public ICommand InsertOptional { get; private set; }

    public ICommand ChangeTemplate { get; private set; }

    public event Action<string>? AliasInserted;

    public event Action? OptionalInserted;

    public SpardTemplateViewModel(string name, IClipboardService clipboardService)
    {
        Name = name;
        Aliases = new Dictionary<string, EditAlias>();

        Cut = new SimpleCommand(
            arg =>
            {
                if (_transform != null)
                {
                    clipboardService.SetData(UnicodeDataFormat, _transform);
                    Transform = "";
                }
            });

        Copy = new SimpleCommand(
            arg =>
            {
                if (_transform != null)
                {
                    clipboardService.SetData(UnicodeDataFormat, _transform);
                }
            });

        Paste = new SimpleCommand(
            arg =>
            {
                Transform = (string)clipboardService.GetData(UnicodeDataFormat);
            });

        InsertAlias = new SimpleCommand(
            arg =>
            {
                AliasInserted?.Invoke(arg.ToString());
            });

        InsertOptional = new SimpleCommand(
            arg =>
            {
                OptionalInserted?.Invoke();
            });

        ChangeTemplate = new SimpleCommand(
            template =>
            {
                Transform = template.ToString();
            });
    }
}
