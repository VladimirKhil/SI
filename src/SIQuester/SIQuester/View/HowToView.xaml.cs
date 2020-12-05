using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace SIQuester
{
    /// <summary>
    /// Логика взаимодействия для HowToView.xaml
    /// </summary>
    public partial class HowToView : UserControl
    {
        public HowToView()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                if (e.Uri.Scheme == "http")
                    Process.Start(e.Uri.ToString());
                else
                    viewer.GoToPage(4);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString(), App.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
