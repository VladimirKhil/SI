using System.Windows;
using System.Windows.Controls;
using SIGame.ViewModel;
using SICore;
using System;
using System.Windows.Documents;
using System.Windows.Media;
using System.Threading.Tasks;

namespace SIGame
{
    /// <summary>
    /// Логика взаимодействия для NewConnectionView.xaml
    /// </summary>
    public partial class SIOnlineView : UserControl
    {
        public SIOnlineView()
        {
            InitializeComponent();

            DataContextChanged += SIOnlineView_DataContextChanged;
        }

        private void SIOnlineView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var viewModel = (SIOnlineViewModel)DataContext;
            if (viewModel != null)
            {
                viewModel.Message += AddMessage;
                AddMessage(CommonSettings.AppName, SIGame.Properties.Resources.WelcomeToSIOnline);

                viewModel.Load();
            }
        }

        private void AddMessage(string userName, string message)
        {
            if (Dispatcher != System.Windows.Threading.Dispatcher.CurrentDispatcher)
            {
                Dispatcher.BeginInvoke((Action<string, string>)AddMessage, userName, message);
                return;
            }

            try
            {
                var c = Brushes.Black;

                var pos = chat.VerticalOffset;
                var tr = new TextRange(chat.Document.ContentEnd, chat.Document.ContentEnd)
                {
                    Text = userName + ": "
                };

                tr.ApplyPropertyValue(TextElement.ForegroundProperty, c);
                tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);

                pos = chat.VerticalOffset;
                tr = new TextRange(chat.Document.ContentEnd, chat.Document.ContentEnd)
                {
                    Text = message + "\r"
                };

                tr.ApplyPropertyValue(TextElement.ForegroundProperty, c);
                tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
                if (pos + chat.ViewportHeight >= chat.ExtentHeight - 5.0)
                    chat.ScrollToEnd();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, App.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var text = message.Text.Trim();
                if (text.Length > 0)
                {
                    ((SIOnlineViewModel)DataContext).Say(text);
                }

                message.Clear();
            }
        }

        private async void ContextMenu_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            await Task.Delay(300);
            message.SelectionStart = message.Text.Length;
            message.Focus();
        }
    }
}
