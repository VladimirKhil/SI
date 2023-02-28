using SImulator.ViewModel;
using SIPackages;
using SIPackages.Core;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace SImulator;

/// <summary>
/// Provides interaction logic for CommandWindow.xaml.
/// </summary>
public partial class CommandWindow : Window
{
    /// <summary>
    /// Application version.
    /// </summary>
    public string? Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);

    public CommandWindow() => InitializeComponent();

    private async void Window_Closing(object sender, CancelEventArgs e)
    {
        if (DataContext != null)
        {
            var result = await ((MainViewModel)DataContext).RaiseStop();
            e.Cancel = !result;
        }
    }

    private async void Button_LostKeyboardFocus_1(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (DataContext is MainViewModel mainViewModel)
        {
            await mainViewModel.OnButtonsLeftAsync();
        }
    }

    private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
    {
        e.Accepted = (ViewModel.Core.PlayerKeysModes)e.Item != ViewModel.Core.PlayerKeysModes.Com;
    }

    private void Parameters_Filter(object sender, FilterEventArgs e)
    {
        var parameterName = ((KeyValuePair<string, StepParameter>)e.Item).Key;
        e.Accepted = parameterName != QuestionParameterNames.Question && parameterName != QuestionParameterNames.Answer;
    }
}
