using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.IO;

using System.Drawing;

using System.Diagnostics;

namespace Client_GUI
{
    /// <summary>
    /// Client Class
    /// Contains basic client functionality and methods
    /// </summary>
    class Client
    {
        // Client attributes
        private const string QUIT_STRING = ":IQUIT:"; // String to safely DC from server
        private static NetworkStream stream; // stream used to read and write data to server
        private static TcpClient client; // Stores client info when connected to server
        private bool connected = false;
        private string serverAddr;
        private Int32 serverPort;
        private string name;

        // Getters
        public NetworkStream ServerStream { get { return stream; } }
        public bool isConnected { get { return connected; } }
        public string hostAddr { get { return serverAddr; } }
        public Int32 hostPort { get { return serverPort; } }
        public string username { get { return name; } }

        
        public Client() { } // Constructor

        public bool Connect(String server, string username = "GUI_USER", Int32 port = 13000) // Connect to messaging server 
        {
            bool blnReturn = false;

            // Check if we are already connected to something
            if (this.isConnected)
                this.Disconnect();

            serverAddr = server;
            serverPort = port;
            name = username;

            try
            {
                client = new TcpClient(server, port); // Connect to server on stated port and address
                stream = client.GetStream(); // Get stream to communicate with

                this.SendInitialData(username); // Send username to server

                connected = true;
                blnReturn = true;
            }
            catch (SocketException except)
            {
                blnReturn = false;
                Debug.WriteLine("client class: " + except.Message + " " + except.StackTrace);
            }

            return blnReturn;
        }

        public void Reconnect()
        {

        }

        private void SendInitialData(string username) // Send inital client data to server
        {
            // Safe check username
            SendMessage("'#INITIALDATA#':" + username + ":"); 
        }

        public void SendMessage(string message) // Send string message to server
        {
            try
            {
                Byte[] buffer = new Byte[4096]; // Message buffer
                buffer = System.Text.Encoding.ASCII.GetBytes(message); // Convert ascii string to bytes
                stream.Write(buffer, 0, buffer.Length); // Send message to server
            }
            catch (Exception) { }
        }

        public string SendImage(MemoryStream imgMemStream)
        {
            string strReturn = null;

            try
            {
                Byte[] buffer = new Byte[4096];
                buffer = imgMemStream.ToArray();
                stream.Write(buffer, 0, buffer.Length);

                strReturn = "[me]\n";

            }
            catch (Exception e)
            {
                strReturn = "Error sending message\n" + e;
            }

            return strReturn;
        }

        public bool Disconnect() // Disconnect from messaging server
        {
            try
            {
                SendMessage(QUIT_STRING);
                stream.Close();
                client.Close();
            }
            catch (NullReferenceException) { }
            finally
            {
                connected = false;
            }

            return connected;
        }
    }
}