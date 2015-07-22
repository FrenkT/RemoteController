﻿using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Utils.InputGenerator;
using System.Windows.Forms;
using Utils.Clipboard;
using System.Threading;
using Utils.ClipboardSend;

namespace RemoteControllerServer
{
    
    public class StateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
    }

    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon ni;
        //private System.Windows.Forms.ContextMenu contextMenu1;
        //private System.Windows.Forms.MenuItem menuItemExit;
        private System.ComponentModel.IContainer components;
        private static string outPutDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        //private string logoImageStart = new Uri(Path.Combine(outPutDirectory, "Icons\\Computers.ico")).LocalPath;
        private string logoImageConnected = new Uri(Path.Combine(outPutDirectory, "Icons\\Circle_Green.ico")).LocalPath;
        private string logoImageDisconnected = new Uri(Path.Combine(outPutDirectory, "Icons\\Circle_Red.ico")).LocalPath;
        private string logoImagePause = new Uri(Path.Combine(outPutDirectory, "Icons\\pause.ico")).LocalPath;
        
        private Socket controlSocket, keyboardSocket, mouseSocket, clipboardSocket;
        private Socket receiveControl, receiveKeyboard, receiveClipboard;

        private IPEndPoint ipEndPoint, ipEndPointKb, ipEndPointM;
        private String pass = "";
        private String locIp = GetIP4Address();
        private int defaultPort = 4510;
        private int connected = 0;
        private ClipboardListener CListener;
        private ClipboardSender CSender;


        public MainWindow()
        {
            InitializeComponent();

            TextBox_IpAddress.Text = locIp;

            // Create the notifyIcon
            ni = new System.Windows.Forms.NotifyIcon();

            // The Icon property sets the icon that will appear in the systray for this application
            ni.Icon = new System.Drawing.Icon(logoImageDisconnected);
            
            this.components = new System.ComponentModel.Container();
            //this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            //this.menuItemExit = new System.Windows.Forms.MenuItem();

            //this.contextMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] { this.menuItemExit });
            //this.menuItemExit.Index = 0;
            //this.menuItemExit.Text = "Exit";
            //this.menuItemExit.Click += new System.EventHandler(this.menuItemExitClick);

            // The ContextMenu property sets the menu that will appear when the systray icon is right clicked.
            //ni.ContextMenu = this.contextMenu1;

            // The Text property sets the text that will be displayed, in a tooltip, when the mouse hovers over the systray icon.
            ni.Visible = true;

            // Handle the DoubleClick event to activate the form.
            ni.DoubleClick += new System.EventHandler(this.NotifyIcon1DoubleClick);

            // per gestire la chiusura tramite alt+f4 e dal close 'x'
            //this.Closed += new EventHandler(Window_Closing);
            
            Start_Button.IsEnabled = true;
            Close_Button.IsEnabled = false;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(TextBox_ConfigPassword.Password) && !string.IsNullOrWhiteSpace(TextBox_ConfigPort.Text))
                {
                    
                    pass = TextBox_ConfigPassword.Password;
                    defaultPort = int.Parse(TextBox_ConfigPort.Text);
                    InitControlSocket();
                    InitKeyboardSocket();
                    InitMouseSocket();
                    InitClipboardSocket();

                    CListener = new ClipboardListener(this);
                    
                    CListener.ClipboardChange += new RawClipboardEventHandler(CListener_ClipboardChange);
                    
                    controlSocket.Listen(1);
                    
                    AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                    controlSocket.BeginAccept(aCallback, controlSocket);
                }
                else
                {
                    System.Windows.MessageBox.Show("Fill All the fields", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
            Start_Button.IsEnabled = false;
            Close_Button.IsEnabled = true;
        }

        public static string GetIP4Address()
        {
            string IP4Address = String.Empty;

            foreach (IPAddress IPA in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (IPA.AddressFamily == AddressFamily.InterNetwork)
                {
                    IP4Address = IPA.ToString();
                    break;
                }
            }       
            return IP4Address;
        }

        private void InitControlSocket() {
            int locPort = defaultPort;
            try
            {
                SocketPermission permission = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts);
                
                controlSocket = null;

                permission.Demand();

                IPAddress ipAddr = IPAddress.Parse(locIp);

                ipEndPoint = new IPEndPoint(ipAddr, locPort);

                controlSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                controlSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                controlSocket.LingerState = new LingerOption(true, 0);
                controlSocket.Bind(ipEndPoint);
            }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }    
        }
       
        private void InitKeyboardSocket() {
            int locPort = defaultPort+10;
            try
            {
                SocketPermission permissionKb = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts);

                keyboardSocket = null;

                permissionKb.Demand();

                IPAddress ipAddr = IPAddress.Parse(locIp);

                ipEndPointKb = new IPEndPoint(ipAddr, locPort);

                keyboardSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                keyboardSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                keyboardSocket.LingerState = new LingerOption(true, 0);
                keyboardSocket.Bind(ipEndPointKb);
            }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
        }

        private void InitClipboardSocket()
        {
            int locPort = defaultPort + 30;
            try
            {
                SocketPermission permissionClipboard = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts);

                clipboardSocket = null;

                permissionClipboard.Demand();

                IPAddress ipAddr = IPAddress.Parse(locIp);

                ipEndPointKb = new IPEndPoint(ipAddr, locPort);

                clipboardSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                clipboardSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                clipboardSocket.LingerState = new LingerOption(true, 0);
                clipboardSocket.Bind(ipEndPointKb);
            }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
        }
      
        private void InitMouseSocket() {
            int locPort = defaultPort+20;
            try
            {
                SocketPermission permissionM = new SocketPermission(NetworkAccess.Accept, TransportType.Udp, "", SocketPermission.AllPorts);
               
                mouseSocket = null;

                permissionM.Demand();

                IPAddress ipAddr = IPAddress.Parse(locIp);

                ipEndPointM = new IPEndPoint(ipAddr, locPort);

                mouseSocket = new Socket(ipAddr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                mouseSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                mouseSocket.Bind(ipEndPointM);
            }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
        }

        public void StartListenMouse()
        {
            StateObject state = new StateObject();
            state.workSocket = mouseSocket;
            
            mouseSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallbackMouse), state);
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = null;
            Socket handler = null;
            try
            {
                listener = (Socket)ar.AsyncState;
                EndPoint endpoint = listener.LocalEndPoint;
                handler = listener.EndAccept(ar);
                handler.NoDelay = true;

                StateObject state = new StateObject();
                state.workSocket = handler;

                if (endpoint.GetHashCode() == controlSocket.LocalEndPoint.GetHashCode())
                {
                    receiveControl = handler;
                    receiveControl.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackControl), state);
                }
                if (endpoint.GetHashCode() == keyboardSocket.LocalEndPoint.GetHashCode())
                {
                    receiveKeyboard = handler;
                    receiveKeyboard.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackKeyboard), state);
                }
                if (endpoint.GetHashCode() == clipboardSocket.LocalEndPoint.GetHashCode())
                {
                    receiveClipboard = handler;
                    CSender = new ClipboardSender(receiveClipboard);
                }
                
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                if (endpoint.GetHashCode() == controlSocket.LocalEndPoint.GetHashCode())
                {
                    controlSocket.BeginAccept(aCallback, controlSocket);
                }
                if (endpoint.GetHashCode() == keyboardSocket.LocalEndPoint.GetHashCode())
                {
                    keyboardSocket.BeginAccept(aCallback, keyboardSocket);
                }
                if (endpoint.GetHashCode() == clipboardSocket.LocalEndPoint.GetHashCode())
                {
                    clipboardSocket.BeginAccept(aCallback, clipboardSocket);
                }
            }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
        }

        public void ReceiveCallbackControl(IAsyncResult ar)
        {
            try
            {
                string content = string.Empty;

                if (receiveControl.Connected)
                {
                    StateObject state = (StateObject)ar.AsyncState;
                    Socket handler = state.workSocket;

                    int bytesRead = handler.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        content += Encoding.Unicode.GetString(state.buffer, 0, bytesRead);
                        if (content.IndexOf("<PasswordCheck>") > -1)
                        {
                            string str = content.Substring(0, content.LastIndexOf("<PasswordCheck>"));
                            if (CheckPassword(str, handler))
                            {
                                keyboardSocket.Listen(1);
                                AsyncCallback aCallback2 = new AsyncCallback(AcceptCallback);
                                keyboardSocket.BeginAccept(aCallback2, keyboardSocket);
                                clipboardSocket.Listen(1);
                                AsyncCallback aCallback3 = new AsyncCallback(AcceptCallback);
                                clipboardSocket.BeginAccept(aCallback3, clipboardSocket);
                                StartListenMouse();

                                ni.Icon = new System.Drawing.Icon(logoImageConnected);
                                

                                
                                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallbackControl), state);
                                
                                Thread tt = new Thread(() =>
                                {
                                    while (receiveClipboard.Connected)
                                    {
                                        Thread t = new Thread(() => CSender.ReceiveClipboard());
                                        t.SetApartmentState(ApartmentState.STA);
                                        t.Start();
                                        t.Join();
                                        
                                    }
                                });
                                tt.Start();
                                connected = 1;
                            }
                        }
                        else if (content.IndexOf("<Disconnect>") > -1)
                        {
                            ni.Icon = new System.Drawing.Icon(logoImageDisconnected);
                            ni.BalloonTipText = "Client is disconnected";
                            ni.ShowBalloonTip(3000);

                            receiveControl.Shutdown(SocketShutdown.Both);
                            receiveControl.Close();
                            receiveControl.Dispose();

                            receiveKeyboard.Shutdown(SocketShutdown.Both);
                            receiveKeyboard.Close();
                            receiveKeyboard.Dispose();

                            receiveClipboard.Shutdown(SocketShutdown.Both);
                            receiveClipboard.Close();
                            receiveClipboard.Dispose();

                            connected = 0;

                            //mouseSocket.Shutdown(SocketShutdown.Both);
                            //mouseSocket.Close();

                            //CListener.Dispose();
                        }
                        else if(content.IndexOf("<Stop>") > -1)
                        {
                            ni.Icon = new System.Drawing.Icon(logoImagePause);
                            ni.BalloonTipText = "Client stopped control";
                            ni.ShowBalloonTip(3000);
                        }
                        else if (content.IndexOf("<Restart>") > -1)
                        {
                            ni.Icon = new System.Drawing.Icon(logoImageConnected);
                            ni.BalloonTipText = "Client restarted control";
                            ni.ShowBalloonTip(3000);
                        }
                        else
                        {
                            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallbackControl), state);
                        }
                    }
                }
            }
            catch (ObjectDisposedException) { } //Need this when the socket are closed, exception not managed since we don't care if we loose some data
            catch (SocketException) { }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
        }

        public void ReceiveCallbackKeyboard(IAsyncResult ar)
        {
            try
            {
                string content = string.Empty;

                if (receiveKeyboard.Connected)
                {
                    StateObject state = (StateObject)ar.AsyncState;
                    Socket handlerKb = state.workSocket;

                    int bytesRead = handlerKb.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        content += Encoding.Unicode.GetString(state.buffer, 0, bytesRead);

                        ParseKBEvent(content);
                        handlerKb.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallbackKeyboard), state);
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
        }
      
        public void ReceiveCallbackMouse(IAsyncResult ar)
        {
            try
            {
                string content = string.Empty;
                if (mouseSocket != null)
                {
                    StateObject state = (StateObject)ar.AsyncState;
                    Socket handlerM = state.workSocket;

                    int bytesRead = handlerM.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        content += Encoding.Unicode.GetString(state.buffer, 0, bytesRead);

                        ParseMouseEvent(content);

                        handlerM.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallbackMouse), state);
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
        }
      
        public bool CheckPassword(String inputpassword, Socket handler) 
        {
            try
            {
                if (pass.CompareTo(inputpassword) == 0)
                {
                    
                    string str = "ok";

                    ni.Text = "Remote Controller";
                    ni.BalloonTipTitle = "Remote Connection";
                    ni.BalloonTipText = "Client is now connected";
                    ni.ShowBalloonTip(30000);

                    byte[] byteData = Encoding.Unicode.GetBytes(str);
                    handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
                    return true;
                }
                else {
                    string str = "quit";
                    byte[] byteData = Encoding.Unicode.GetBytes(str);
                    handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
                }
            }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
            
            return false;
        }
        
        public void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSend = handler.EndSend(ar);
            }
            catch (ObjectDisposedException) { }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
        }
        
        private void CloseClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (connected == 1)
                {
                    ni.Icon = new System.Drawing.Icon(logoImageDisconnected);

                    string str = "Disconnect";
                    byte[] byteData = Encoding.Unicode.GetBytes(str);
                    receiveControl.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), receiveControl);

                    receiveControl.Shutdown(SocketShutdown.Both);
                    receiveControl.Close();
                    receiveControl.Dispose();

                    receiveKeyboard.Shutdown(SocketShutdown.Both);
                    receiveKeyboard.Close();
                    receiveKeyboard.Dispose();

                    receiveClipboard.Shutdown(SocketShutdown.Both);
                    receiveClipboard.Close();
                    receiveClipboard.Dispose();

                    mouseSocket.Shutdown(SocketShutdown.Both);
                    mouseSocket.Close();

                    CListener.Dispose();

                    connected = 0;
                }
                else {
                    controlSocket.Close();
                    controlSocket.Dispose();
                }
            }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
            
            Close_Button.IsEnabled = false;
            Start_Button.IsEnabled = true;
        }

        private void ParseKBEvent(string kbEvent)
        {
            
            string[] words = kbEvent.Split(new char[] { '+' }, 2);
            if (words[0] == "UP")
            {
                InputGenerator.SendKeyUP(words[1]);
            }
            if (words[0] == "DOWN")
            {
                InputGenerator.SendKeyDown(words[1]);
            }
        }
       
        private void ParseMouseEvent(string mouseEvent)
        {
            
            string[] words = mouseEvent.Split('+');
            if (words[0] == "LEFTDOWN")
            {
                InputGenerator.SendLeftDown(int.Parse(words[1]), int.Parse(words[2]));
            }
            if (words[0] == "LEFTUP")
            {
                InputGenerator.SendLeftUp(int.Parse(words[1]), int.Parse(words[2]));
            }
            if (words[0] == "MOVE")
            {
                InputGenerator.SendMove(int.Parse(words[1]), int.Parse(words[2]));
            }
            if (words[0] == "RIGHTUP")
            {
                InputGenerator.SendRightUp(int.Parse(words[1]), int.Parse(words[2]));
            }
            if (words[0] == "RIGHTDOWN")
            {
                InputGenerator.SendRightDown(int.Parse(words[1]), int.Parse(words[2]));
            }
            if (words[0] == "WHEEL")
            {
                InputGenerator.SendWheel(int.Parse(words[1]), int.Parse(words[2]), int.Parse(words[3]));
                
            }
        }

        void CListener_ClipboardChange(object sender, RawClipboardEventArgs args)
        {
            if (CSender != null) 
            {
                if (!CSender.justReceivedContent)
                {
                    
                    Thread t = new Thread(() => CSender.SendClipboard());
                    t.SetApartmentState(ApartmentState.STA);
                    t.Start();
                   // t.Join();
                }
                else
                {
                    CSender.justReceivedContent = false;
                }
            }
        }

        private void NotifyIcon1DoubleClick(object Sender, EventArgs e)
        {
            // Show the form when the user double clicks on the notify icon. 

            if (this.WindowState == WindowState.Minimized)
                this.WindowState = WindowState.Normal;
            this.Activate();
        }
        
        /*private void menuItemExitClick(object Sender, EventArgs e)
        {
            Window_Closing(Sender, e);
        }*/
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;
            ni.BalloonTipTitle = "Remote Connection";
            ni.BalloonTipText = "Remote Connection is now available";
            ni.ShowBalloonTip(3000);
        }
        
        private void WindowClosed(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (connected == 1)
                {
                    ni.Icon = new System.Drawing.Icon(logoImageDisconnected);

                    string str = "Disconnect";
                    byte[] byteData = Encoding.Unicode.GetBytes(str);
                    receiveControl.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), receiveControl);

                    receiveControl.Shutdown(SocketShutdown.Both);
                    receiveControl.Close();
                    receiveControl.Dispose();

                    receiveKeyboard.Shutdown(SocketShutdown.Both);
                    receiveKeyboard.Close();
                    receiveKeyboard.Dispose();

                    receiveClipboard.Shutdown(SocketShutdown.Both);
                    receiveClipboard.Close();
                    receiveClipboard.Dispose();

                    mouseSocket.Shutdown(SocketShutdown.Both);
                    mouseSocket.Close();

                    CListener.Dispose();
                }

                ni.Visible = false;

                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
        }        
    }
}
