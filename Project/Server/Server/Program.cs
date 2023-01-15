/*****************************************************************************/
/*                                                                           */
/* Program Title : MaChe Messaging Server                     Version : 0.40 */
/*                                                                           */
/*****************************************************************************/
/*                                                                           */
/*  MaChe Messenger - Server, provides server for messenger clients to       */
/*  connect to over a LAN network. Works for modern Windows NT and with      */
/*  IPv4 and IPv6.                                                           */
/*                                                                           */
/*****************************************************************************/
/* Author             Version     Comments                          Date     */
/*****************************************************************************/
/* Matthew Carney      0.40       Added simple image handling protocols      */
/*                                                                           */
/* Matthew Carney      0.30       Added client message broadcast             */
/*                                to all other clients                       */
/* Matthew Carney      0.21       Rewrote server logging, added     24/03/16 */
/*                                usernames and rudermentary client          */
/*                                server handshake.                          */
/* Matthew Carney      0.20       Switche from blocking call to    22/03/16 */
/*                                asynchronous                               */
/* Matthew Carney      0.10       Initial version                   21/03/16 */
/*                                                                           */
/*****************************************************************************/
/*                   Email: matthewcarney64@gmail.com                        */
/*****************************************************************************/
/*  Asynchronous Programming - https://msdn.microsoft.com/en-us/library/hh191443.aspx
/*  Threadpool server basis - http://stackoverflow.com/questions/21013751/what-is-the-async-await-equivalent-of-a-threadpool-server
/*  lock  - https://msdn.microsoft.com/en-us/library/c5kehkcz.aspx            
/*  task - https://msdn.microsoft.com/en-gb/library/system.threading.tasks.task(v=vs.110).aspx
/*  await - https://msdn.microsoft.com/en-GB/library/hh156528.aspx
/*****************************************************************************/

// TODO: Edit background worker in client to user Message
// TODO: Change Message struct to class for simplicity (in client especially) (add convert content to string class
// TODO: Change to serialisation

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Drawing;

using MaChe.Common;

namespace Server
{
    class Program
    {
        private static string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        object syncLock = new Object(); // Locking object (stops two threads using same code)
        List<Task> pendingConns = new List<Task>(); // list of connections to be established
        List<TcpClient> conns = new List<TcpClient>(); // List of connected clients

        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            Console.WriteLine("MaChe Messenger - Server v" + version + " [ALPHA]");
            Console.WriteLine("Hit Ctrl-C to exit.\n");

            new Program().StartListener().Wait(); // Start core server method
        } // Entry point

        private async Task StartListener()
        {
            var tcpListener = TcpListener.Create(13000); // Start listening for clients on port 13000
            tcpListener.Start();
            LogMessage("Server started on port 13000");

            while (true) // Core Listening loop
            {
                var tcpClient = await tcpListener.AcceptTcpClientAsync(); // suspend while looking for client, resume from method caller
                LogMessage("A client has connected, retrieving info...");
                var task = StartHandleConnectionAsync(tcpClient); // Handle connected client in new task(thread)
                if (task.IsFaulted)
                    task.Wait(); // If handling connection exceptioned wait for task to finish
            }
        } // Core async server method

        private async Task StartHandleConnectionAsync(TcpClient tcpClient)
        {
            var connectionTask = HandleConnectionAsync(tcpClient);

            lock (syncLock) // Dont access list if its being accessed by other thread
                pendingConns.Add(connectionTask); // Add connection to list of pending client connections

            try
            {
                await connectionTask;
            }
            catch (Exception e)
            {
                LogMessage(e.Message.ToString() + "\n" + e.StackTrace.ToString(), "", "Error");
            }
            finally
            {
                lock (syncLock)
                    pendingConns.Remove(connectionTask); // Connected to client remove from pending list
            }
        } // Handle newly connected clients

