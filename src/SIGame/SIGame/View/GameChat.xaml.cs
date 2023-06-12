using SICore;
using SIGame.ViewModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SIGame;

/// <summary>
/// Provides interaction logic for GameChat.xaml.
/// </summary>
public partial class GameChat : UserControl
{
    private static readonly IDictionary<LogMode, Brush> ModeColors = new Dictionary<LogMode, Brush>
    {
        [LogMode.Protocol] = Brushes.Black,
        [LogMode.Log] = Brushes.Red,
        [LogMode.Chat] = Brushes.Blue,
        [LogMode.Chat + 1] = Brushes.Brown,
        [LogMode.Chat + 2] = Brushes.DarkGreen,
        [LogMode.Chat + 3] = Brushes.Maroon,
        [LogMode.Chat + 4] = Brushes.Purple,
        [LogMode.Chat + 5] = Brushes.LightCoral,
        [LogMode.Chat + 6] = Brushes.Firebrick,
        [LogMode.Chat + 7] = Brushes.Olive,
        [LogMode.Chat + 8] = Brushes.Peru,
        [LogMode.Chat + 9] = Brushes.SteelBlue,
        [LogMode.Chat + 10] = Brushes.Indigo,
        [LogMode.Chat + 11] = Brushes.ForestGreen
    };

    public GameChat() => InitializeComponent();

    private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is GameViewModel gameViewModel)
        {
            gameViewModel.Data.StringAdding += AddMessage;
        }

        if (e.OldValue is GameViewModel oldModel)
        {
            oldModel.Data.StringAdding -= AddMessage;
        }
    }

    private void AddMessage(string? person, string message, LogMode mode)
    {
        if (Dispatcher != System.Windows.Threading.Dispatcher.CurrentDispatcher)
        {
            Dispatcher.BeginInvoke(AddMessage, person, message, mode);
            return;
        }

        if (!ModeColors.TryGetValue(mode, out var c))
        {
            c = ModeColors[LogMode.Chat];
        }

        var pos = text.VerticalOffset;

        if (person != null)
        {
            var tr = new TextRange(text.Document.ContentEnd, text.Document.ContentEnd)
            {
                Text = person
            };

            tr.ApplyPropertyValue(TextElement.ForegroundProperty, c);

            tr = new TextRange(text.Document.ContentEnd, text.Document.ContentEnd)
            {
                Text = $": {message}\r"
            };

            tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
        }
        else
        {
            var tr = new TextRange(text.Document.ContentEnd, text.Document.ContentEnd)
            {
                Text = $"{message}\r"
            };

            tr.ApplyPropertyValue(TextElement.ForegroundProperty, c);
        }

        if (pos + text.ViewportHeight >= text.ExtentHeight - 5.0)
        {
            text.ScrollToEnd();
        }
    }
}
