using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.IO;

// TODO: Remove string returns change to bools

namespace Client_GUI
{
    /// <summary>
    /// Client Utility Class
    /// Contains basic client functionality and methods
    /// Optimised for connecting to MaChe server
    /// </summary>
    class Client
    {
        private static NetworkStream stream; // stream used to read and write data to server
        private static TcpClient client; // Stores client info when connected to server
        public NetworkStream ServerStream { get {return stream; } } // Server stream getter

        // Connection attributes
        private string serverAddr { get; set; }
        private Int32 serverPort { get; set; }
        private string name { get; set; }
        private bool connected { get; set; }

        public Client() { } // Constructor

        public bool Connect(String server, string username = "GUI_USER", Int32 port = 13000) // Connect to messaging server 
        {
            bool blnReturn = false;

            serverAddr = server;
            serverPort = port;
            name = username;

            try
            {
                TcpClient client = new TcpClient(server, port); // Connect to server on stated port and address
                stream = client.GetStream(); // Get stream to communicate with

                this.SendInitialData(username); // Send username to server

                connected = true;
                blnReturn = true;
            }
            catch (SocketException)
            {
                blnReturn = false;
            }

            return blnReturn;
        }

        private void SendInitialData(string username) // Send inital client data to server
        {
            // Safe check username
            SendMessage("'#INITIALDATA#':" + username + ":"); 
        }

        public string SendMessage(string message) // Send string message to server
        {
            string strReturn = null; // Return string

            try
            {
                Byte[] buffer = new Byte[4096]; // Message buffer
                buffer = System.Text.Encoding.ASCII.GetBytes(message); // Convert ascii string to bytes
                stream.Write(buffer, 0, buffer.Length); // Send message to server

                strReturn = "[me] " + message;
            }
            catch (Exception e)
            {
                strReturn = "Error sending message\n" + e;
            }

            return strReturn;

            //catch (SocketException e)
            //{
            //    strReturn = "Error sending message\n" + e;
            //}
            //catch (IOException e)
            //{
                
            //}
        }

        public string Disconnect() // Disconnect from messaging server
        {
            string strReturn = null;

            try
            {
                stream.Close();
                client.Close();
            }
            catch (NullReferenceException) { }
            finally
            {
                strReturn = "Connection closed.";
                connected = false;
            }

            return strReturn;
        }
    }
}
