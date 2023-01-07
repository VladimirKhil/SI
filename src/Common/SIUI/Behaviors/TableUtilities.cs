using System;
using System.Windows;
using System.Windows.Media.Animation;
using SIUI.ViewModel;

namespace SIUI.Behaviors
{
    public static class TableUtilities
    {
        public static Table GetGameThemesStoryboard(DependencyObject obj)
        {
            return (Table)obj.GetValue(GameThemesStoryboardProperty);
        }

        public static void SetGameThemesStoryboard(DependencyObject obj, Table value)
        {
            obj.SetValue(GameThemesStoryboardProperty, value);
        }

        // Using a DependencyProperty as the backing store for GameThemesStoryboard.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GameThemesStoryboardProperty =
            DependencyProperty.RegisterAttached("GameThemesStoryboard", typeof(Table), typeof(TableUtilities), new UIPropertyMetadata(null, OnGameThemesStoryboardChanged));

        public static void OnGameThemesStoryboardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var table = (Table)e.NewValue;
            var element = (FrameworkElement)d;

            if (table == null || table.DataContext == null)
            {
                return;
            }

            var themesCount = ((TableInfoViewModel)table.DataContext).GameThemes.Count;

            var animation = new DoubleAnimation(-element.ActualHeight, TimeSpan.FromMilliseconds(Math.Max(3, themesCount) * 15000 / 18));
            
            animation.Completed += (sender, e2) =>
            {
                var tableInfo = (TableInfoViewModel)table.DataContext;

                if (tableInfo == null)
                {
                    return;
                }

                lock (tableInfo.TStageLock)
                {
                    if (tableInfo.TStage == TableStage.GameThemes)
                    {
                        tableInfo.TStage = TableStage.Sign;
                    }
                }
            };

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath("RenderTransform.Y"));

            element.Loaded += (sender, e2) =>
            {
                storyboard.Begin();
            };

            element.Unloaded += (sender, e2) =>
            {
                storyboard.Stop();
            };
        }

        public static double GetOffset(DependencyObject obj) => (double)obj.GetValue(OffsetProperty);

        public static void SetOffset(DependencyObject obj, double value) => obj.SetValue(OffsetProperty, value);

        public static readonly DependencyProperty OffsetProperty =
            DependencyProperty.RegisterAttached("Offset", typeof(double), typeof(TableUtilities), new UIPropertyMetadata(0.0));
    }
}
