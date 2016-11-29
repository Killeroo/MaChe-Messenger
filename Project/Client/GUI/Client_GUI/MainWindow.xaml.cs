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

using System.Diagnostics;

using System.ComponentModel;

using System.Net;
using System.Net.Sockets;
using System.IO;

using System.Drawing.Imaging;

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
        FlashWindowHelper flashHelper = new FlashWindowHelper(Application.Current); // Flash window object

        // Draw variables
        Point currentCanvasPoint; // Current point mouse clicked on canvas
        Brush brushColour = SystemColors.WindowFrameBrush; // Colour of paint brush
        int brushThickness = 1; // Thickness of paint brush
        bool blnPenEraser = false; // If eraser is selected

        public MainWindow()
        {
            InitializeComponent();

            // Local Var setup
            client = new Client(); // Get Client object

            // Setup background worker
            listeningWorker.DoWork += listeningWorker_DoWork; // Assign do work function
            listeningWorker.WorkerSupportsCancellation = true;

            // Setup main window
            frmMain.Title = "MaChe Messenger";
            txtMsgBox.AppendText("Welcome to MaChe Messenger\r");
            txtMsgBox.AppendText("Send 'q' to quit.\n");
            
        }

        private void startNewConnection(String serverAddr, String clientUsername, Int32 serverPort) // Connection procedure
        {
            // Move to client class
            if (client.isConnected)
                client.Disconnect();
            client.Connect(serverAddr, clientUsername, serverPort);
            //updateStatusBar(client.isConnected);
            
            if (client.isConnected)
                listeningWorker.RunWorkerAsync();
        } 
        private void sendTextMessage() // Common send message event code 
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

        // Thread safe access to message box & flash
        private void messageBox_Add(String message, bool newLine = true, bool flash = false) 
        {
            Dispatcher.Invoke(new Action(() => { txtMsgBox.AppendText(message + (newLine ? "\r" : "")); }));
            if (flash)
                Dispatcher.Invoke(new Action(() => { flashHelper.FlashApplicationWindow(); }));
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
                        Dispatcher.Invoke(new Action(() => { flashHelper.FlashApplicationWindow(); })); // Flash window when message from server is recieved
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
                            para.Inlines.Add((System.Windows.Controls.Image)img);

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
                // TODO: Change this to display exceptions
                catch (IOException except) { Debug.WriteLine("listener thread: " + except.Message + " " + except.StackTrace); }
                catch (SocketException except) 
                { 
                    Debug.WriteLine("listener thread: " + except.Message + " " + except.StackTrace);
                    listeningWorker.CancelAsync();
                }
                catch (Exception except) { Debug.WriteLine("listener thread: " + except.Message + " " + except.StackTrace); }
                finally
                {
                    try
                    {
                        Dispatcher.Invoke(new Action(() => { this.updateStatusBar("DISCONNECTED"); })); // Safely dc and Update GUI
                    }
                    catch (TaskCanceledException) { }
                }
            }
            
        }


        #region Event handlers

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Check if username is set
            if (Properties.Settings.Default.Username == "")
            {
                ChangeUsername changeUserDialog = new ChangeUsername();
                changeUserDialog.Owner = Application.Current.MainWindow;
                changeUserDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                changeUserDialog.ShowDialog();
            }

            txtMsgBox.AppendText("\rTrying to connect to server " + Properties.Settings.Default.ServerAddress + " as [" + Properties.Settings.Default.Username + "]\r");
            updateStatusBar("CONNECTING");

            // Initial Connection
            if (Properties.Settings.Default.ConnectionType == "MANUAL")
                client.Connect(Properties.Settings.Default.ServerAddress, Properties.Settings.Default.Username, Convert.ToInt32(Properties.Settings.Default.ServerPort)); // Connect to server
            else
                Macros.AutoFindServer();

            // Update status bar
            updateStatusBar(client.isConnected ? "CONNECTED" : "DISCONNECTED");

            // Run listener if connected
            if (client.isConnected)
                listeningWorker.RunWorkerAsync();

            // Set focus to text input
            txtUserBox.Focus();
        }
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
                inputTabControl.Height += 190;
            }
            else // text input tab
            {
                frmMain.Height = 425;
                inputTabControl.Height = 122;
            }

        }
        private void updateStatusBar(string status)
        {
            switch (status.ToUpper()) 
            {
                case "CONNECTED":
                    barItemConnectStatus.Background = Brushes.DarkGreen;
                    lblServerAddr.Text = "Server: " + client.hostAddr + ":" + client.hostPort;
                    break;
                case "DISCONNECTED":
                    barItemConnectStatus.Background = Brushes.DarkRed;
                    lblServerAddr.Text = "Server: " + client.hostAddr + ":" + client.hostPort;
                    break;
                case "CONNECTING":
                    barItemConnectStatus.Background = Brushes.DarkOrange;
                    lblServerAddr.Text = "Server: " + Properties.Settings.Default.ServerAddress + ":" + Properties.Settings.Default.ServerPort;
                    break;
            }
            lblConnectStatus.Text = status.ToUpper();

        }

        // Menu bar events
        private void MenuBar_Connection_New_Click(object sender, RoutedEventArgs e)
        {
            ConnectPopup newConnectionDialog = new ConnectPopup();
            String prevAddr;
            int prevPort;

            // Save values for comparision
            prevAddr = Properties.Settings.Default.ServerAddress;
            prevPort = Properties.Settings.Default.ServerPort;

            // Setup dialog position
            newConnectionDialog.Owner = Application.Current.MainWindow;
            newConnectionDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Stop background listener
            listeningWorker.CancelAsync();
            Debug.WriteLine("1." + listeningWorker.IsBusy);

            // Show new connection popup
            newConnectionDialog.ShowDialog();

            // Check if address entered is new
            if (Properties.Settings.Default.ServerAddress != prevAddr || Properties.Settings.Default.ServerPort != prevPort)
            {
                txtMsgBox.AppendText("\rTrying to connect to server " + Properties.Settings.Default.ServerAddress + " as [" + Properties.Settings.Default.Username + "]\r");
                updateStatusBar("CONNECTING");
                Debug.WriteLine("2." + listeningWorker.IsBusy);

                // Try to connect
                client.Connect(Properties.Settings.Default.ServerAddress, Properties.Settings.Default.Username, Properties.Settings.Default.ServerPort);

                Debug.WriteLine("3." + listeningWorker.IsBusy);

                // Update message box
                if (client.isConnected)
                    txtMsgBox.AppendText("Connected to MaChe server.\n");
                else
                    txtMsgBox.AppendText("Could not find server at this address.\n");

                // Update status bar
                updateStatusBar(client.isConnected ? "CONNECTED" : "DISCONNECTED");

                // Run background listener if connected
                if (client.isConnected)
                    listeningWorker.RunWorkerAsync();
            }

        }
        private void MenuBar_Connection_Reconnect_Click(object sender, RoutedEventArgs e) 
        {
            // Broken

            txtMsgBox.AppendText("\rAttempting to reconnect to server...");

            // Disconnect from last server
            listeningWorker.CancelAsync();
            if (client.isConnected)
                client.Disconnect();

            //client.Connect(client.hostAddr, client.username, client.hostPort);
            startNewConnection(client.hostAddr, client.username, client.hostPort);

            //updateStatusBar(client.isConnected); // Try to connect again

            if (client.isConnected)
            {
                txtMsgBox.AppendText("\rConnected to chat server.\n");
            }
            else
            {
                txtMsgBox.AppendText("\rAttempt failed, please try again later.\n");
            }

        }
        private void MenuBar_About_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox msgBox = new MessageBox();
            About aboutDialog = new About();
            aboutDialog.Owner = Application.Current.MainWindow;
            aboutDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            aboutDialog.ShowDialog();
        }
        private void MenuBar_Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
        private void MenuBar_Settings_Click(object sender, RoutedEventArgs e)
        {
            Settings settingsDialog = new Settings();
            settingsDialog.Owner = Application.Current.MainWindow;
            settingsDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            settingsDialog.ShowDialog();
        }
        private void MenuBar_ChangeUsername_Click(object sender, RoutedEventArgs e)
        {
            String prevName = Properties.Settings.Default.Username;
            ChangeUsername changeUserDialog = new ChangeUsername();
            changeUserDialog.Owner = Application.Current.MainWindow;
            changeUserDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            changeUserDialog.ShowDialog();

            if (!prevName.Equals(Properties.Settings.Default.Username, StringComparison.Ordinal))
            {
                txtMsgBox.AppendText("\rUsername changed to [" + Properties.Settings.Default.Username + "]");
                txtMsgBox.AppendText("\rNew name will be used next time you connect.\n");
            }
        }

        // Text input events
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            sendTextMessage();
        }
        private void txtUser_KeyDown(object sender, KeyEventArgs e)
        {
            if (chkEnter.IsChecked.Value)
            {
                if (e.Key == Key.Enter)
                {
                    sendTextMessage();
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
            // Save png object of Canvas
            Rect canvasRect = new Rect(drawingCanvas.RenderSize); // Rectangle size of canvas (placeholder)
            RenderTargetBitmap canvasBitmapRender = new RenderTargetBitmap((int)canvasRect.Right, // Convert canvas to bitmap
                                                                           (int)canvasRect.Bottom,
                                                                           96d, 96d,
                                                                           System.Windows.Media.PixelFormats.Default);
            BitmapEncoder pngEncoder = new PngBitmapEncoder(); // Encode from Bitmap to png
            pngEncoder.Frames.Add(BitmapFrame.Create(canvasBitmapRender));

            //System.IO.MemoryStream memStream = new System.IO.MemoryStream(); // Save to memory stream

            // Send png to server 
            using (System.IO.MemoryStream memStream = new System.IO.MemoryStream()) 
            {
                pngEncoder.Save(memStream);
                client.SendMessage(":IMAGE:");
                txtMsgBox.AppendText(client.SendImage(memStream));
            }
            
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

            // Change this, file doesnt get saved anymore
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
