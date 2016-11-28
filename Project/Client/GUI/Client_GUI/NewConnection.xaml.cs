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

            // Load user settings 
            txtUsername.Text = Properties.Settings.Default.Username;
            txtServerAddr.Text = Properties.Settings.Default.ServerAddress;
            txtServerPort.Text = Properties.Settings.Default.ServerPort;
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            // Save user settings
            Properties.Settings.Default.Username = txtUsername.Text;
            Properties.Settings.Default.ServerAddress = txtServerAddr.Text;
            Properties.Settings.Default.ServerPort = txtServerPort.Text;
            this.Close();
        }
    }
}
