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
        try
        {
            return await ShowDialogCoreAsync(owner, text, buttons);
        }
        catch (Exception exc)
        {
            System.Diagnostics.Trace.TraceError($"Dialog error: {exc}");
            return false;
        }
    }

    private static async Task<bool> ShowDialogCoreAsync(Window? owner, string text, (string Text, bool Result)[] buttons)
    {
        System.Diagnostics.Trace.TraceInformation($"Dialog: {text}");
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
                System.Diagnostics.Trace.TraceInformation($"Dialog button: {buttonText}");
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

        // A modal ShowDialog does not receive input on macOS while a board (WebKit) window is active,
        // so the dialog is a regular topmost window and the caller awaits its closing
        dialog.Topmost = true;
        dialog.AddHandler(Avalonia.Input.InputElement.PointerPressedEvent, (_, e) => System.Diagnostics.Trace.TraceInformation($"Dialog pointer {e.GetPosition(dialog)}"), Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);

        if (owner != null && owner.IsVisible)
        {
            dialog.Position = new PixelPoint(
                owner.Position.X + (int)((owner.Bounds.Width - 300) / 2 * owner.RenderScaling),
                owner.Position.Y + (int)(owner.Bounds.Height / 3 * owner.RenderScaling));
            dialog.WindowStartupLocation = WindowStartupLocation.Manual;
        }

        var closed = new TaskCompletionSource();
        dialog.Closed += (_, _) => closed.TrySetResult();
        dialog.Show();
        dialog.Activate();
        await closed.Task;

        return result;
    }
}
