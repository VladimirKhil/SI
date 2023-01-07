using System;
using System.Windows;

namespace SImulator;

/// <summary>
/// Provides interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow : Window
{
    public static bool CanClose;

    public MainWindow(bool fullScreen)
    {
        InitializeComponent();

        if (!fullScreen)
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
        }
        else if (System.Windows.Forms.Screen.AllScreens.Length == 1)
        {
            hint.Visibility = Visibility.Visible;
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = !CanClose;
    }

    private void DoubleAnimation_Completed(object sender, EventArgs e)
    {
        hint.Visibility = Visibility.Collapsed;
    }
}
