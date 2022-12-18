using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIQuester;

/// <summary>
/// Текстовое поле, поддерживающее только ввод цифр
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

    // Using a DependencyProperty as the backing store for Maximum.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register("Maximum", typeof(int), typeof(NumericTextBox), new UIPropertyMetadata(Int32.MaxValue));

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
        PreviewTextInput += NumericTextBox_PreviewTextInput;
        LostFocus += NumericTextBox_LostFocus;

        DataObject.AddPastingHandler(this, TextBoxPastingEventHandler);
    }

    private void NumericTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = (TextBox)sender;

        try
        {
            if (!int.TryParse(textBox.Text, out int value))
            {
                if (0 >= Minimum && 0 <= Maximum)
                {
                    textBox.Text = "0";
                }
                else
                {
                    textBox.Text = Minimum.ToString();
                }

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
        catch
        {
            textBox.Text = Minimum.ToString();
        }
    }

    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        try
        {
            if (SelectionStart == 0 && e.Text == "-" && (Text.Length == 0 || Text[0] != '-'))
            {
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

            if (!int.TryParse(fText, out int futureValue) || futureValue > Maximum)
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
        var clipboard = (string)e.DataObject.GetData(typeof(string));

        try
        {
            if (!int.TryParse(clipboard, out int res))
            {
                e.CancelCommand();
                e.Handled = true;
            }
        }
        catch
        {
            e.CancelCommand();
            e.Handled = true;
        }
    }
}
