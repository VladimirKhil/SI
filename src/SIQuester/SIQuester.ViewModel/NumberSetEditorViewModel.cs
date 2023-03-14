using SIPackages.Core;
using SIPackages.TypeConverters;
using SIQuester.ViewModel.Model;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines a NumberSet editor view model.
/// </summary>
public sealed class NumberSetEditorViewModel : ModelViewBase
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
                UpdateValue();
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
                UpdateValue();
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
                UpdateValue();
            }
        }
    }

    private readonly Action<string> _onValueChanged;

    public NumberSetEditorViewModel(string value, Action<string> onValueChanged)
    {
        _model = NumberSetTypeConverter.ParseNumberSet(value) ?? new NumberSet();
        _onValueChanged = onValueChanged;

        _mode = _model.Minimum == _model.Maximum
            ? _model.Minimum == 0
                ? NumberSetMode.MinimumOrMaximumInRound
                : NumberSetMode.FixedValue
            : _model.Step == _model.Maximum - _model.Minimum
                ? NumberSetMode.Range
                : NumberSetMode.RangeWithStep;
    }

    private void UpdateValue()
    {
        var modelToUse = _mode switch
        {
            NumberSetMode.FixedValue => new NumberSet { Minimum = _model.Minimum, Maximum = _model.Minimum },
            NumberSetMode.MinimumOrMaximumInRound => new NumberSet { Minimum = 0, Maximum = 0 },
            NumberSetMode.Range => CoerceIgnoreStep(new NumberSet { Minimum = _model.Minimum, Maximum = _model.Maximum }),
            _ => CoerceIgnoreStep(_model)
        };

        _onValueChanged(modelToUse.ToString());
    }

    private static NumberSet CoerceIgnoreStep(NumberSet model) =>
        model.Minimum <= model.Maximum ? model : new NumberSet { Minimum = model.Minimum, Maximum = model.Minimum };
}
