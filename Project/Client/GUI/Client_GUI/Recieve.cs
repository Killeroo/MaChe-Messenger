using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.IO;

using System.Windows.Threading;

using System.Drawing;

using System.Windows.Documents;

// TODO: add_drawing: remove dont need async, maybe?

namespace Client_GUI
{
    // Holds functions related to recieving server data
    class Recieve
    {

        // Local variable declaration
        private NetworkStream serverStream;
        MainWindow window = new MainWindow(); // Main Window object
        Object syncLock; // Locking object

        public void ServerListening(NetworkStream stream)
        {
            serverStream = stream;
            
            // Start listening
            new Recieve().StartListening().Wait();
        }

        // Does this need to be async?
        private async Task StartListening()
        {
            try
            {
                while (true)
                {
                    var buffer = new Byte[4096]; // Read buffer
                    var serverByteCount = serverStream.Read(buffer, 0, buffer.Length); // Get bytes sent by server
                    var serverResponse = System.Text.Encoding.UTF8.GetString(buffer, 0, serverByteCount); // Convert bytes
                    if (serverResponse == ":IMAGE:") // If recieving an image
                        /* Recieve and display image */
                        await RecieveImageMessage(); 
                    else
                        /* Send text to UI */
                        window.Dispatcher.Invoke(new Action(() => { window.txtMsgBox.AppendText(serverResponse + "\r"); })); // Access the message box using controls dispatcher for safe multi thread access
                }
            }
            catch (IOException) { }
            catch (Exception) { }
        }

        private async Task RecieveImageMessage()
        {
            Byte[] buffer = new Byte[4096]; // Read image buffer
            Image img; // Transfered image storage

            await serverStream.ReadAsync(buffer, 0, buffer.Length);
            var imgMemStream = new MemoryStream(buffer); // Store in memory stream
            img = Image.FromStream(imgMemStream); // Convert to Image object

            /* Send Image to UI */
            // Construct image in side paragraph for RTB
            Paragraph para = new Paragraph();
            para.Inlines.Add(img);



        }
    }
}
