using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using SIQuester.ViewModel;

namespace SIQuester
{
    /// <summary>
    /// Класс, позволяющий мгновенно перевести фокус на редактор только что созданного объекта после загрузки этого редактора
    /// </summary>
    public static class ActivateManager
    {
        public static bool GetIsWatching(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsWatchingProperty);
        }

        public static void SetIsWatching(DependencyObject obj, bool value)
        {
            obj.SetValue(IsWatchingProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsWatching.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsWatchingProperty =
            DependencyProperty.RegisterAttached("IsWatching", typeof(bool), typeof(ActivateManager), new UIPropertyMetadata(false, PropertyChanged));

        private static void PropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var control = (FrameworkElement)sender;
            if ((bool)e.NewValue)
            {
                control.Loaded += Control_Loaded;
            }
            else
            {
                control.Loaded -= Control_Loaded;
            }
        }

        private static void Control_Loaded(object sender, RoutedEventArgs e)
        {
            var control = (FrameworkElement)sender;
            control.Loaded -= Control_Loaded;
            if (control.DataContext == QDocument.ActivatedObject)
            {
                // Финт для того, чтобы появился TextBox для комментария и ограничения. Пустой - сразу исчезает по триггеру
                if (control is TextBox textBox && (textBox.Text == "Ограничение" || textBox.Text == "Комментарий"))
                    textBox.Clear();

                QDocument.ActivatedObject = null;

                control.Focus();
            }
        }
    }
}
