using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using SIUI.ViewModel;
using SIUI.Converters;

namespace SIUI
{
    /// <summary>
    /// Логика взаимодействия для Table.xaml
    /// </summary>
    public partial class Table : UserControl
    {
        public bool Finished
        {
            get { return (bool)GetValue(FinishedProperty); }
            set { SetValue(FinishedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Finished.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FinishedProperty =
            DependencyProperty.Register("Finished", typeof(bool), typeof(Table), new UIPropertyMetadata(false));
        
        /// <summary>
        /// Коэффициент увеличения
        /// </summary>
        public double Zoom
        {
            get { return (double)GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Zoom.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register("Zoom", typeof(double), typeof(Table), new UIPropertyMetadata(1.0));
        
        private readonly Timeline _qSelection = null;
        private readonly Timeline _tSelection = null;

        public Table()
        {
            InitializeComponent();

            _qSelection = FindResource("QSelection") as Timeline;
            _qSelection.Completed += QSelection_Completed;

            _tSelection = FindResource("TSelection") as Timeline;
            _tSelection.Completed += QSelection_Completed;

            var widthBinding = new Binding("ActualWidth") { Source = this };
            var heightBinding = new Binding("ActualHeight") { Source = this };
            var zoomBinding = new MultiBinding { Converter = new ZoomConverter { BaseWidth = 400, BaseHeight = 300 } };
            zoomBinding.Bindings.Add(widthBinding);
            zoomBinding.Bindings.Add(heightBinding);
            SetBinding(ZoomProperty, zoomBinding);
        }

        private void QSelection_Completed(object sender, EventArgs e)
        {
            Finished = true;
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is TableInfoViewModel data)
            {
                SetBinding(FinishedProperty, new Binding(nameof(Finished)) { Source = data, Mode = BindingMode.TwoWay });
            }
        }
    }
}
