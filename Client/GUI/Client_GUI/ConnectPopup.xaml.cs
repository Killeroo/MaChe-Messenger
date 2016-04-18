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

using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Client_GUI
{
    /// <summary>
    /// Interaction logic for ConnectPopup.xaml
    /// </summary>
    public partial class ConnectPopup : Window
    {

        public ConnectPopup()
        {
            InitializeComponent();

            radioManual.IsChecked = true;

            // Load user settings 
            txtUsername.Text = Properties.Settings.Default.Username;
            txtServerAddr.Text = Properties.Settings.Default.ServerAddr;
            txtServerPort.Text = Properties.Settings.Default.ServerPort;
            chkRemSettings.IsChecked = Properties.Settings.Default.RememberSettings;
        }

        private void radioAuto_Checked(object sender, RoutedEventArgs e)
        {
            txtServerAddr.IsEnabled = false;
            txtServerPort.IsEnabled = false;
        }

        private void radioManual_Checked(object sender, RoutedEventArgs e)
        {
            txtServerAddr.IsEnabled = true;
            txtServerPort.IsEnabled = true;
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            // Save user settings
            Properties.Settings.Default.Username = txtUsername.Text;
            Properties.Settings.Default.ServerAddr = txtServerAddr.Text;
            Properties.Settings.Default.ServerPort = txtServerPort.Text;
            Properties.Settings.Default.RememberSettings = (bool) chkRemSettings.IsChecked;
            Properties.Settings.Default.Save();

            this.Close();
        }
    }
}
