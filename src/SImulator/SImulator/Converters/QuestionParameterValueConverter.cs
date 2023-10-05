using SImulator.Properties;
using SIPackages;
using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace SImulator.Converters;

/// <summary>
/// Produces localized version of question parameter value.
/// </summary>
public sealed class QuestionParameterValueConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var stepParameterPair = (KeyValuePair<string, StepParameter>)value;

        if (stepParameterPair.Key == null)
        {
            return null;
        }

        var stepParameter = stepParameterPair.Value;

        return stepParameterPair.Key switch
        {
            QuestionParameterNames.Theme => stepParameter.SimpleValue,
            QuestionParameterNames.Price => PrintNumberSet(stepParameter.NumberSetValue),
            QuestionParameterNames.SelectionMode => stepParameter.SimpleValue == StepParameterValues.SetAnswererSelect_Any
                ? Resources.SelectAny
                : Resources.SelectAnyExceptCurrent,
            _ => stepParameter.SimpleValue,
        };
    }

    private static string? PrintNumberSet(NumberSet? numberSet)
    {
        if (numberSet == null)
        {
            return null;
        }

        if (numberSet.Maximum == 0)
        {
            return Resources.PriceMinMaxRound;
        }

        if (numberSet.Maximum == numberSet.Minimum)
        {
            return numberSet.Maximum.ToString();
        }

        if (numberSet.Maximum - numberSet.Minimum == numberSet.Step)
        {
            return string.Format(Resources.PriceOr, numberSet.Minimum, numberSet.Maximum);
        }

        return string.Format(Resources.PriceFromToStep, numberSet.Minimum, numberSet.Maximum, numberSet.Step);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
