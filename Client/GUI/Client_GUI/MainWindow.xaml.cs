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

using System.Net;
using System.Net.Sockets;
using System.IO;

// Sort out gui
// AFTER getting prroof of concept working communicatio  with server working

namespace Client_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Local Variable Declaration
        private static string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        Client client; // Client Object (communication etc)
        String server; // Server Address
        Int32 port; // Server Port

        public MainWindow()
        {
            InitializeComponent();

            // Local Var setup
            client = new Client(); // Get Client object
            server = "127.0.0.1";
            port = 13000;

            // Client and GUI setup
            frmMain.Title = "MaChe Messenger - Client [ALPHA]";
            // Connect to server
            txtMsgBox.AppendText("Looking for server on " + server + ":" + port + " . . . " + client.Connect("127.0.0.1"));
            txtMsgBox.AppendText("Server found.\n");

            txtMsgBox.AppendText("Welcome to MaChe Messenger v" + version + "\n");
            txtMsgBox.AppendText("Send 'q' to quit.\n");

            //txtMsgBox.AppendText("\nPlease enter a username : ");

        }

        private void SendMessage() // Common send message event code 
        {
            if (txtUserBox.Text == "q") // Quit flag
            {
                // Send quit string
                client.SendMessage(":IQUIT:");
                client.Disconnect();
            }
            else if (txtUserBox.Text.Length != 0) // send when there is something to send
            {
                txtMsgBox.AppendText(client.SendMessage(txtUserBox.Text) + "\n");
            }

            txtUserBox.Clear();
        }

        /* Event Handlers */

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

        

    }
}
