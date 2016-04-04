﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ComponentModel;

using System.Net;
using System.Net.Sockets;
using System.IO;

// TODO: Auto search functionality for server

// TODO: add unload event to correctly dc from server when window is closed

namespace Client_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Local Variable Declaration
        private static string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private readonly BackgroundWorker listeningWorker = new BackgroundWorker(); // Background worker for getting server messages
        Client client; // Client Object
        String server; // Server Address
        Int32 port; // Server Port

        Point currentCanvasPoint; // Current point mouse clicked on canvas
        Brush brushColour = SystemColors.WindowFrameBrush; // Colour of paint brush
        int brushThickness = 1; // Thickness of paint brush
        bool blnPenEraser = false; // If eraser is selected

        public MainWindow()
        {
            InitializeComponent();

            // Local Var setup
            string connectStatus; // Status of client-server connection
            bool connected; // Are we connected?
            client = new Client(); // Get Client object
            server = "127.0.0.1";
            port = 13000;

            /* Client, backgroundworker and GUI setup */
            frmMain.Title = "MaChe Messenger - Client [ALPHA]";
            txtMsgBox.AppendText("Welcome to MaChe Messenger v" + version + "\r");
            txtMsgBox.AppendText("Send 'q' to quit.\n");
            txtMsgBox.AppendText("Looking for server on " + server + ":" + port + " . . . ");

            connectStatus = client.Connect(server); // Connect to server
            connected = connectStatus.Equals("Connected"); 

            txtMsgBox.AppendText(connectStatus + "\r"); 
            txtMsgBox.AppendText(connected ? "Server found.\n" : "Server not found.\n");
            listeningWorker.DoWork += listeningWorker_DoWork; // Assign do work function

            // If connected, start listening for server response
            if (connected)
                listeningWorker.RunWorkerAsync();
        }

        private void SendMessage() // Common send message event code 
        {
            if (txtUserBox.Text == "q") // Quit flag
            {
                // Send quit string
                client.SendMessage(":IQUIT:");
                client.Disconnect();
                Environment.Exit(0);
            }
            else if (txtUserBox.Text.Length != 0) // send when there is something to send
            {
                client.SendMessage(txtUserBox.Text);
            }

            txtUserBox.Clear();
        }

        private void FindServer() // Searches on LAN for server
        {
            
        }


        /* Background worker */

        void listeningWorker_DoWork(object sender, DoWorkEventArgs e)
        {// MOVE
            // Listen for response from server
            var stream = client.ServerStream;

            try
            {
                while (true)
                {
                    var buffer = new byte[4096]; // Read buffer
                    var serverByteCount = stream.Read(buffer, 0, buffer.Length); // Get Bytes sent by server
                    var serverResponse = System.Text.Encoding.UTF8.GetString(buffer, 0, serverByteCount);
                    Dispatcher.Invoke(new Action(() => { txtMsgBox.AppendText(serverResponse + "\r"); })); // Access the message box using controls dispatcher for safe multi thread access
                }
            }
            catch (IOException) { }
            catch (Exception) { }
        }

        /* Event Handlers */

        private void txtMsgBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtMsgBox.ScrollToEnd();
        }
        private void inputTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var inputTabControl = sender as TabControl;
            var selectedTab = inputTabControl.SelectedItem as TabItem;
            if (selectedTab.Header.ToString() == "Drawing")
            {
                // Extend window for drawingpad
                frmMain.Height += 200;
                inputTabControl.Height += 207;
            }
            else if (frmMain.Height != 362)
            {
                // Retract window when not default form size
                frmMain.Height -= 200;
                inputTabControl.Height -= 207;
            }


        } // Tab Changing even

        // Tab 'Text' Events
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }
        private void txtUser_KeyDown(object sender, KeyEventArgs e)
        {
            if (chkEnter.IsChecked.Value)
            {
                if (e.Key == Key.Enter)
                {
                    SendMessage();
                }
            }
        }

        // Tab 'Drawing' Events
        private void drawingCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) 
                currentCanvasPoint = e.GetPosition(drawingCanvas); // If mouse button pressed, get mouse position on canvas
        }
        private void drawingCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Line line = new Line();

                line.Stroke = brushColour; // Brush colour
                line.StrokeThickness = brushThickness; // Brush thickness

                // Draw user line
                line.X1 = currentCanvasPoint.X;
                line.Y1 = currentCanvasPoint.Y;
                line.X2 = e.GetPosition(drawingCanvas).X;
                line.Y2 = e.GetPosition(drawingCanvas).Y;

                currentCanvasPoint = e.GetPosition(drawingCanvas); // Get new current mouse position

                drawingCanvas.Children.Add(line); // Add to canvas
            }
        }
        private void btnCanvasSend_Click(object sender, RoutedEventArgs e)
        {
            /* Save .png of Canvas */
            Rect canvasRect = new Rect(drawingCanvas.RenderSize); // Rectangle size of canvas (placeholder)
            RenderTargetBitmap canvasBitmapRender = new RenderTargetBitmap((int)canvasRect.Right, // Convert canvas to bitmap
                                                                           (int)canvasRect.Bottom,
                                                                           96d, 96d,
                                                                           System.Windows.Media.PixelFormats.Default);
            BitmapEncoder pngEncoder = new PngBitmapEncoder(); // Encode from Bitmap to png
            pngEncoder.Frames.Add(BitmapFrame.Create(canvasBitmapRender));

            // TODO: replace with using
            System.IO.MemoryStream memStream = new System.IO.MemoryStream(); // Save to memory stream

            // Save Image
            // TODO: remove saving file, use memory stream to send file directly
            pngEncoder.Save(memStream);
            memStream.Close();
            System.IO.File.WriteAllBytes("test.png", memStream.ToArray()); // Write out to file in exe dir


            // Displaying image in RTF (Raw)
            //http://stackoverflow.com/questions/542850/how-can-i-insert-an-image-into-a-richtextbox

            // Displaying image in RTF (In depth)
            //http://stackoverflow.com/questions/18017044/insert-image-at-cursor-position-in-rich-text-box

            // Sending an image
            //http://stackoverflow.com/questions/32685333/send-image-from-c-sharp-to-python-through-tcp-not-working

            // Saving an image from canvas
            //http://www.ageektrapped.com/blog/how-to-save-xaml-as-an-image/

            // Versioning
            //http://stackoverflow.com/questions/826777/how-to-have-an-auto-incrementing-version-number-visual-studio

            // Popup
            //http://stackoverflow.com/questions/11499932/wpf-popup-window

            // Base Drawing
            //http://stackoverflow.com/questions/16037753/wpf-drawing-on-canvas-with-mouse-events


        }
        private void btnCanvasClear_Click(object sender, RoutedEventArgs e)
        {
            drawingCanvas.Children.Clear(); // Clear canvas
        }
        private void btnPenThickness_1_Click(object sender, RoutedEventArgs e)
        {
            brushThickness = 1;
        }
        private void btnPenThickness_2_Click(object sender, RoutedEventArgs e)
        {
            brushThickness = 2;
        }
        private void btnPenThickness_3_Click(object sender, RoutedEventArgs e)
        {
            brushThickness = 4;
        }
        private void btnEraser_Click(object sender, RoutedEventArgs e)
        {
            brushColour = System.Windows.Media.Brushes.White;
            brushThickness = 6;

            blnPenEraser = true; // TODO: Add proper eraser
        }
        private void btnPen_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Change colour
            brushColour = SystemColors.WindowFrameBrush; 
            blnPenEraser = false;
        }
        private void btnPenColour_Black_Click(object sender, RoutedEventArgs e)
        {
            brushColour = System.Windows.Media.Brushes.Black;
        }
        private void btnPenColour_Red_Click(object sender, RoutedEventArgs e)
        {
            brushColour = System.Windows.Media.Brushes.Red;
        }
        private void btnPenColour_Green_Click(object sender, RoutedEventArgs e)
        {
            brushColour = System.Windows.Media.Brushes.Green;
        }
        private void btnPenColour_Blue_Click(object sender, RoutedEventArgs e)
        {
            brushColour = System.Windows.Media.Brushes.Blue;
        }
        private void btnPenColour_Custom_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Add colour picker
        }
       
    }
}
