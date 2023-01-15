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

using MaChe.Common;
using System.Windows.Documents.DocumentStructures;

namespace Client_GUI
{
    /// <summary>
    /// Client Class
    /// Contains basic client functionality and methods
    /// </summary>
    class Client
    {
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
            // TODO: Sanity check username
            // Construct initial message
            Message iniMsg = new Message
            {
                Type = MessageType.Initial,
                Content = System.Text.Encoding.ASCII.GetBytes(username),
                Username = username
            };

            // Send message
            SendMessage(iniMsg); 
        }

        public void SendMessage(Message msg) // Send message to server
        {
            msg.Username = name;

            //try
            //{
                byte[] data = msg.ToBytes();

                // Write buffers to stream
                stream.Write(data.ToArray(), 0, data.Length); // Write type buffer first
            //}
            //catch (Exception) { } // TODO: Add some error feedback
        }

        public string SendImage(MemoryStream imgMemStream)
        {
            string strReturn = null;

            try
            {
                Byte[] buffer = new Byte[16384];
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
                // Construct quit message
                Message quitMsg = new Message();
                quitMsg.Type = MessageType.Quit;
                quitMsg.Username = name;
                quitMsg.Content = new byte[0];

                // Send quit message
                SendMessage(quitMsg);
                
                // Close streams
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