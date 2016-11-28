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

namespace Client_GUI
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
            
            // Load values from settings
            txtUsername.Text = Properties.Settings.Default.Username;
            txtServerAddr.Text = Properties.Settings.Default.ServerAddress;
            txtServerPort.Text = Properties.Settings.Default.ServerPort;
            if (Properties.Settings.Default.ConnectionType == "MANUAL")
                radioManual.IsChecked = true;
            else
                radioAuto.IsChecked = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save settings when closing
            Properties.Settings.Default.Username = txtUsername.Text;
            Properties.Settings.Default.ServerAddress = txtServerAddr.Text;
            Properties.Settings.Default.ServerPort = txtServerPort.Text;
            if ((bool)radioManual.IsChecked)
                Properties.Settings.Default.ConnectionType = "MANUAL";
            else
                Properties.Settings.Default.ConnectionType = "AUTOMATIC";
            Properties.Settings.Default.Save();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
