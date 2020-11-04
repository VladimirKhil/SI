using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections;
using SIGame.Behaviors;

namespace SIGame
{
    /// <summary>
    /// Логика взаимодействия для DraggerList.xaml
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
            DependencyObject parent = null;
            do
            {
                parent = VisualTreeHelper.GetParent(child);
                if (parent == null || parent is ContentPresenter)
                    break;
                child = parent;
            } while (true);

            if (parent == null)
                return;

            index = list.ItemContainerGenerator.IndexFromContainer(parent);
            Dragger.SetIsDragged(parent, true);

            var pos = Mouse.GetPosition(list);
            initPos = pos.X;
        }

        private void List_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (index > -1)
            {
                var item = list.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
                var pos = Mouse.GetPosition(list);
                Dragger.SetDragPosition(item, pos.X - initPos);

                var data = DataContext as IList;

                if (index > 0) // Не пересёк ли середину левого элемента
                {
                    var item2 = list.ItemContainerGenerator.ContainerFromIndex(index - 1) as FrameworkElement;
                    if (pos.X - initPos < -item2.ActualWidth * 0.6)
                    {
                        // Меняем элементы местами
                        if (data.IsFixedSize)
                        {
                            var dataItem = data[index - 1];
                            data[index - 1] = data[index];
                            data[index] = dataItem;
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
                if (index < data.Count - 1) // Не пересёк ли середину правого элемента
                {
                    var item2 = list.ItemContainerGenerator.ContainerFromIndex(index + 1) as FrameworkElement;
                    if (pos.X - initPos > item2.ActualWidth * 0.6)
                    {
                        // Меняем элементы местами
                        if (data.IsFixedSize)
                        {
                            var dataItem = data[index + 1];
                            data[index + 1] = data[index];
                            data[index] = dataItem;
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
            if (index > -1)
            {
                var item = list.ItemContainerGenerator.ContainerFromIndex(index);
                Dragger.SetIsDragged(item, false);
                Dragger.SetDragPosition(item, 0);

                index = -1;
            }
        }
    }
}
