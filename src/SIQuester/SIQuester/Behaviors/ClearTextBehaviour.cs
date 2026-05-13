using SIQuester.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace SIQuester.Behaviors;

public static class ClearTextBehaviour
{
    public static TextBox GetTarget(DependencyObject obj) => (TextBox)obj.GetValue(TargetProperty);

    public static void SetTarget(DependencyObject obj, TextBox value) => obj.SetValue(TargetProperty, value);

    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.RegisterAttached("Target", typeof(TextBox), typeof(ClearTextBehaviour), new PropertyMetadata(null, IsAttachedChanged));

    private static void IsAttachedChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (e.OldValue is TextBox oldTextBox)
        {
            button.Click -= (s, e) =>
            {
                if (oldTextBox.DataContext is ShowmanCommentsViewModel showmanCommentsViewModel)
                {
                    showmanCommentsViewModel.Clear();
                }
                else if (oldTextBox.DataContext is CommentsViewModel commentsViewModel)
                {
                    commentsViewModel.Clear();
                }
                else
                {
                    oldTextBox.Clear();
                }
            };
        }

        if (e.NewValue is TextBox textBox)
        {
            button.Click += (s, e) =>
            {
                if (textBox.DataContext is ShowmanCommentsViewModel showmanCommentsViewModel)
                {
                    showmanCommentsViewModel.Clear();
                }
                else if (textBox.DataContext is CommentsViewModel commentsViewModel)
                {
                    commentsViewModel.Clear();
                }
                else
                {
                    textBox.Clear();
                }
            };
        }
    }
}
