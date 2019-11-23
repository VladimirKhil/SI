using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace SIQuester.Behaviors
{
    public static class FocusableBorder
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
            DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(FocusableBorder), new PropertyMetadata(false, IsAttachedChanged));

        private static void IsAttachedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var border = (Border)sender;
            border.MouseDown += (s, e2) =>
                {
                    border.Focus();
                    e2.Handled = true;
                };
        }
    }
}
