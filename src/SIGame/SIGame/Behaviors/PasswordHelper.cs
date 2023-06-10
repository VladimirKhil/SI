using SIGame.Helpers;
using System.Security;
using System.Windows;
using System.Windows.Controls;

namespace SIGame.Behaviors;

/// <summary>
/// Allows to use data binding for <see cref="PasswordBox.Password" /> property of <see cref="PasswordBox" />.
/// </summary>
public static class PasswordHelper
{
    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(PasswordHelper), new PropertyMetadata(false, OnIsAttachedChanged));

    private static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var passwordBox = (PasswordBox)d;
        var set = (bool)e.NewValue;

        if (set)
        {
            passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
        }
        else
        {
            passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
        }
    }

    private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        var passwordBox = (PasswordBox)sender;
        SetPassword(passwordBox, passwordBox.Password);
    }

    public static string GetPassword(DependencyObject obj) => (string)obj.GetValue(PasswordProperty);

    public static void SetPassword(DependencyObject obj, string value) => obj.SetValue(PasswordProperty, value);

    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.RegisterAttached("Password", typeof(string), typeof(PasswordHelper), new PropertyMetadata(null, OnPasswordChanged));
    
    private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var passwordBox = (PasswordBox)d;
        var newPassword = (string)e.NewValue;

        if (passwordBox.Password != newPassword)
        {
            try
            {
                passwordBox.Password = newPassword;
            }
            catch (SecurityException exc)
            {
                MessageBox.Show(exc.Message, AppConstants.ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
