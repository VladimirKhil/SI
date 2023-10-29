using SIData;
using System.Windows.Controls;
using System.Windows.Data;

namespace SIGame.View;

/// <summary>
/// Provides interaction logic for RulesSettingsPage.xaml.
/// </summary>
public partial class RulesSettingsPage : Page
{
    public RulesSettingsPage() => InitializeComponent();

    private void ButtonPressMode_Filter(object sender, FilterEventArgs e) =>
        e.Accepted = e.Item is not ButtonPressMode buttonPressMode || buttonPressMode != ButtonPressMode.UsePingPenalty;
}
