using System;
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

using System.Drawing.Imaging;

//using System.Drawing;

// TODO: Auto search functionality for server

namespace Client_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Local Variable Declaration
        private static string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private readonly BackgroundWorker listeningWorker = new BackgroundWorker(); // Background thread for handling server messages
        Client client = new Client(); // Client Object
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
            client = new Client(); // Get Client object
            server = "127.0.0.1";
            port = 13000;

            listeningWorker.DoWork += listeningWorker_DoWork; // Assign do work function

            /* Client, statusbar, backgroundworker and GUI setup */
            frmMain.Title = "MaChe Messenger";
            txtMsgBox.AppendText("Welcome to MaChe Messenger\r");
            txtMsgBox.AppendText("Send 'q' to quit.\n");

            if (!Properties.Settings.Default.RememberSettings)
                // Show popup
                ConnectionPopup();

            if (Properties.Settings.Default.SearchType == "MANUAL")
                // Initial Connection
                StartConnection(Properties.Settings.Default.ServerAddr, Properties.Settings.Default.Username, Convert.ToInt32(Properties.Settings.Default.ServerPort)); // Connect to server
            else
                // (connecting/macro.) AutoConnect();
                Macros.AutoFindServer();
            
        }

        private void StartConnection(String serverAddr, String clientUsername, Int32 serverPort) // Connection procedure
        {
            if (client.isConnected)
                client.Disconnect();
            client.Connect(serverAddr, clientUsername, serverPort);
            UpdateStatusBar(client.isConnected);
            lblServerAddr.Text = "Server: " + client.hostAddr + ":" + client.hostPort;
            if (client.isConnected)
                listeningWorker.RunWorkerAsync();
        } 

        private void ConnectionPopup() // Connection popup code
        {
            ConnectPopup popup = new ConnectPopup();
            popup.ShowDialog();

            //client.Connect(popup.txtServerAddr.Text, popup.txtUsername.Text, Convert.ToInt32(popup.txtServerPort.Text)); // Connect to server

            //UpdateStatusBar(client.isConnected);
            //lblServerAddr.Text = "Server: " + client.hostAddr + ":" + client.hostPort;

            //// If connected, start listening for server response
            //if (client.isConnected)
            //    listeningWorker.RunWorkerAsync();
        }

        // Get rid of
        public void SetMsgBoxText(string text) 
        {
            Dispatcher.Invoke(new Action(() => { txtMsgBox.AppendText(text + "\r"); })); // Access the message box using controls dispatcher for safe multi thread access
        } // Threadsafe setting of text in txtMsgBox

        private void SendTextMessage() // Common send message event code 
        {
            if (txtUserBox.Text == "q") // Quit flag
            {
                client.Disconnect();
                Application.Current.Shutdown();
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

        private void UpdateStatusBar(bool connected)
        {
            if (connected)
            {
                lblConnectStatus.Text = "CONNECTED";
                barItemConnectStatus.Background = Brushes.DarkGreen;
            }
            else
            {
                lblConnectStatus.Text = "DISCONNECTED";
                barItemConnectStatus.Background = Brushes.DarkRed;
            }
        }

        // Background connection thread
        void listeningWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            // Listen for response from server
            using (var stream = client.ServerStream) 
            {
                try
                {
                    while (true)
                    {
                        var buffer = new byte[4096]; // Read buffer
                        var serverByteCount = stream.Read(buffer, 0, buffer.Length); // Get Bytes sent by server
                        var serverResponse = System.Text.Encoding.UTF8.GetString(buffer, 0, serverByteCount);
                        if (serverResponse == ":IMAGE:")
                        {
                            // If Image tag, listen for image
                            buffer = new byte[4096]; // Reset buffer
                            stream.Read(buffer, 0, buffer.Length); // Listen for image

                            var imgMemStream = new MemoryStream(buffer); // Store in memory stream
                            var pngDecorder = new PngBitmapDecoder(imgMemStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                            var pngSource = pngDecorder.Frames[0];
                            var img = new System.Windows.Controls.Image();

                            img.Source = pngSource;
                            img.Width = 120;

                            Paragraph para = new Paragraph();
                            para.Inlines.Add(img);

                            // Display using dispather
                            Dispatcher.Invoke(new Action(() => { txtMsgBox.Document.Blocks.Add(para); })); // Access the message box using controls dispatcher for safe multi thread access
                        }
                        else
                        {
                            // Display text normally
                            Dispatcher.Invoke(new Action(() => { txtMsgBox.AppendText(serverResponse + "\r"); })); // Access the message box using controls dispatcher for safe multi thread access
                        }
                    
                    }
                }
                catch (IOException) { }
                catch (Exception) { }
                finally
                {
                    try
                    {
                        Dispatcher.Invoke(new Action(() => { this.UpdateStatusBar(client.Disconnect()); })); // Safely dc and Update GUI
                    }
                    catch (TaskCanceledException) { }
                }
            }
            
        }


        #region Event handlers

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Send quit string
            client.SendMessage(":IQUIT:");
            client.Disconnect();
        }
        private void txtMsgBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtMsgBox.ScrollToEnd();
        }
        private void txtUserBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update status bar
            lblMsgLength.Text = "Len: " + txtUserBox.Text.Length + "/400";
        }
        private void inputTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var inputTabControl = sender as TabControl;
            var selectedTab = inputTabControl.SelectedItem as TabItem;
            if (selectedTab.Header.ToString() == "Drawing") // Drawing tab
            {
                frmMain.Height += 200;
                inputTabControl.Height += 207;
            }
            else if (frmMain.Height != 362) // text input tab
            {
                frmMain.Height = 394;
                inputTabControl.Height = 122;
            }

        }

        // Menu bar events
        private void MenuBar_Connection_New_Click(object sender, RoutedEventArgs e)
        {
            ConnectPopup popup = new ConnectPopup();

            client.Disconnect();

            popup.ShowDialog();

            client.Connect(popup.txtServerAddr.Text, popup.txtUsername.Text, Convert.ToInt32(popup.txtServerPort.Text)); // Connect to server

            UpdateStatusBar(client.isConnected);
            lblServerAddr.Text = "Server: " + client.hostAddr + ":" + client.hostPort;

            if (client.isConnected)
            {
                listeningWorker.RunWorkerAsync();
                txtMsgBox.AppendText("\rConnected to chat server.\n");
            }
            else
            {
                txtMsgBox.AppendText("\rCould not find server at this address.\n");
            }
        }
        private void MenuBar_Connection_Reconnect_Click(object sender, RoutedEventArgs e)
        {
            txtMsgBox.AppendText("\rAttempting to reconnect to chat server...");
            client.Disconnect();
            client.Connect(client.hostAddr, client.username, client.hostPort);
            UpdateStatusBar(client.isConnected); // Try to connect again

            if (client.isConnected)
            {
                txtMsgBox.AppendText("\rConnected to chat server.\n");
                listeningWorker.RunWorkerAsync();
            }
            else
            {
                txtMsgBox.AppendText("\rAttempt failed, please try again later.\n");
            }
        }
        private void MenuBar_About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("MaChe Messenger - Client [ALPHA]\nVersion " + version + "\nWritten by Matthew Carney =^-^=\n[matthewcarney64@gmail.com]", "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void MenuBar_Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        // Text input events
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            SendTextMessage();
        }
        private void txtUser_KeyDown(object sender, KeyEventArgs e)
        {
            if (chkEnter.IsChecked.Value)
            {
                if (e.Key == Key.Enter)
                {
                    SendTextMessage();
                }
            }
        }

        // Drawing input events
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
            Paragraph para = new Paragraph();
            para.Inlines.Add("[Test_User]\r");

            BitmapImage bitmapPic = new BitmapImage(new Uri(@"C:\test.png"));
            Image pic = new Image();
            pic.Source = bitmapPic;
            pic.Width = 150;//20;
            para.Inlines.Add(pic);
            para.Inlines.Add("\r");

            txtMsgBox.Document.Blocks.Add(para);
        }


        #endregion

    }
}
