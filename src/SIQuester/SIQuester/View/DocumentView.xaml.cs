using SIQuester.Model;
using System.Windows;
using System.Windows.Controls;

namespace SIQuester;

/// <summary>
/// Логика взаимодействия для DocumentView.xaml
/// </summary>
public partial class DocumentView : UserControl
{
    public DocumentView() => InitializeComponent();

    private void FontSmaller_Click(object sender, RoutedEventArgs e) => AppSettings.Default.ChangeFontSize(-1);

    private void FontBigger_Click(object sender, RoutedEventArgs e) => AppSettings.Default.ChangeFontSize(+1);

    private void FontReset_Click(object sender, RoutedEventArgs e) => AppSettings.Default.ResetFontSize();
}
