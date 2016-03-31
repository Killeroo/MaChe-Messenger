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

        public MainWindow()
        {
            InitializeComponent();

            Main(); // Backend code entry
        }

        /* BACKEND CODE */

        private static NetworkStream stream; // stream used to read and write data to server
        private static TcpClient client; // Stores client info when connected to server

        private void Main()
        {

            frmMain.Title = "MaChe Messenger v0.1 - Client";

            Connect("127.0.0.1"); // Connect to server

            txtMsgBox.AppendText("\nPlease enter a username : ");

            SendClientData(Console.ReadLine());

            txtMsgBox.AppendText("Welcome to MaChe Messenger v0.1\n");
            txtMsgBox.AppendText("Send 'q' to quit.\n\n");

        }

        // Connect to messaging server 
        private void Connect(String server)
        {
            try
            {
                Int32 port = 13000; // default port to listen to

                txtMsgBox.AppendText("Looking for server on " + server + ":" + port + " . . . ");

                TcpClient client = new TcpClient(server, port); // Connect to server on stated port and address

                txtMsgBox.AppendText("server found.\n");

                stream = client.GetStream(); // Get stream to communicate

                SendClientData("GUI_User");

                txtMsgBox.AppendText("Connected to server.\n");
            }
            //catch (ArgumentNullException e)
            catch (SocketException e)
            {
                //WriteErrorMessage(e, "SocketException", "Error occured while trying to connect to server. Please try again.", true);
            }
        }

        // Send string message to server
        private void SendMessage(string message)
        {
            try
            {
                Byte[] buffer = new Byte[4096]; // Message buffer

                // Convert ascii string to bytes to be sent
                buffer = System.Text.Encoding.ASCII.GetBytes(message);

                // Send message on stream to server
                stream.Write(buffer, 0, buffer.Length);
            }
            catch (SocketException e)
            {
                //WriteErrorMessage(e, "SocketException", "Error occured sending message.");
            }
            catch (IOException e)
            {
                //WriteErrorMessage(e, "IOException", "Connection error when sending message", true);
            }
        }

        // Send inital client data to server
        private void SendClientData(string username)
        {
            // Safe check username
            SendMessage("'#INITIALDATA#':" + username + ":"); // Send initial data to server
        }

        // Disconnect from messaging server
        private void Disconnect()
        {
            try
            {
                stream.Close();
                client.Close();
            }
            catch (NullReferenceException) { }
            finally
            {
                txtMsgBox.AppendText("Connection closed.");
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(txtUserBox.Text);
            txtMsgBox.AppendText("\n[Me] " + txtUserBox);

            if (txtUserBox.Text == "q") // Quit flag
            {
                // Send quit string
                SendMessage(":IQUIT:");
                Disconnect();
            }
            else if (txtUserBox.Text.Length != 0) // send when there is something to send
            {
                SendMessage(txtUserBox.Text);
            }
        }

    }
}
