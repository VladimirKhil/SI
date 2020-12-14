using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Media;
using System.Globalization;

namespace SIUI.Behaviors
{
    /// <summary>
    /// Поведение, изменяющее размер шрифта текста до максимальной величины, при которой он целиком умещается на экране
    /// </summary>
    public static class FillManager
    {
        private static readonly DependencyPropertyDescriptor TextDescriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
        private static readonly DependencyPropertyDescriptor FontFamilyDescriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.FontFamilyProperty, typeof(TextBlock));

        public static bool GetFill(DependencyObject obj)
        {
            return (bool)obj.GetValue(FillProperty);
        }

        public static void SetFill(DependencyObject obj, bool value)
        {
            obj.SetValue(FillProperty, value);
        }

        // Using a DependencyProperty as the backing store for Fill.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.RegisterAttached("Fill", typeof(bool), typeof(FillManager), new UIPropertyMetadata(false, OnFillChanged));

        public static void OnFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is TextBlock textBlock))
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                textBlock.Loaded += TextBlock_Loaded;
                textBlock.Unloaded += TextBlock_Unloaded;
            }
            else
            {
                textBlock.Loaded -= TextBlock_Loaded;
                textBlock.Unloaded -= TextBlock_Unloaded;
            }
        }

        public static FrameworkElement GetParent(DependencyObject obj)
        {
            return (FrameworkElement)obj.GetValue(ParentProperty);
        }

        public static void SetParent(DependencyObject obj, FrameworkElement value)
        {
            obj.SetValue(ParentProperty, value);
        }

        // Using a DependencyProperty as the backing store for Parent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParentProperty =
            DependencyProperty.RegisterAttached("Parent", typeof(FrameworkElement), typeof(FillManager), new UIPropertyMetadata(null));
        
        public static SizeChangedEventHandler GetHandler(DependencyObject obj)
        {
            return (SizeChangedEventHandler)obj.GetValue(HandlerProperty);
        }

        public static void SetHandler(DependencyObject obj, SizeChangedEventHandler value)
        {
            obj.SetValue(HandlerProperty, value);
        }

        // Using a DependencyProperty as the backing store for Handler.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HandlerProperty =
            DependencyProperty.RegisterAttached("Handler", typeof(SizeChangedEventHandler), typeof(FillManager), new UIPropertyMetadata(null));

        private static void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            var textBlock = (TextBlock)sender;

            if (!(VisualTreeHelper.GetParent(textBlock) is FrameworkElement parent))
            {
                return;
            }

            SetParent(textBlock, parent);

            void handler(object sender2, SizeChangedEventArgs e2) => MeasureFontSize(sender, EventArgs.Empty);
            SetHandler(textBlock, handler);
            
            parent.SizeChanged += handler;
            MeasureFontSize(sender, EventArgs.Empty);

            TextDescriptor.AddValueChanged(textBlock, MeasureFontSize);
            FontFamilyDescriptor.AddValueChanged(textBlock, MeasureFontSize);
        }

        private static void TextBlock_Unloaded(object sender, RoutedEventArgs e)
        {
            var textBlock = (TextBlock)sender;
            var parent = GetParent(textBlock);

            if (parent != null)
            {
                parent.SizeChanged -= GetHandler(textBlock);
                textBlock.ClearValue(ParentProperty);
            }

            textBlock.ClearValue(HandlerProperty);

            TextDescriptor.RemoveValueChanged(textBlock, MeasureFontSize);
            FontFamilyDescriptor.RemoveValueChanged(textBlock, MeasureFontSize);
        }
        
        public static double GetMaxFontSize(DependencyObject obj)
        {
            return (double)obj.GetValue(MaxFontSizeProperty);
        }

        public static void SetMaxFontSize(DependencyObject obj, double value)
        {
            obj.SetValue(MaxFontSizeProperty, value);
        }

        // Using a DependencyProperty as the backing store for MaxFontSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxFontSizeProperty =
            DependencyProperty.RegisterAttached("MaxFontSize", typeof(double), typeof(FillManager), new UIPropertyMetadata(100.0));
        
        public static double GetInterlinyage(DependencyObject obj)
        {
            return (double)obj.GetValue(InterlinyageProperty);
        }

        public static void SetInterlinyage(DependencyObject obj, double value)
        {
            obj.SetValue(InterlinyageProperty, value);
        }

        // Using a DependencyProperty as the backing store for Interlinyage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InterlinyageProperty =
            DependencyProperty.RegisterAttached("Interlinyage", typeof(double), typeof(FillManager), new UIPropertyMetadata(0.0, OnInterlinyageChanged));

        public static void OnInterlinyageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MeasureFontSize(d, EventArgs.Empty);
        }

        /// <summary>
        /// Задаёт как можно больший размер шрифта текстового блока исходя из доступной для него области
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void MeasureFontSize(object sender, EventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (!(VisualTreeHelper.GetParent(textBlock) is FrameworkElement parent))
            {
                return;
            }

            var width = parent.ActualWidth - textBlock.Margin.Left - textBlock.Margin.Right;
            var height = parent.ActualHeight - textBlock.Margin.Top - textBlock.Margin.Bottom;

            if (textBlock.Text.Length == 0 || width < double.Epsilon || height < double.Epsilon)
            {
                return;
            }

            if (textBlock.DataContext != null && textBlock.DataContext.ToString() == "{DisconnectedItem}")
            {
                return;
            }

            var ft = new FormattedText(textBlock.Text, CultureInfo.CurrentUICulture, textBlock.FlowDirection, 
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                1.0, textBlock.Foreground, 1.0)
                { TextAlignment = textBlock.TextAlignment, Trimming = textBlock.TextTrimming };

            var lineHeight = GetInterlinyage(textBlock);
            if (lineHeight < textBlock.FontFamily.LineSpacing)
            {
                lineHeight = textBlock.FontFamily.LineSpacing;
            }

            var coef = lineHeight / textBlock.FontFamily.LineSpacing;

            double fontSize;
            if (textBlock.TextWrapping == TextWrapping.NoWrap)
            {
                fontSize = GetMaxFontSize(textBlock);
            }
            else
            {
                // Предскажем количество строк текста, исходя из полученных параметров

                // Количество блоков, получаемое при "разрезании" всего текста (с размером шрифта 1) на куски, равные по длина доступной ширине текстового блока
                var numOfLines = Math.Max(1.0, Math.Round(ft.Height / textBlock.FontFamily.LineSpacing));
                var fullTextLength = ft.WidthIncludingTrailingWhitespace * numOfLines;
                var numberOfLinesPredictedByWidth = fullTextLength / width;

                // Количество строк как отношение всей доступной высоты к высоте 1 строки текста (с размером шрифта 1)
                var numberOfLinesPredictedByHeight = height / lineHeight;
                var numberOfLines = Math.Max(1.0, Math.Max(numOfLines, Math.Floor(Math.Sqrt(numberOfLinesPredictedByWidth * numberOfLinesPredictedByHeight))));

                fontSize = Math.Min(numberOfLinesPredictedByHeight / numberOfLines, GetMaxFontSize(textBlock) / textBlock.FontFamily.LineSpacing);

                ft.MaxTextWidth = width * 0.97;
            }

            do
            {
                fontSize = Math.Max(fontSize, 1.0);

                ft.SetFontSize(fontSize);
                double textHeight = ft.Height * coef;

                if (fontSize > 1.0 && (textHeight > height || (textBlock.TextWrapping == TextWrapping.NoWrap ? ft.Width > width * 0.97 : ft.MinWidth > ft.MaxTextWidth)))
                {
                    var lower = 1.0;

                    if (textHeight > height && textBlock.TextWrapping == TextWrapping.NoWrap)
                        lower = Math.Max(lower, (textHeight - height) / lineHeight);

                    fontSize -= lower;
                    continue;
                }
                else
                {
                    break;
                }

            } while (true);

            textBlock.FontSize = fontSize;
            textBlock.LineHeight = textBlock.FontSize * lineHeight;
        }
    }
}
