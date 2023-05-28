using SIGame.Converters;
using SIGame.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SIGame.Behaviors;

public static class PlayerTableBehavior
{
    public static GameSettingsViewModel GetIsAttached(DependencyObject obj) => (GameSettingsViewModel)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, GameSettingsViewModel value) => obj.SetValue(IsAttachedProperty, value);

    // Using a DependencyProperty as the backing store for IsAttached.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(GameSettingsViewModel), typeof(PlayerTableBehavior), new PropertyMetadata(null, OnIsAttachedChanged));

    private static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var settings = (GameSettingsViewModel)e.NewValue;
        var radio = (RadioButton)d;

        if (settings != null)
        {
            var index = radio.DataContext as int?;

            if (index.HasValue)
            {
                radio.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, new Binding("PlayerNumber") { Source = settings, Converter = new EqualityConverter(), ConverterParameter = index.Value, Mode = BindingMode.TwoWay });
            }
        }
        else
        {
            BindingOperations.ClearBinding(radio, System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty);
        }
    }
}
