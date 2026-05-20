using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LaserCleanChamber.View.Controls
{
    public partial class IndicatorItem : UserControl
    {
        public IndicatorItem()
        {
            InitializeComponent();
        }

        // Состояние (Вкл/Выкл)
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(IndicatorItem), new PropertyMetadata(false));

        // Заголовок (например, "ДВЕРЬ")
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(IndicatorItem), new PropertyMetadata("Параметр"));

        // Текст для активного состояния (например, "ЗАКРЫТА")
        public static readonly DependencyProperty ActiveTextProperty =
            DependencyProperty.Register("ActiveText", typeof(string), typeof(IndicatorItem), new PropertyMetadata("ОК"));

        // Текст для неактивного состояния (например, "ОТКРЫТА")
        public static readonly DependencyProperty InactiveTextProperty =
            DependencyProperty.Register("InactiveText", typeof(string), typeof(IndicatorItem), new PropertyMetadata("ОШИБКА"));

        // Цвета
        public static readonly DependencyProperty ActiveColorProperty =
            DependencyProperty.Register("ActiveColor", typeof(SolidColorBrush), typeof(IndicatorItem), new PropertyMetadata(Brushes.Lime));

        public static readonly DependencyProperty InactiveColorProperty =
            DependencyProperty.Register("InactiveColor", typeof(SolidColorBrush), typeof(IndicatorItem), new PropertyMetadata(Brushes.DarkRed));

        public bool IsActive { get => (bool)GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }
        public string Header { get => (string)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }
        public string ActiveText { get => (string)GetValue(ActiveTextProperty); set => SetValue(ActiveTextProperty, value); }
        public string InactiveText { get => (string)GetValue(InactiveTextProperty); set => SetValue(InactiveTextProperty, value); }
        public SolidColorBrush ActiveColor { get => (SolidColorBrush)GetValue(ActiveColorProperty); set => SetValue(ActiveColorProperty, value); }
        public SolidColorBrush InactiveColor { get => (SolidColorBrush)GetValue(InactiveColorProperty); set => SetValue(InactiveColorProperty, value); }
    }
}