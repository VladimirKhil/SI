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

    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    // Using a DependencyProperty as the backing store for IsAttached.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(QuestionReading), new PropertyMetadata(false, OnIsAttachedChanged));

    public static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var textBlock = (TextBlock)d;
        var tableInfoViewModel = (TableInfoViewModel)textBlock.DataContext;

        if ((bool)e.NewValue)
        {
            if (tableInfoViewModel.TextSpeed < double.Epsilon)
            {
                return;
            }

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
        var tableInfoViewModel = (TableInfoViewModel)textBlock.DataContext;

        textBlock.TextEffects[0].BeginAnimation(
            TextEffect.PositionCountProperty,
            new Int32Animation
            {
                From = 0,
                To = tableInfoViewModel.Text.Length,
                Duration = new Duration(TimeSpan.FromSeconds(tableInfoViewModel.Text.Length * tableInfoViewModel.TextSpeed))
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
