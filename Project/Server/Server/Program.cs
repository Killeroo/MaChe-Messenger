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

// TODO: Change send image in client
// TODO: Edit background worker in client to user Message
// TODO: Edit SendClientsMsgAsync to send message with a type
// TODO: edit client to recieve messages in a similar manner to RecieveClientMessage

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
        // Global variable declaration
        //private const Byte INI_TAG = 0;
        //private const Byte MSG_TAG = 1;
        //private const Byte[] IMG_TAG = new Byte[] {84, 88, 84, 58}; // TXT:

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

                    /* Get Initial Client Data */
                    var bufferB = new byte[4096];
                    var initialData = networkStream.Read(bufferB, 0, bufferB.Length);
                    var initialString = System.Text.Encoding.UTF8.GetString(bufferB, 0, initialData);
                    
                    RecieveMessage(bufferB, initialData);
                    
                    username = initialString.Split(':')[1];

                    await SendClientsMsgAsync(username + " has connected."); // tell clients someone has connected

                    LogMessage("Client [" + username + "] has connected");
                    LogMessage("Listening to " + username + "...");

                    /* Message Listening Loop */
                    while (connected)
                    {
                        var buffer = new byte[4096]; // Write and Recieve buffer for client-server stream
                        var clientByteCount = await networkStream.ReadAsync(buffer, 0, buffer.Length); // Read bytes sent by client store in buffer

                        Message message = RecieveMessage(buffer, clientByteCount);

                        switch (message.type)
                        {
                            case MessageType.INITIAL:
                                username = System.Text.Encoding.UTF8.GetString(message.content, 0, message.contentLen).Split(':')[1];
                                // tell clients someone has connected
                                await SendClientsMsgAsync(username + " has connected.");
                                LogMessage("Client [" + username + "] has connected");
                                LogMessage("Listening to " + username + "...");
                                break;
                            case MessageType.TEXT:
                                string clientText = System.Text.Encoding.UTF8.GetString(message.content, 0, message.contentLen);
                                LogMessage(clientText, username, "Message");
                                await SendClientsMsgAsync(clientText, username);
                                break;
                            case MessageType.IMAGE:
                                LogMessage("Sent an image . . .", username, "Info");
                                await SendClientsImgAsync(message.content, message.contentLen, username); // Send image
                                break;
                            case MessageType.QUIT:
                                connected = false;
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
            
            await SendClientsMsgAsync(username + " has disconnected."); // Tell all clients someone has disconnected

        } // Handle client connection

        private async Task SendClientsMsgAsync(string message, string username = "")
        {
            Byte[] buffer = new Byte[4096];
            StringBuilder builder = new StringBuilder();

            if (username != "")
                builder.Append("[" + username + "] " + message);
            else
                builder.Append(message);

            buffer = System.Text.Encoding.ASCII.GetBytes(builder.ToString()); // convert string to bytes to send

            foreach (var client in conns) // Send message to all clients
            {
                var stream = client.GetStream();
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
        } // Broadcasts messages to all connected clients

        private async Task SendClientsImgAsync(Byte[] imageData, int imageDataLen,  string username)
        {
            foreach (var client in conns)
            {
                var stream = client.GetStream();
                
                await stream.WriteAsync(imageData, 0, imageData.Length); // Write byte array to stream
            }
        } // Broadcasts images to all connected clients


        //change to async
        private static MemoryStream RecieveClientImage(NetworkStream stream) // Recieve image being sent by client
        {
            Byte[] buffer = new Byte[4096];

            var imageByteCount = stream.Read(buffer, 0, buffer.Length); // Read image Byte array
            var imageMemStream = new MemoryStream(buffer); // Store in memory stream

            return imageMemStream; // Return memory stream to be processed further
        }

        private static Message RecieveMessage(Byte[] rawStream, int len) //List<Byte[]> RecieveClientMessage(Byte[] message, int messageLen)
        {
            Message message;
            len -= 4; // remove type elements from length
            message.content = new Byte[len];

            message.type = System.Text.Encoding.UTF8.GetString(rawStream, 0, 4);
            Buffer.BlockCopy(rawStream, 3, message.content, 0, len);
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