/*****************************************************************************/
/*                                                                           */
/* Program Title : MaChe Messaging Client                     Version : 0.10 */
/*                                                                           */
/*****************************************************************************/
/*                                                                           */
/*  MaChe Messenger - Client, provides a client app for to a message server  */
/*  and communicating messages to other clients idealy over a LAN network.   */
/*  Works for modern Windows NT and with IPv4 and IPv6. Using Block call     */
/*  to connect to server program.                                            */
/*                                                                           */
/*****************************************************************************/
/* Author             Version     Comments                          Date     */
/*****************************************************************************/
/*                                                                           */
/* Matthew Carney      0.10       Initial Version                   21/03/16 */
/*                                                                           */
/*****************************************************************************/
/*                        Email: matthewcarney64@gmail.com                   */
/*****************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.IO;

namespace Client
{
    class Program
    {
        // Add lisening method to get info from server

        // Global variable declaration
        private static NetworkStream stream; // stream used to read and write data to server
        private static TcpClient client; // Stores client info when connected to server

        static void Main(string[] args)
        {
            // Switch to use args
            bool running = true;

            Console.Title = "MaChe Messenger v0.1 - Client";

            Connect("127.0.0.1"); // Connect to server

            Console.Write("\nPlease enter a username : ");

            SendClientData(Console.ReadLine());

            Console.WriteLine("\nWelcome to MaChe Messenger v0.1");
            Console.WriteLine("Enter 'q' to quit");
            while (running)
            {
                Console.Write("Send Message: ");
                String message = Console.ReadLine();
                if (message == "q") // Quit flag
                {
                    // Send quit string
                    SendMessage(":IQUIT:");
                    running = false;
                }
                else if (message.Length != 0) // send when there is something to send
                {
                    SendMessage(message);
                }
            }
            Disconnect();
            Console.WriteLine("Press any key to exit. . . ");
            Console.Read();
        }

        // Connect to messaging server 
        static void Connect(String server) 
        {
            try
            {
                Int32 port = 13000; // default port to listen to

                Console.Write("Looking for server on {0}:{1} . . . ", server, port);
                TcpClient client = new TcpClient(server, port); // Connect to server on stated port and address

                Console.WriteLine("server found.");

                stream = client.GetStream(); // Get stream to communicate
                Console.WriteLine("Connected to server.");
            }
            //catch (ArgumentNullException e)
            catch (SocketException e) 
            {
                WriteErrorMessage(e, "SocketException", "Error occured while trying to connect to server. Please try again.", true);
            }
        }

        // Send string message to server
        static void SendMessage(string message)
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
                WriteErrorMessage(e, "SocketException", "Error occured sending message.");
            }
            catch (IOException e)
            {
                WriteErrorMessage(e, "IOException", "Connection error when sending message", true);
            }
        }

        // Send inital client data to server
        static void SendClientData(string username)
        {
            // Safe check username
            SendMessage("'#INITIALDATA#':" + username + ":"); // Send initial data to server
        }

        // Disconnect from messaging server
        static void Disconnect() 
        {
            try
            {
                stream.Close();
                client.Close();
            }
            catch (NullReferenceException) { }
            finally
            {
                Console.WriteLine("Connection closed.");
            }
        }

        // Writes an error message to the screen
        static void WriteErrorMessage(Exception e, string errorTitle, string errorMsg = "", bool exitOnError = false)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine("CRITICAL : " + errorTitle);
            if (errorMsg != "") { Console.WriteLine(errorMsg); }
            Console.WriteLine(e.Message);
            Console.ForegroundColor = ConsoleColor.Gray;

            if (exitOnError)
            {
                System.Environment.Exit(1);
                Console.WriteLine("Press any key to exit. . . ");
                Console.Read();
            }
        }

    }
}
