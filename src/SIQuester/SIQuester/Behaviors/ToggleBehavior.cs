using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SIQuester.Behaviors
{
    public static class ToggleBehavior
    {


        public static ToggleButton GetTarget(DependencyObject obj)
        {
            return (ToggleButton)obj.GetValue(TargetProperty);
        }

        public static void SetTarget(DependencyObject obj, ToggleButton value)
        {
            obj.SetValue(TargetProperty, value);
        }

        // Using a DependencyProperty as the backing store for Target.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.RegisterAttached("Target", typeof(ToggleButton), typeof(ToggleBehavior), new PropertyMetadata(null, OnTargetChanged));

        public static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (Button)d;
            if (e.NewValue != null)
            {
                button.Click += (s, e1) =>
                {
                    ((ToggleButton)e.NewValue).IsChecked = false;
                };
            }
        }
    }
}
