using SIGame.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SIGame.View
{
    /// <summary>
    /// Interaction logic for DesignSettingsPage.xaml
    /// </summary>
    public partial class DesignSettingsPage : Page
    {
        public DesignSettingsPage()
        {
            InitializeComponent();
        }

        private void ToggleButton_KeyDown(object sender, KeyEventArgs e)
        {
            var toggleButton = (ToggleButton)sender;
            if (toggleButton.IsChecked == true)
            {
                if (DataContext == null)
                    return;

                var model = ((MainViewModel)DataContext).Settings.Model;
                if (model == null)
                    throw new ArgumentNullException(nameof(model));

                model.GameButtonKey2 = (int)e.Key;
                toggleButton.IsChecked = false;
                e.Handled = true;
            }
        }

        private void ToggleButton_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((ToggleButton)sender).IsChecked = false;
        }
    }
}
