using LaserCleanChamber.Model.LaserComm;
using LaserCleanChamber.ViewModel;
using MahApps.Metro.Controls;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LaserCleanChamber
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            var request = ModbusRtuHelper.BuildWriteSingleRequest(1, 10, 500);
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                (this.DataContext as MainViewModel)?.Dispose();
            }
            catch { }
            Settings.Default.Save();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                (this.DataContext as MainViewModel)?.AutoConnectIfNeeded();
            }
            catch { }
        }
    }
}