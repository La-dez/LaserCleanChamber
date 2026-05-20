using LaserCleanChamber.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LaserCleanChamber.View.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для ModesWindow.xaml
    /// </summary>
    public partial class ModesWindow : Window
    {
        public ModesWindow()
        {
            InitializeComponent();
        }

        private bool apply = false;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            apply = true;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (!apply)
                {
                    MainViewModel mvm = (MainViewModel)DataContext;
                    mvm?.ChamberViewModel?.PresetsViewModel.DiscardChangesCommand.Execute(this);
                }
            }
            catch { }
        }
    }
}
