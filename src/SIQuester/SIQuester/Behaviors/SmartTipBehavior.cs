using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SIQuester.Behaviors;

/// <summary>
/// Provides tooltip for a TextBlock which text is trimmed.
/// </summary>
public static class SmartTipBehavior
{
    private static readonly DependencyPropertyDescriptor TextDescriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
    private static readonly DependencyPropertyDescriptor WidthDescriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.ActualWidthProperty, typeof(TextBlock));

    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(SmartTipBehavior), new UIPropertyMetadata(false, OnIsAttachedChanged));

    public static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            AddHandlers(textBlock);
            SetToolTip(textBlock, EventArgs.Empty);
        }
        else
        {
            RemoveHandlers(textBlock);
        }
    }

    private static void RemoveHandlers(TextBlock textBlock)
    {
        TextDescriptor.RemoveValueChanged(textBlock, SetToolTip);
        WidthDescriptor.RemoveValueChanged(textBlock, SetToolTip);
    }

    private static void SetToolTip(object? sender, EventArgs e)
    {
        var textBlock = sender as TextBlock;

        var ft = new FormattedText(
            textBlock.Text,
            CultureInfo.CurrentUICulture,
            textBlock.FlowDirection,
            new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
            textBlock.FontSize,
            textBlock.Foreground,
            VisualTreeHelper.GetDpi(textBlock).PixelsPerDip)
        {
            TextAlignment = textBlock.TextAlignment,
            Trimming = TextTrimming.None,
            LineHeight = textBlock.LineHeight
        };
        
        var showTooltip = ft.WidthIncludingTrailingWhitespace > (textBlock.ActualWidth - textBlock.Padding.Left - textBlock.Padding.Right);
        textBlock.ToolTip = showTooltip ? textBlock.Text : null;
    }

    private static void AddHandlers(TextBlock textBlock)
    {
        TextDescriptor.AddValueChanged(textBlock, SetToolTip);
        WidthDescriptor.AddValueChanged(textBlock, SetToolTip);
    }
}
