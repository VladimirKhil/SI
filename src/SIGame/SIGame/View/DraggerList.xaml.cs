using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections;
using SIGame.Behaviors;

namespace SIGame;

/// <summary>
/// Defines interaction logic for DraggerList.xaml.
/// </summary>
public partial class DraggerList : UserControl
{
    private int index = -1;
    private double initPos = 0.0;

    public DraggerList()
    {
        InitializeComponent();
    }

    private void List_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var child = e.OriginalSource as DependencyObject;
        DependencyObject? parent = null;

        do
        {
            parent = VisualTreeHelper.GetParent(child);
            if (parent == null || parent is ContentPresenter)
                break;
            child = parent;
        } while (true);

        if (parent == null)
        {
            return;
        }

        index = list.ItemContainerGenerator.IndexFromContainer(parent);
        Dragger.SetIsDragged(parent, true);

        var pos = Mouse.GetPosition(list);
        initPos = pos.X;
    }

    private void List_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (index > -1)
        {
            if (list.ItemContainerGenerator.ContainerFromIndex(index) is not FrameworkElement item)
            {
                return;
            }

            var pos = Mouse.GetPosition(list);
            Dragger.SetDragPosition(item, pos.X - initPos);

            if (DataContext is not IList data)
            {
                return;
            }

            if (index > 0) // Does it cross the middle of the left element
            {
                if (list.ItemContainerGenerator.ContainerFromIndex(index - 1) is not FrameworkElement item2)
                {
                    return;
                }

                if (pos.X - initPos < -item2.ActualWidth * 0.6)
                {
                    // Swap elements
                    if (data.IsFixedSize)
                    {
                        (data[index], data[index - 1]) = (data[index - 1], data[index]);
                    }
                    else
                    {
                        var dataItem = data[index - 1];
                        data.RemoveAt(index - 1);
                        data.Insert(index, dataItem);
                    }

                    initPos -= item2.ActualWidth;
                    index--;
                    return;
                }
            }
            if (index < data.Count - 1) // Does it cross the middle of the right element
            {
                if (list.ItemContainerGenerator.ContainerFromIndex(index + 1) is not FrameworkElement item2)
                {
                    return;
                }

                if (pos.X - initPos > item2.ActualWidth * 0.6)
                {
                    // Swap elements
                    if (data.IsFixedSize)
                    {
                        (data[index], data[index + 1]) = (data[index + 1], data[index]);
                    }
                    else
                    {
                        var dataItem = data[index + 1];
                        data.RemoveAt(index + 1);
                        data.Insert(index, dataItem);
                    }

                    initPos += item2.ActualWidth;
                    index++;
                }
            }
        }
    }

    private void List_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndDrag();
    }

    private void List_MouseLeave(object sender, MouseEventArgs e)
    {
        EndDrag();
    }

    private void EndDrag()
    {
        if (index <= -1)
        {
            return;
        }

        var item = list.ItemContainerGenerator.ContainerFromIndex(index);
        Dragger.SetIsDragged(item, false);
        Dragger.SetDragPosition(item, 0);

        index = -1;
    }
}
