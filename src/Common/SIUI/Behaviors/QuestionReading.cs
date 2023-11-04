using SIUI.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SIUI.Behaviors;

/// <summary>
/// Enables text reading effects ("karaoke").
/// </summary>
public static class QuestionReading
{
    private static readonly DependencyPropertyDescriptor TextProperty = DependencyPropertyDescriptor.FromProperty(
        TextBlock.TextProperty,
        typeof(TextBlock));

    public static double GetTextSpeed(DependencyObject obj) => (double)obj.GetValue(TextSpeedProperty);

    public static void SetTextSpeed(DependencyObject obj, double value) => obj.SetValue(TextSpeedProperty, value);

    public static readonly DependencyProperty TextSpeedProperty =
        DependencyProperty.RegisterAttached("TextSpeed", typeof(double), typeof(QuestionReading), new PropertyMetadata(0.0, OnTextSpeedChanged));

    public static void OnTextSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var textBlock = (TextBlock)d;

        if ((double)e.NewValue > 0)
        {
            textBlock.Loaded += OnLoaded;
            textBlock.DataContextChanged += TextBlock_DataContextChanged;
            TextProperty.AddValueChanged(textBlock, TextBlock_TextChanged);
        }
        else
        {
            textBlock.Loaded -= OnLoaded;
            textBlock.DataContextChanged -= TextBlock_DataContextChanged;
            TextProperty.RemoveValueChanged(textBlock, TextBlock_TextChanged);
        }
    }

    private static void TextBlock_TextChanged(object? sender, EventArgs e)
    {
        if (sender == null)
        {
            return;
        }

        var textBlock = (TextBlock)sender;
        AnimateTextReading(textBlock);
    }

    private static void TextBlock_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var textBlock = (TextBlock)sender;
        AnimateTextReading(textBlock);
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        var textBlock = (TextBlock)sender;
        AnimateTextReading(textBlock);
    }

    private static void AnimateTextReading(TextBlock textBlock)
    {
        var textSpeed = GetTextSpeed(textBlock);

        textBlock.TextEffects[0].BeginAnimation(
            TextEffect.PositionCountProperty,
            new Int32Animation
            {
                From = 0,
                To = textBlock.Text.Length,
                Duration = new Duration(TimeSpan.FromSeconds(textBlock.Text.Length * textSpeed))
            });
    }

    public static bool GetIsAttachedPartial(DependencyObject obj) => (bool)obj.GetValue(IsAttachedPartialProperty);

    public static void SetIsAttachedPartial(DependencyObject obj, bool value) => obj.SetValue(IsAttachedPartialProperty, value);

    // Using a DependencyProperty as the backing store for IsAttachedPartial.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsAttachedPartialProperty =
        DependencyProperty.RegisterAttached("IsAttachedPartial", typeof(bool), typeof(QuestionReading), new PropertyMetadata(false, OnIsAttachedPartialChanged));

    // Пока сделано синглтоном
    private static int CurrentTarget;

    public static void OnIsAttachedPartialChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var textBlock = (TextBlock)d;
        var tableInfoViewModel = (TableInfoViewModel)textBlock.DataContext;

        if (!(bool)e.NewValue)
        {
            return;
        }

        if (tableInfoViewModel.TextSpeed < double.Epsilon)
        {
            return;
        }

        void handler(object? sender, PropertyChangedEventArgs e2)
        {
            if (e2.PropertyName == nameof(TableInfoViewModel.TextLength))
            {
                UpdateAnimation(textBlock, tableInfoViewModel);
            }
        }

        tableInfoViewModel.PropertyChanged += handler;

        textBlock.Loaded += (sender, e2) =>
        {
            var animation = new Int32Animation
            {
                To = tableInfoViewModel.TextLength,
                Duration = new Duration(TimeSpan.FromSeconds((tableInfoViewModel.TextLength - CurrentTarget) * tableInfoViewModel.TextSpeed))
            };

            textBlock.TextEffects[0].BeginAnimation(TextEffect.PositionCountProperty, animation);
            CurrentTarget = tableInfoViewModel.TextLength;
        };

        textBlock.Unloaded += (sender, e2) =>
        {
            tableInfoViewModel.PropertyChanged -= handler;
        };

        CurrentTarget = 0;
    }

    private static void UpdateAnimation(TextBlock textBlock, TableInfoViewModel tableInfoViewModel)
    {
        if (System.Windows.Threading.Dispatcher.CurrentDispatcher != textBlock.Dispatcher)
        {
            textBlock.Dispatcher.Invoke(UpdateAnimation, textBlock, tableInfoViewModel);
            return;
        }

        if (tableInfoViewModel.TextLength > CurrentTarget)
        {
            var animation = new Int32Animation
            {
                To = tableInfoViewModel.TextLength,
                Duration = new Duration(TimeSpan.FromSeconds((tableInfoViewModel.TextLength - CurrentTarget) * tableInfoViewModel.TextSpeed))
            };

            textBlock.TextEffects[0].BeginAnimation(TextEffect.PositionCountProperty, animation);

            CurrentTarget = tableInfoViewModel.TextLength;
        }
    }
}
