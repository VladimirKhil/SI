using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace SImulator.Behaviors
{
    public static class NumericBehavior
    {
        public static bool GetIsAttached(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsAttachedProperty);
        }

        public static void SetIsAttached(DependencyObject obj, bool value)
        {
            obj.SetValue(IsAttachedProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsAttached.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsAttachedProperty =
            DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(NumericBehavior), new PropertyMetadata(false, IsAttachedChanged));

        private static void IsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TextBox)d).PreviewTextInput += NumericBehavior_PreviewTextInput;
            DataObject.AddPastingHandler(d, TextBoxPastingEventHandler);
        }

        private static void NumericBehavior_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            try
            {
                var textBox = (TextBox)sender;

                if (textBox.SelectionStart == 0 && e.Text == "-" && (textBox.Text.Length == 0 || textBox.Text[0] != '-'))
                    return;

                Convert.ToInt32(e.Text);

                var futureText = new StringBuilder();
                if (textBox.SelectionStart > 0)
                    futureText.Append(textBox.Text.Substring(0, textBox.SelectionStart));
                futureText.Append(e.Text);
                if (textBox.SelectionStart + textBox.SelectionLength < textBox.Text.Length)
                    futureText.Append(textBox.Text.Substring(textBox.SelectionStart + textBox.SelectionLength));

                var fText = futureText.ToString();
                var futureValue = int.Parse(fText);
                if (fText.StartsWith("0"))
                {
                    e.Handled = true;
                    textBox.Text = futureValue.ToString();
                    textBox.SelectionStart = textBox.Text.Length;
                }
            }
            catch
            {
                e.Handled = true;
            }
        }

        private static void TextBoxPastingEventHandler(object sender, DataObjectPastingEventArgs e)
        {
            var clipboard = e.DataObject.GetData(typeof(string)) as string;
            try
            {
                Convert.ToInt32(clipboard);
            }
            catch
            {
                e.CancelCommand();
                e.Handled = true;
            }
        }
    }
}
