using System.Text;
using System.Windows;
using System.Windows.Controls;
using System;

namespace SIGame;

/// <summary>
/// Defines a text editor which accepts only numbers.
/// </summary>
public sealed class NumericTextBox : TextBox
{
    /// <summary>
    /// Максимально возможное вводимое значение
    /// </summary>
    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set { SetValue(MaximumProperty, value); }
    }

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register("Maximum", typeof(int), typeof(NumericTextBox), new UIPropertyMetadata(int.MaxValue));
    
    /// <summary>
    /// Минимально возможное вводимое значение
    /// </summary>
    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set { SetValue(MinimumProperty, value); }
    }

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register("Minimum", typeof(int), typeof(NumericTextBox), new UIPropertyMetadata(0));
    
    /// <summary>
    /// Величина шага между значениями
    /// </summary>
    public int Step
    {
        get => (int)GetValue(StepProperty);
        set { SetValue(StepProperty, value); }
    }

    public static readonly DependencyProperty StepProperty =
        DependencyProperty.Register("Step", typeof(int), typeof(NumericTextBox), new UIPropertyMetadata(1));

    public NumericTextBox()
    {
        PreviewTextInput += new System.Windows.Input.TextCompositionEventHandler(NumericTextBox_PreviewTextInput);
        LostFocus += new RoutedEventHandler(NumericTextBox_LostFocus);

        DataObject.AddPastingHandler(this, TextBoxPastingEventHandler);
    }

    private void NumericTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;

        if (!int.TryParse(textBox.Text, out var value))
        {
            textBox.Text = Minimum.ToString();
            return;
        }

        if (value < Minimum)
        {
            textBox.Text = Minimum.ToString();
        }
        else if (value > Maximum)
        {
            textBox.Text = Maximum.ToString();
        }
        else
        {
            var rem = (value - Minimum) % Step;

            if (rem > 0)
            {
                textBox.Text = (value - rem).ToString();
            }
        }
    }

    private void NumericTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        try
        {
            if (SelectionStart == 0 && e.Text == "-" && (Text.Length == 0 || Text[0] != '-'))
            {
                return;
            }
            
            if (!int.TryParse(e.Text, out _))
            {
                e.Handled = true;
                return;
            }

            var futureText = new StringBuilder();

            if (SelectionStart > 0)
            {
                futureText.Append(Text.AsSpan(0, SelectionStart));
            }

            futureText.Append(e.Text);

            if (SelectionStart + SelectionLength < Text.Length)
            {
                futureText.Append(Text.AsSpan(SelectionStart + SelectionLength));
            }

            var fText = futureText.ToString();
            var futureValue = int.Parse(fText);

            if (futureValue > Maximum)
            {
                e.Handled = true;
            }
            else if (fText.StartsWith("0"))
            {
                e.Handled = true;
                Text = futureValue.ToString();
                SelectionStart = Text.Length;
            }
        }
        catch
        {
            e.Handled = true;
        }
    }

    private void TextBoxPastingEventHandler(object sender, DataObjectPastingEventArgs e)
    {
        var clipboard = e.DataObject.GetData(typeof(string)) as string;

        if (!int.TryParse(clipboard, out _))
        {
            e.CancelCommand();
            e.Handled = true;
        }
    }
}