        private async Task HandleConnectionAsync(TcpClient tcpClient)
        {
            await Task.Yield(); // Resume other threads here

            bool connected = true; // Is client connected
            string username = null;

            try
            {
                using (var networkStream = tcpClient.GetStream()) // Using stream from/to client
                {
                    // Add client to active connections 
                    lock (syncLock)
                        conns.Add(tcpClient);

                    /* Message Listening Loop */
                    while (connected)
                    {
                        Message receivedMessage = new Message(); // Message coming from client
                        Message responseMessage = new Message(); // Server's response to client

                        // Wait for a message from client
                        var buffer = new byte[16384]; // Write and Recieve buffer for client-server stream
                        int clientByteCount = await networkStream.ReadAsync(buffer, 0, buffer.Length); // Read bytes sent by client store in buffer
                        // Store incoming message
                        receivedMessage.FromBytes(buffer);

                        // React to type of client message
                        switch (receivedMessage.Type)
                        {
                            case MessageType.Initial:
                                // Get user name from initial client message
                                username = System.Text.Encoding.ASCII.GetString(receivedMessage.Content, 0, receivedMessage.ContentLength);
                                // Construct message for other client
                                responseMessage.Type = MessageType.Text;
                                responseMessage.Content = System.Text.Encoding.ASCII.GetBytes(username + " has connected.");
                                // Log client connecting
                                LogMessage("Client [" + username + "] has connected");
                                // Send server response to other clients
                                await SendMessageAsync(responseMessage, username);
                                break;
                            case MessageType.Text:
                                // Get text from client message
                                string clientText = System.Text.Encoding.ASCII.GetString(receivedMessage.Content, 0, receivedMessage.ContentLength);
                                //// Construct message for other client
                                //responseMessage.Type = MessageType.Text;
                                //responseMessage.Content = System.Text.Encoding.ASCII.GetBytes("[" + username + "] " + clientText); // Send on client message with username of sender
                                
                                // Log client message
                                LogMessage(clientText, username, "Message");
                                // Send server response to other clients
                                await SendMessageAsync(receivedMessage, username);
                                break;
                            case MessageType.Image:
                                // Construct message for other client
                                responseMessage = receivedMessage; // Send on copy of client message

                                //foreach (var data in  receivedMessage.Content)
                                //{
                                //    Console.Write(data.ToString("X"));
                                //}
                                //Console.WriteLine();

                                // Log image sent
                                LogMessage("Sent an image, size=" + responseMessage.ContentLength + "bytes", username);
                                // Send server response to other clients
                                await SendMessageAsync(responseMessage, username);
                                break;
                            case MessageType.Quit:
                                connected = false;
                                break;
                            default:
                                // Invalid tag
                                LogMessage("Invalid message tag from [" + username + "]", "Server", "Error");
                                break;
                        }

                    }

                    LogMessage("Disconnected from " + username + ".");
                }
            }
            catch (IOException)
            {
                LogMessage("Client [" + username + "] unexpectedly disconnected.", "", "Warning");
            }
            finally
            {
                tcpClient.Close(); // Close connection

                lock (syncLock)
                    conns.Remove(tcpClient); // Remove client from active connections
            }

            // Construct client disconnected message
            Message dcMsg = new Message
            {
                Type = MessageType.Text,
                Content = System.Text.Encoding.ASCII.GetBytes(username + " has disconnected.")
            };
            await SendMessageAsync(dcMsg, username); 

        } // Handle client connection

        private async Task SendMessageAsync(Message msg, string username)
        {
            msg.Username = username;
            byte[] data = msg.ToBytes();

            // Send message to all currently connected clients
            foreach (var client in conns) 
            {
                // Get stream for current client
                NetworkStream stream = client.GetStream();

                // Send message type first
                await stream.WriteAsync(data, 0, data.Length);
            }
        } // Broadcasts messages to all connected clients

        private static void LogMessage(string message, string header = "Server", string status = "", bool newLine = true)
        {
            ConsoleColor color; ; // Default colour
            string statusMsg = null;

            // Determine color of message based on status
            switch (status)
            {
                case "Info":
                    color = ConsoleColor.DarkGreen;
                    statusMsg = status;
                    break;
                case "Error":
                    color = ConsoleColor.Red;
                    statusMsg = status;
                    break;
                case "Warning":
                    color = ConsoleColor.DarkMagenta;
                    statusMsg = status;
                    break;
                case "Careful":
                    color = ConsoleColor.DarkCyan;
                    statusMsg = status;
                    break;
                case "Message":
                    color = ConsoleColor.DarkGreen;
                    statusMsg = "";
                    break;
                default:
                    color = ConsoleColor.Gray;
                    statusMsg = "";
                    break;
            }

            Console.ForegroundColor = color;

            // Construct log message
            Console.Write("[" + DateTime.Now + "] ");
            if (header != "")
                Console.Write("[{0}] ", header);
            if (statusMsg != "")
                Console.Write("[{0}] ", statusMsg);
            Console.Write(message + "{0}", newLine ? "\n" : "");

            // Reset console colour
            Console.ForegroundColor = ConsoleColor.Gray;
        } // Change to use a string builder

    }
}