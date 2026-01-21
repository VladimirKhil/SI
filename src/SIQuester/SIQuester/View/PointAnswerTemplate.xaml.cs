using SIPackages;
using SIQuester.View.Dialogs;
using SIQuester.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace SIQuester.View;

public partial class PointAnswerTemplate : UserControl
{
    public PointAnswerTemplate()
    {
        InitializeComponent();
        DataContextChanged += PointAnswerTemplate_DataContextChanged;
    }

    private void PointAnswerTemplate_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is PointAnswerViewModel oldVm)
        {
            oldVm.SelectPointRequest -= OnSelectPointRequest;
        }

        if (e.NewValue is PointAnswerViewModel newVm)
        {
            newVm.SelectPointRequest += OnSelectPointRequest;
        }
    }

    private void OnSelectPointRequest(ContentItem contentItem)
    {
        var dialog = new SelectPointView(contentItem, (PointAnswerViewModel)DataContext);
        dialog.Owner = Window.GetWindow(this);
        dialog.ShowDialog();
    }
}
