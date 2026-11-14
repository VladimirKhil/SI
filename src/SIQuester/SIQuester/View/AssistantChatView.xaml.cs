using SIQuester.ViewModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SIQuester;

/// <summary>
/// Floating Claude assistant chat (bottom-right corner of the main window).
/// </summary>
public partial class AssistantChatView : UserControl
{
    private AssistantChatViewModel? _viewModel;

    public AssistantChatView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>
    /// Hides the assistant; it comes back with the checkbox in Settings → AI assistant.
    /// </summary>
    private void Hide_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.IsOpen == true)
        {
            _viewModel.Toggle.Execute(null);
        }

        Model.AppSettings.Default.ShowAssistant = false;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Messages.CollectionChanged -= Messages_CollectionChanged;
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            foreach (var message in _viewModel.Messages)
            {
                message.PropertyChanged -= Message_PropertyChanged;
            }
        }

        _viewModel = DataContext as AssistantChatViewModel;

        if (_viewModel != null)
        {
            _viewModel.Messages.CollectionChanged += Messages_CollectionChanged;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    // Focus the input when the panel opens
    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AssistantChatViewModel.IsOpen) && _viewModel?.IsOpen == true)
        {
            // Focus after the open animation (0.25 s): the caret is drawn on the window layer and would
            // otherwise be placed where the sliding panel was a moment ago
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(280) };
            timer.Tick += (_, _) => { timer.Stop(); InputBox.Focus(); };
            timer.Start();
        }
    }

    private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (ChatMessageViewModel message in e.NewItems)
            {
                message.PropertyChanged += Message_PropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (ChatMessageViewModel message in e.OldItems)
            {
                message.PropertyChanged -= Message_PropertyChanged;
            }
        }

        ScrollToEnd();
    }

    // Keep the streaming answer in view
    private void Message_PropertyChanged(object? sender, PropertyChangedEventArgs e) => ScrollToEnd();

    private void ScrollToEnd() => Dispatcher.BeginInvoke(() => MessagesScroll.ScrollToEnd(), DispatcherPriority.Background);

    // Enter sends, Shift+Enter inserts a new line
    private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            e.Handled = true;

            if (_viewModel?.Send.CanExecute(null) == true)
            {
                _viewModel.Send.Execute(null);
            }
        }
    }
}
