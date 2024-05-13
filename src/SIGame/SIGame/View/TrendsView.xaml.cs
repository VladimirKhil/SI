using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Utils;

namespace SIGame.View;

/// <summary>
/// Provides interaction logic for TrendsView.xaml.
/// </summary>
public partial class TrendsView : UserControl
{
    public TrendsView() => InitializeComponent();

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
        var link = ((Hyperlink)sender).NavigateUri;

        try
        {
            Browser.Open(link.ToString());
        }
        catch (Exception exc)
        {
            MessageBox.Show(exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }
    }
}
