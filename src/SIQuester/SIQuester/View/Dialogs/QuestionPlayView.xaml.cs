using System.Windows;
using System.Windows.Controls;

namespace SIQuester;

/// <summary>
/// Provides interaction logic for QuestionPlayView.xaml.
/// </summary>
public partial class QuestionPlayView : UserControl
{
    public QuestionPlayView() => InitializeComponent();

    private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue == null)
        {
            webView.DataContext = null; // https://github.com/MicrosoftEdge/WebView2Feedback/issues/1136
        }
    }
}
