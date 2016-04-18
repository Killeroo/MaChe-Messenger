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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ComponentModel;

using System.Net;
using System.Net.Sockets;
using System.IO;

// Auto search functionality

namespace Client_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Local Variable Declaration
        private static string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private readonly BackgroundWorker listeningWorker = new BackgroundWorker(); // Background worker for getting server messages
        Client client; // Client Object (communication etc)
        String server; // Server Address
        String username;
        Int32 port; // Server Port

        public MainWindow()
        {
            InitializeComponent();

            // Local Var setup
            bool connected; // Are we connected?
            client = new Client(); // Get Client object
            server = "127.0.0.1";
            username = "Meow";
            port = 13000;
            
            
            /* Client, statusbar, backgroundworker and GUI setup */
            // TODO: Split into seperate functions
            frmMain.Title = "MaChe Messenger";
            txtMsgBox.AppendText("Welcome to MaChe Messenger\r");
            txtMsgBox.AppendText("Send 'q' to quit.\n");

            connected = client.Connect(server); // Connect to server

            if (Properties.Settings.Default.SearchType == "MANUAL")
            {
                // (connecting/macro.) ManualConnect();
            }
            else
            {
                // (connecting/macro.) AutoConnect();
            }

            if (!Properties.Settings.Default.RememberSettings)
            {
                // Show popup
            }

            ConnectPopup popup = new ConnectPopup();
            popup.ShowDialog();

            UpdateStatusBar(client.isConnected);
            lblServerAddr.Text = "Server: " + server + ":" + port;
            
            listeningWorker.DoWork += listeningWorker_DoWork; // Assign do work function

            // If connected, start listening for server response
            if (client.isConnected)
                listeningWorker.RunWorkerAsync();
        }

        private void ConnectionPopup() // Connection popup code
        {
            ConnectPopup popup = new ConnectPopup();
            popup.ShowDialog();

            client.Connect(popup.txtServerAddr.Text, popup.txtUsername.Text, Convert.ToInt32(popup.txtServerPort.Text)); // Connect to server

            UpdateStatusBar(client.isConnected);
            lblServerAddr.Text = "Server: " + client.hostAddr + ":" + client.hostPort;

            if (client.isConnected)
                listeningWorker.RunWorkerAsync();
        }

        private void SendMessage() // Common send message event code 
        {
            if (txtUserBox.Text == "q") // Quit flag
            {
                // Send quit string
                client.Disconnect();
                Environment.Exit(0);
            }
            else if (txtUserBox.Text.Length != 0) // send when there is something to send
            {
                client.SendMessage(txtUserBox.Text);
            }

            txtUserBox.Clear();
        }

        private void UpdateStatusBar(bool connected)
        {
            if (connected)
            {
                lblConnectStatus.Text = "CONNECTED";
                barItemConnectStatus.Background = Brushes.DarkGreen;
            }
            else
            {
                lblConnectStatus.Text = "DISCONNECTED";
                barItemConnectStatus.Background = Brushes.DarkRed;
            }
        }

        /* Background worker */

        void listeningWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Listen for response from server
            var stream = client.ServerStream;

            try
            {
                while (true)
                {
                    var buffer = new byte[4096]; // Read buffer
                    var serverByteCount = stream.Read(buffer, 0, buffer.Length); // Get Bytes sent by server
                    var serverResponse = System.Text.Encoding.UTF8.GetString(buffer, 0, serverByteCount);
                    Dispatcher.Invoke(new Action(() => { txtMsgBox.AppendText(serverResponse + "\r"); })); // Access the message box using controls dispatcher for safe multi thread access
                }
            }
            catch (IOException) { }
            catch (Exception) { }
            finally
            {
                try
                {
                    Dispatcher.Invoke(new Action(() => { this.UpdateStatusBar(client.Disconnect()); })); // Safely dc and Update GUI
                }
                catch (TaskCanceledException) { }
            }

        }

        /* Event Handlers */

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Send quit string
            client.SendMessage(":IQUIT:");
            client.Disconnect();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void txtUser_KeyDown(object sender, KeyEventArgs e)
        {
            if (chkEnter.IsChecked.Value)
            {
                if (e.Key == Key.Enter)
                {
                    SendMessage();
                }
            }
        }

        private void txtMsgBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtMsgBox.ScrollToEnd();
        }

        private void txtUserBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update status bar
            lblMsgLength.Text = "Len: " + txtUserBox.Text.Length + "/400";
        }

        private void MenuBar_Connection_New_Click(object sender, RoutedEventArgs e)
        {
            ConnectPopup popup = new ConnectPopup();

            client.Disconnect();

            popup.ShowDialog();

            client.Connect(popup.txtServerAddr.Text, popup.txtUsername.Text, Convert.ToInt32(popup.txtServerPort.Text)); // Connect to server

            UpdateStatusBar(client.isConnected);
            lblServerAddr.Text = "Server: " + client.hostAddr + ":" + client.hostPort;

            if (client.isConnected)
            {
                listeningWorker.RunWorkerAsync();
                txtMsgBox.AppendText("\rConnected to chat server.\n");
            }
            else
            {
                txtMsgBox.AppendText("\rCould not find server at this address.\n");
            }
        }

        private void MenuBar_Connection_Reconnect_Click(object sender, RoutedEventArgs e)
        {
            txtMsgBox.AppendText("\rAttempting to reconnect to chat server...");
            client.Disconnect();
            client.Connect(client.hostAddr, client.username, client.hostPort);
            UpdateStatusBar(client.isConnected); // Try to connect again

            if (client.isConnected)
            {
                txtMsgBox.AppendText("\rConnected to chat server.\n");
                listeningWorker.RunWorkerAsync();
            }
            else
            {
                txtMsgBox.AppendText("\rAttempt failed, please try again later.\n");
            }
        }

        private void MenuBar_About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("MaChe Messenger - Client [ALPHA]\nVersion " + version + "\nWritten by Matthew Carney =^-^=\n[matthewcarney64@gmail.com]", "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }
}
