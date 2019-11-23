using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    /// <summary>
    /// Класс, позволяющий передать привязки команд визуальному элементу
    /// </summary>
    public sealed class CommandBindingsManager: DependencyObject
    {
        public static CommandBindingCollection GetRegisterCommandBindings(DependencyObject obj)
        {
            return (CommandBindingCollection)obj.GetValue(RegisterCommandBindingsProperty);
        }

        public static void SetRegisterCommandBindings(DependencyObject obj, CommandBindingCollection value)
        {
            obj.SetValue(RegisterCommandBindingsProperty, value);
        }

        // Using a DependencyProperty as the backing store for RegisterCommandBindings.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RegisterCommandBindingsProperty =
            DependencyProperty.RegisterAttached("RegisterCommandBindings", typeof(CommandBindingCollection), typeof(CommandBindingsManager), new UIPropertyMetadata(null, OnRegisterCommandBindingChanged));

        private static void OnRegisterCommandBindingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement element)
            {
                if (e.OldValue is CommandBindingCollection bindings)
                {
                    foreach (CommandBinding item in bindings)
                    {
                        element.CommandBindings.Remove(item);
                    }
                }

                bindings = e.NewValue as CommandBindingCollection;
                if (bindings != null)
                {
                    element.CommandBindings.AddRange(bindings);
                }
            }
        }
    }
}
