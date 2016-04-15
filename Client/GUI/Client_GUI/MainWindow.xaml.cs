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
        Int32 port; // Server Port

        public MainWindow()
        {
            InitializeComponent();

            // Local Var setup
            bool connected; // Are we connected?
            client = new Client(); // Get Client object
            server = "127.0.0.1";
            port = 13000;
            
            /* Client, statusbar, backgroundworker and GUI setup */
            // TODO: Split into seperate functions
            frmMain.Title = "MaChe Messenger - Client [ALPHA]";
            txtMsgBox.AppendText("Welcome to MaChe Messenger v" + version + "\r");
            txtMsgBox.AppendText("Send 'q' to quit.\n");

            connected = client.Connect(server); // Connect to server

            UpdateStatusBar(connected);
            lblServerAddr.Text = "Server: " + server + ":" + port;
            
            listeningWorker.DoWork += listeningWorker_DoWork; // Assign do work function

            // If connected, start listening for server response
            if (connected)
                listeningWorker.RunWorkerAsync();
        }

        private void SendMessage() // Common send message event code 
        {
            if (txtUserBox.Text == "q") // Quit flag
            {
                // Send quit string
                client.SendMessage(":IQUIT:");
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
                lblConnectStatus.Text = "    CONNECTED    ";
                lblConnectStatus.Background = Brushes.DarkGreen;
            }
            else
            {
                lblConnectStatus.Text = " DISCONNECTED ";
                lblConnectStatus.Background = Brushes.DarkRed;
            }
        }

        /* Background worker */

        void listeningWorker_DoWork(object sender, DoWorkEventArgs e)
        {// Move to function
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
            //throw new NotImplementedException();
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
            //SendMessage();
            
            ConnectPopup popup = new ConnectPopup();
            popup.ShowDialog();
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

    }
}
