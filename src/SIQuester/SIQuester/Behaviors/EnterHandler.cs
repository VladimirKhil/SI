using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace SIQuester.Behaviors
{
    public static class EnterHandler
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
            DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(EnterHandler), new UIPropertyMetadata(false, IsAttachedChanged));

        private static void IsAttachedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var box = (RichTextBox)sender;
            if ((bool)e.NewValue)
            {
                box.PreviewKeyDown += Box_PreviewKeyDown;
                box.PreviewTextInput += Box_TextInput;
            }
            else
            {
                box.PreviewKeyDown -= Box_PreviewKeyDown;
                box.PreviewTextInput -= Box_TextInput;
            }
        }

        private static void Box_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var box = (RichTextBox)sender;
            box.Selection.Text = "";
            InsertText(box, e.Text);
            e.Handled = true;
        }

        private static void InsertText(RichTextBox box, string text)
        {
            if (box.CaretPosition.LogicalDirection == LogicalDirection.Forward)
                box.CaretPosition.InsertTextInRun(text);
            else
            {
                box.CaretPosition.InsertTextInRun(text);
                box.CaretPosition = box.CaretPosition.GetPositionAtOffset(text.Length, LogicalDirection.Forward);
            }
        }

        private static void Box_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var box = (RichTextBox)sender;
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                box.Selection.Text = "";
                InsertText(box, "\r\n");
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Space)
            {
                box.Selection.Text = "";
                InsertText(box, " ");
                e.Handled = true;
            }
        }
    }
}
