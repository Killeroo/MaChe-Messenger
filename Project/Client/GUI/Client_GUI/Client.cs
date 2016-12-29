﻿using System;
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
        public struct Message
        {
            public string type;
            public Byte[] content;
            public int contentLen;
        }

        public struct MessageType
        {
            public const string INITIAL = "INI:";
            public const string TEXT = "TXT:";
            public const string IMAGE = "IMG:";
            public const string QUIT = "QUT:";
        }

        // Client attributes
        private const string QUIT_STRING = ":IQUIT:"; // String to safely DC from server
        private const string TXT_MSG = ":TXT:";

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
            Message iniMsg;
            iniMsg.type = MessageType.INITIAL;
            iniMsg.content = System.Text.Encoding.ASCII.GetBytes("username:" + username);
            iniMsg.contentLen = iniMsg.content.Length;

            // Send message
            SendMessage(iniMsg); 
        }

        public void SendMessage(Message msg) // Send message to server
        {
            try
            {
                Byte[] typeBuffer = new Byte[4096]; // Buffer for message type
                Byte[] msgBuffer = new Byte[4096]; // Buffer for actual message

                // Construct buffers
                typeBuffer = System.Text.Encoding.ASCII.GetBytes(msg.type);
                msgBuffer = msg.content;

                // Write buffers to stream
                stream.Write(typeBuffer, 0, typeBuffer.Length); // Write type buffer first
                stream.Write(msgBuffer, 0, msgBuffer.Length);
            }
            catch (Exception) { } // TODO: Add some error feedback
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

        public Message RecieveMessage(Byte[] rawStream, int len)
        {
            Message message;

            len -= 4; // remove type elements from length
            message.content = new Byte[len];

            message.type = System.Text.Encoding.UTF8.GetString(rawStream, 0, 4);
            Buffer.BlockCopy(rawStream, 3, message.content, 0, len);
            message.contentLen = len;

            return message;
        }

        public bool Disconnect() // Disconnect from messaging server
        {
            try
            {
                // Construct quit message
                Message quitMsg;
                quitMsg.type = MessageType.QUIT;
                quitMsg.content = new Byte[0];
                quitMsg.contentLen = 0;

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