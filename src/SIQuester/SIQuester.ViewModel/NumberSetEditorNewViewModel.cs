using SIPackages.Core;
using SIQuester.ViewModel.Model;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines a NumberSet editor view model.
/// </summary>
public sealed class NumberSetEditorNewViewModel : ModelViewBase
{
    private NumberSetMode _mode = NumberSetMode.FixedValue;

    public NumberSetMode Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
            {
                _mode = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _useStep = false;

    public bool UseStep
    {
        get => _useStep;
        set
        {
            if (_useStep != value)
            {
                _useStep = value;
                OnPropertyChanged();
            }
        }
    }

    private readonly NumberSet _model;

    public int Minimum
    {
        get => _model.Minimum;
        set
        {
            if (_model.Minimum != value)
            {
                _model.Minimum = value;
                OnPropertyChanged();
            }
        }
    }

    public int Maximum
    {
        get => _model.Maximum;
        set
        {
            if (_model.Maximum != value)
            {
                _model.Maximum = value;
                OnPropertyChanged();
            }
        }
    }

    public int Step
    {
        get => _model.Step;
        set
        {
            if (_model.Step != value)
            {
                _model.Step = value;
                OnPropertyChanged();
            }
        }
    }

    public NumberSetEditorNewViewModel(NumberSet model)
    {
        _model = model;

        _mode = _model.Minimum == _model.Maximum
            ? _model.Minimum == 0
                ? NumberSetMode.MinimumOrMaximumInRound
                : NumberSetMode.FixedValue
            : _model.Step == _model.Maximum - _model.Minimum
                ? NumberSetMode.Range
                : NumberSetMode.RangeWithStep;
    }
}
