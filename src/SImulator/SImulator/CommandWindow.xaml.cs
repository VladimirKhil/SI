using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SImulator.ViewModel;
using System.Reflection;
using SImulator.ViewModel.PlatformSpecific;

namespace SImulator
{
    /// <summary>
    /// Interaction logic for CommandWindow.xaml
    /// </summary>
    public partial class CommandWindow : Window
    {
        /// <summary>
        /// Версия программы
        /// </summary>
        public string Version
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            }
        }

        public CommandWindow()
        {
            InitializeComponent();
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext != null)
            {
                var result = await ((MainViewModel)DataContext).RaiseStop();
                e.Cancel = !result;
            }
        }

        private void Button_LostKeyboardFocus_1(object sender, KeyboardFocusChangedEventArgs e)
        {
			if (DataContext is MainViewModel gameEngine)
				gameEngine.OnButtonsLeft();
		}

        private void DemonstrationScreens_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = !((IScreen)e.Item).IsRemote;
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = (SImulator.ViewModel.Core.PlayerKeysModes)e.Item != ViewModel.Core.PlayerKeysModes.Com;
        }
    }
}
