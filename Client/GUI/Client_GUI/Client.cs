using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.IO;

using System.Drawing;

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

        public Client() { } // Constructor

        public string Connect(String server, string username = "GUI_USER", Int32 port = 13000) // Connect to messaging server 
        {
            string strReturn = null; // Return String

            try
            {
                TcpClient client = new TcpClient(server, port); // Connect to server on stated port and address
                stream = client.GetStream(); // Get stream to communicate with

                this.SendInitialData(username); // Send username to server

                strReturn = "Connected";
            }
            catch (SocketException e)
            {
                strReturn = "Could not find server\r" + e;
            }

            return strReturn;
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

        public void SendImage(Image img)
        {
            // TODO: add_drawing
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
            }

            return strReturn;
        }
    }
}
