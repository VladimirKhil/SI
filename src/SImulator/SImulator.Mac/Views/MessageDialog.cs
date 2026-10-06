using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using SImulator.Properties;
using SImulator.ViewModel;

namespace SImulator.Views;

/// <summary>
/// Simple message and confirmation dialogs.
/// </summary>
internal static class MessageDialog
{
    public static Task ShowAsync(Window? owner, string text, bool error) =>
        ShowCoreAsync(owner, text, [(MacResources.OK, true)]);

    public static Task<bool> AskAsync(Window? owner, string question) =>
        ShowCoreAsync(owner, question, [(MacResources.Yes, true), (MacResources.No, false)]);

    private static async Task<bool> ShowCoreAsync(Window? owner, string text, (string Text, bool Result)[] buttons)
    {
        var result = false;
        var dialog = new Window
        {
            Title = MainViewModel.ProductName,
            SizeToContent = SizeToContent.WidthAndHeight,
            MaxWidth = 600,
            CanResize = false,
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
        };

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };

        foreach (var (buttonText, buttonResult) in buttons)
        {
            var button = new Button { Content = buttonText, MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
            button.Click += (_, _) =>
            {
                result = buttonResult;
                dialog.Close();
            };

            buttonPanel.Children.Add(button);
        }

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                new SelectableTextBlock { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                buttonPanel,
            },
        };

        if (owner != null && owner.IsVisible)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            var closed = new TaskCompletionSource();
            dialog.Closed += (_, _) => closed.TrySetResult();
            dialog.Show();
            await closed.Task;
        }

        return result;
    }
}
