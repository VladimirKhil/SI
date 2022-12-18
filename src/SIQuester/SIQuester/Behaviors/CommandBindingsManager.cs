using System.Windows;
using System.Windows.Input;

namespace SIQuester.ViewModel;

/// <summary>
/// Класс, позволяющий передать привязки команд визуальному элементу
/// </summary>
public sealed class CommandBindingsManager : DependencyObject
{
    public static CommandBindingCollection GetRegisterCommandBindings(DependencyObject obj) =>
        (CommandBindingCollection)obj.GetValue(RegisterCommandBindingsProperty);

    public static void SetRegisterCommandBindings(DependencyObject obj, CommandBindingCollection value) =>
        obj.SetValue(RegisterCommandBindingsProperty, value);

    public static readonly DependencyProperty RegisterCommandBindingsProperty =
        DependencyProperty.RegisterAttached(
            "RegisterCommandBindings",
            typeof(CommandBindingCollection),
            typeof(CommandBindingsManager),
            new UIPropertyMetadata(null, OnRegisterCommandBindingChanged));

    private static void OnRegisterCommandBindingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not UIElement element)
        {
            return;
        }

        if (e.OldValue is CommandBindingCollection bindings)
        {
            foreach (CommandBinding item in bindings)
            {
                element.CommandBindings.Remove(item);
            }
        }

        if (e.NewValue is CommandBindingCollection newBindings)
        {
            element.CommandBindings.AddRange(newBindings);
        }
    }
}
