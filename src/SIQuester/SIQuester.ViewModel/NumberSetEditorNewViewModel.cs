using SIPackages.Core;
using SIQuester.ViewModel.Model;

namespace SIQuester.ViewModel;

/// <summary>
/// Defines a NumberSet editor view model.
/// </summary>
public sealed class NumberSetEditorNewViewModel : ModelViewBase
{
    private readonly NumberSet _model;

    private NumberSetMode _mode = NumberSetMode.FixedValue;

    public NumberSetMode Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
            {
                var oldValue = _mode;
                _mode = value;
                OnPropertyChanged(oldValue);
            }
        }
    }

    public int Minimum
    {
        get => _model.Minimum;
        set
        {
            if (_model.Minimum != value)
            {
                var oldValue = _model.Minimum;
                _model.Minimum = value;
                OnPropertyChanged(oldValue);
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
                var oldValue = _model.Maximum;
                _model.Maximum = value;
                OnPropertyChanged(oldValue);
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
                var oldValue = _model.Step;
                
                if (value >= 0)
                {
                    _model.Step = value;
                }

                OnPropertyChanged(oldValue);
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
