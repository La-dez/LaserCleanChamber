using LaserCleanChamber.Model.LaserComm;
using System;
using System.Collections.Generic;
using System.IO.Ports;
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

namespace LaserCleanChamber
{
    /// <summary>
    /// Логика взаимодействия для LaserTestWindow.xaml
    /// </summary>
    public partial class LaserTestWindow : Window
    {
        //private SerialPort serialPort = null;
        LaserPortManager laserPortManager = null;

        LaserParameter<short> powerParam = LaserLimits.Get<short>(LaserRegisters.LaserPowerOutput);
        LaserParameter<ushort> swingSpeedParam = LaserLimits.Get<ushort>(LaserRegisters.SwingSpeed);
        LaserParameter<short> swingWidthParam = LaserLimits.Get<short>(LaserRegisters.SwingWidth);

        public LaserTestWindow()
        {
            InitializeComponent();

            try
            {
                nud_power.Minimum = powerParam.MinValue;
                nud_power.Maximum = powerParam.MaxValue;

                nud_speed.Minimum = swingSpeedParam.MinValue;
                nud_speed.Maximum = swingSpeedParam.MaxValue;

                nud_width.Minimum = swingWidthParam.MinValue;
                nud_width.Maximum = swingWidthParam.MaxValue;
            }
            catch { }

            refresh();
            updateUI();
        }

        private byte slaveId = 1;

        void readInit()
        {
            try
            {
                ushort power_mb = readPower();
                ushort width_mb = readSwingWidth();
                ushort speed_mb = readSwingSpeed();

                nud_power.Value = power_mb;
                nud_speed.Value = speed_mb / 10d;
                nud_width.Value = width_mb / 10d;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        ushort readPower()
        {
            byte[] request = ModbusRtuHelper.BuildReadRequest(slaveId, LaserRegisters.LaserPowerOutput, 1);
            byte[] responce = laserPortManager.SendRequestAndWaitResponse(request);
            ushort[] values = ModbusRtuHelper.ParseReadResponse(responce, slaveId);
            return values[0];
        }

        ushort readSwingWidth()
        {
            byte[] request = ModbusRtuHelper.BuildReadRequest(slaveId, LaserRegisters.SwingWidth, 1);
            byte[] responce = laserPortManager.SendRequestAndWaitResponse(request);
            ushort[] values = ModbusRtuHelper.ParseReadResponse(responce, slaveId);
            return values[0];
        }

        ushort readSwingSpeed()
        {
            byte[] request = ModbusRtuHelper.BuildReadRequest(slaveId, LaserRegisters.SwingSpeed, 1);
            byte[] responce = laserPortManager.SendRequestAndWaitResponse(request);
            ushort[] values = ModbusRtuHelper.ParseReadResponse(responce, slaveId);
            return values[0];
        }

        private void set_power_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ushort power_mb = System.Convert.ToUInt16(nud_power.Value);
                byte[] request = ModbusRtuHelper.BuildWriteSingleRequest(slaveId, LaserRegisters.LaserPowerOutput, power_mb);
                byte[] responce = laserPortManager.SendRequestAndWaitResponse(request);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void set_width_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ushort width_mb = System.Convert.ToUInt16(nud_width.Value * 10);
                byte[] request = ModbusRtuHelper.BuildWriteSingleRequest(slaveId, LaserRegisters.SwingWidth, width_mb);
                byte[] responce = laserPortManager.SendRequestAndWaitResponse(request);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btn_conn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(laserPortManager != null)
                {
                    laserPortManager.Dispose();
                    laserPortManager = null;
                }

                if (cb_ports.SelectedIndex < 0) return;
                string port = cb_ports.SelectedValue as string;
                //serialPort = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);

                laserPortManager = new LaserPortManager(port);
                laserPortManager.Open();

                readInit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            updateUI();
        }

        private void updateUI()
        {
            try
            {
                bool connected = laserPortManager != null && laserPortManager.IsOpened;

                stackParamControls.IsEnabled = connected;
                
            }
            catch { }
        }

        private void btn_refresh_Click(object sender, RoutedEventArgs e)
        {
            refresh();
        }

        void refresh()
        {
            try
            {
                cb_ports.Items.Clear();
                cb_ports.ItemsSource = SerialPort.GetPortNames();

                cb_ports.SelectedIndex = cb_ports.Items.Count - 1;
            }
            catch { }
        }

        private void set_speed_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ushort speed_mb = System.Convert.ToUInt16(nud_speed.Value * 10);
                byte[] request = ModbusRtuHelper.BuildWriteSingleRequest(slaveId, LaserRegisters.SwingSpeed, speed_mb);
                byte[] responce = laserPortManager.SendRequestAndWaitResponse(request);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
