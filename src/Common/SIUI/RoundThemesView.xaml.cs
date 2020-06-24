using SIUI.ViewModel;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace SIUI
{
    /// <summary>
    /// Логика взаимодействия для RoundThemesView.xaml
    /// </summary>
    public partial class RoundThemesView : UserControl
    {
        private readonly Storyboard _biggerAnim = null;
        private readonly Storyboard _smallerAnim = null;

        private ICollectionView collection = null;

        public RoundThemesView()
        {
            InitializeComponent();

            _biggerAnim = (Storyboard)Resources["bigger"];
            _smallerAnim = (Storyboard)Resources["smaller"];

            _biggerAnim.Completed += BiggerAnim_Completed;
            _smallerAnim.Completed += SmallerAnim_Completed;
        }

        private void SmallerAnim_Completed(object sender, EventArgs e)
        {
            if (collection != null && collection.MoveCurrentToNext())
            {
                _biggerAnim.Begin(text);
            }
            else
            {
                if (DataContext is TableInfoViewModel tableInfo)
                {
                    lock (tableInfo.TStageLock)
                    {
                        tableInfo.TStage = TableStage.RoundTable;
                    }
                }
            }
        }

        private void BiggerAnim_Completed(object sender, EventArgs e)
        {
            _smallerAnim.Begin(text);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _biggerAnim.Completed -= BiggerAnim_Completed;
            _smallerAnim.Completed -= SmallerAnim_Completed;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            collection = CollectionViewSource.GetDefaultView(((TableInfoViewModel)DataContext).RoundInfo);
            collection.MoveCurrentToPosition(0);

            BeginAnimation();
        }

        private async void BeginAnimation()
        {
            await Task.Delay(1000);
            Animate();
        }

        private void Animate()
        {
            _biggerAnim.Begin(text);
        }
    }
}
