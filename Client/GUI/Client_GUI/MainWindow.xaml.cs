﻿using System;
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
            string connectStatus; // Status of client-server connection
            bool connected; // Are we connected?
            client = new Client(); // Get Client object
            server = "192.168.1.29";
            port = 13000;

            /* Client, backgroundworker and GUI setup */
            frmMain.Title = "MaChe Messenger - Client [ALPHA]";
            txtMsgBox.AppendText("Welcome to MaChe Messenger v" + version + "\r");
            txtMsgBox.AppendText("Send 'q' to quit.\n");
            txtMsgBox.AppendText("Looking for server on " + server + ":" + port + " . . . ");

            connectStatus = client.Connect(server); // Connect to server
            connected = connectStatus.Equals("Connected"); 

            txtMsgBox.AppendText(connectStatus + "\r"); 
            txtMsgBox.AppendText(connected ? "Server found.\n" : "Server not found.\n");
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

    }
}
