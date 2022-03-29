using SIQuester.ViewModel;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SIQuester.Behaviors
{
    public static class TextImportManager
    {
        public static ImportTextViewModel GetTextSource(DependencyObject obj)
        {
            return (ImportTextViewModel)obj.GetValue(TextSourceProperty);
        }

        public static void SetTextSource(DependencyObject obj, ImportTextViewModel value)
        {
            obj.SetValue(TextSourceProperty, value);
        }

        // Using a DependencyProperty as the backing store for TextSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextSourceProperty =
            DependencyProperty.RegisterAttached("TextSource", typeof(ImportTextViewModel), typeof(TextImportManager), new UIPropertyMetadata(null, OnTextSourceChanged));

        public static void OnTextSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBox = (RichTextBox)d;
            var model = (ImportTextViewModel)e.NewValue;

            void propertyChangedHandler(object sender, PropertyChangedEventArgs e2)
            {
                if (e2.PropertyName == nameof(ImportTextViewModel.Text))
                {
                    var par = (Paragraph)textBox.Document.Blocks.LastBlock;
                    while (par.Inlines.Count > 1)
                    {
                        par.Inlines.Remove(par.Inlines.LastInline);
                    }

                    ((Run)par.Inlines.LastInline).Text = model.Text;
                }
            }

            void selectTextHandler(int start, int length, Color? color, bool scroll)
            {
                textBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var position = textBox.Document.ContentStart;
                    var offset = 0;
                    TextPointer startPos = null, endPos = null;
                    while (position != null)
                    {
                        if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                        {
                            var len = position.GetTextRunLength(LogicalDirection.Forward);
                            if (startPos == null)
                            {
                                if (start > offset + len)
                                {
                                    offset += len;
                                    position = position.GetNextContextPosition(LogicalDirection.Forward);
                                    continue;
                                }

                                startPos = position.GetPositionAtOffset(start - offset);
                            }

                            if (endPos == null)
                            {
                                if (start + length > offset + len)
                                {
                                    offset += len;
                                    position = position.GetNextContextPosition(LogicalDirection.Forward);
                                    continue;
                                }

                                endPos = position.GetPositionAtOffset(start + length - offset);
                                if (endPos == null)
                                {
                                    endPos = textBox.Document.ContentEnd;
                                }
                            }

                            var range = new TextRange(startPos, endPos);
                            if (color.HasValue)
                            {
                                range.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(color.Value));
                            }
                            else
                            {
                                range.ApplyPropertyValue(TextElement.BackgroundProperty, null);
                            }

                            if (scroll)
                            {
                                var rect = startPos.GetPositionAtOffset(0, LogicalDirection.Forward).GetCharacterRect(LogicalDirection.Forward);

                                textBox.CaretPosition = startPos;

                                double totaloffset = rect.Top + textBox.VerticalOffset;

                                textBox.ScrollToVerticalOffset(totaloffset - textBox.ActualHeight / 2);
                            }

                            break;
                        }

                        position = position.GetNextContextPosition(LogicalDirection.Forward);
                    }
                }));
            }

            if (model == null)
            {
                var oldModel = (ImportTextViewModel)e.OldValue;

                oldModel.PropertyChanged -= propertyChangedHandler;
                oldModel.SelectText -= selectTextHandler;

                return;
            }

            var blocks = textBox.Document.Blocks;
            blocks.Clear();
            blocks.Add(new Paragraph(new Run(model.Text)));

            model.PropertyChanged += propertyChangedHandler;
            model.SelectText += selectTextHandler;
        }
    }
}
