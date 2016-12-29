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

namespace Server
{
    class Program
    {

        struct Message
        {
            public string type;
            public Byte[] content;
            public int contentLen;
        }

        struct MessageType
        {
            public const string INITIAL = "INI:";
            public const string TEXT = "TXT:";
            public const string IMAGE = "IMG:";
            public const string QUIT = "QUT:";
        }

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
                        Message clientMsg; // Message coming from client
                        Message serverMsg; // Server's response to client

                        // Wait for a message from client
                        var buffer = new byte[4096]; // Write and Recieve buffer for client-server stream
                        var clientByteCount = await networkStream.ReadAsync(buffer, 0, buffer.Length); // Read bytes sent by client store in buffer
                        // Store incoming message
                        clientMsg = RecieveMessage(buffer, clientByteCount);

                        // React to type of client message
                        switch (clientMsg.type)
                        {
                            case MessageType.INITIAL:
                                // Get user name from initial client message
                                username = System.Text.Encoding.UTF8.GetString(clientMsg.content, 0, clientMsg.contentLen).Split(':')[1];
                                // Construct message for other client
                                serverMsg.type = MessageType.TEXT;
                                serverMsg.content = System.Text.Encoding.ASCII.GetBytes(username + " has connected.");
                                serverMsg.contentLen = serverMsg.content.Length;
                                // Log client connecting
                                LogMessage("Client [" + username + "] has connected");
                                // Send server response to other clients
                                await SendMessageAsync(serverMsg);
                                break;
                            case MessageType.TEXT:
                                // Get text from client message
                                string clientText = System.Text.Encoding.UTF8.GetString(clientMsg.content, 0, clientMsg.contentLen);
                                // Construct message for other client
                                serverMsg.type = MessageType.TEXT;
                                serverMsg.content = System.Text.Encoding.ASCII.GetBytes("[" + username + "] " + clientText); // Send on client message with username of sender
                                serverMsg.contentLen = serverMsg.content.Length;
                                // Log client message
                                LogMessage(clientText, username, "Message");
                                // Send server response to other clients
                                await SendMessageAsync(serverMsg);
                                break;
                            case MessageType.IMAGE:
                                // Construct message for other client
                                serverMsg = clientMsg; // Send on copy of client message
                                // Log image sent
                                LogMessage("Sent an image . . .", username, "Info");
                                // Send server response to other clients
                                await SendMessageAsync(serverMsg);
                                break;
                            case MessageType.QUIT:
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
            Message dcMsg;
            dcMsg.type = MessageType.TEXT;
            dcMsg.content = System.Text.Encoding.ASCII.GetBytes(username + " has disconnected.");
            dcMsg.contentLen = dcMsg.content.Length;
            await SendMessageAsync(dcMsg); 

        } // Handle client connection

        private async Task SendMessageAsync(Message msg)
        {
            Byte[] typeBuffer = new Byte[4096]; // Buffer containing message type
            Byte[] msgBuffer = new Byte[4096]; // Buffer containing actual message

            // Construct buffers
            typeBuffer = System.Text.Encoding.ASCII.GetBytes(msg.type);
            msgBuffer = msg.content;

            // Send message to all currently connected clients
            foreach (var client in conns) 
            {
                // Get stream for current client
                var stream = client.GetStream();
                // Send message type first
                await stream.WriteAsync(typeBuffer, 0, typeBuffer.Length);
                // Then send message contents
                await stream.WriteAsync(msgBuffer, 0, msgBuffer.Length);
            }
        } // Broadcasts messages to all connected clients

        private static Message RecieveMessage(Byte[] rawStream, int len) //List<Byte[]> RecieveClientMessage(Byte[] message, int messageLen)
        {
            Message message;

            // TODO: fix exception when debug closes client

            len -= 4; // Remove 4 type elements to get length of content
            message.content = new Byte[len]; // Create byte array to size of message contents

            // Get message type from first 4 elements of the stream
            message.type = System.Text.Encoding.UTF8.GetString(rawStream, 0, 4);
            // Copy the message content from the stream to message.content
            Buffer.BlockCopy(rawStream, 3, message.content, 0, len);
            // Set the content length
            message.contentLen = len;

            return message;
        }

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