using SIGame.ViewModel;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SIGame
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl
    {
        private readonly Storyboard _mainStoryboard = null;

        public MainView()
        {
            InitializeComponent();

            _mainStoryboard = (Storyboard)Resources["Storyboard1"];
            _mainStoryboard.Completed += MainStoryboard_Completed;

            Dispatcher.CurrentDispatcher.ShutdownStarted += CurrentDispatcher_ShutdownStarted;
        }

        void CurrentDispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            var main = (MainViewModel)DataContext;
            if (main != null)
            {
                main.ActiveView = null;
            }
        }

        void MainStoryboard_Completed(object sender, EventArgs e)
        {
            var main = (MainViewModel)DataContext;
            if (main != null && main.ActiveView is IntroViewModel)
            {
                main.ShowMenu();
            }
        }

        private void Body_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ((MainViewModel)DataContext).IsSlideMenuOpen = false;
        }
    }
}
