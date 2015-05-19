using System;
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
        public StringBuilder sb = new StringBuilder();
    }

    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon ni;
        private System.Windows.Forms.ContextMenu contextMenu1;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem2;
        private System.ComponentModel.IContainer components;


        Socket controlSocket, keyboardSocket, mouseSocket, clipboardSocket;
        Socket receiveControl, receiveKeyboard, receiveClipboard;

        IPEndPoint ipEndPoint, ipEndPointKb, ipEndPointM;
        String pass = "";
        String locIp = GetIP4Address();
        int port_conn = 4510;
        ClipboardListener CListener;
        ClipboardSender CSender;

        public MainWindow()
        {
            InitializeComponent();

            // Create the notifyIcon
            ni = new System.Windows.Forms.NotifyIcon();

            var outPutDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            string logoimage = new Uri(Path.Combine(outPutDirectory, "Icons\\Computers.ico")).LocalPath;

            // The Icon property sets the icon that will appear 
            // in the systray for this application
            ni.Icon = new System.Drawing.Icon(logoimage);
            
            this.components = new System.ComponentModel.Container();
            this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();

            // Initialize contextMenu1 
            this.contextMenu1.MenuItems.AddRange(
                        new System.Windows.Forms.MenuItem[] { 
                            this.menuItem1,
                            this.menuItem2});

            // Initialize menuItem1 
            this.menuItem1.Index = 0;
            this.menuItem1.Text = "Exit";
            this.menuItem1.Click += new System.EventHandler(this.menuItem1_Click);

            // Initialize menuItem1 
            this.menuItem2.Index = 0;
            this.menuItem2.Text = "Show Settings";
            this.menuItem2.Click += new System.EventHandler(this.menuItem1_Click);

            // Set up how the form should be displayed. 
            //this.ClientSize = new System.Drawing.Size(292, 266);
            //this.Text = "Notify Icon Example";

            // The ContextMenu property sets the menu that will 
            // appear when the systray icon is right clicked.
            ni.ContextMenu = this.contextMenu1;

            // The Text property sets the text that will be displayed, 
            // in a tooltip, when the mouse hovers over the systray icon.
            ni.Text = "Remote Controller";
            ni.Visible = true;

            // Handle the DoubleClick event to activate the form.
            ni.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
            
            Start_Button.IsEnabled = true;
            StartListen_Button.IsEnabled = false;
            Close_Button.IsEnabled = false;
        }
        /* 
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
                ni.ShowBalloonTip(5000, "APP Hidden", "Application has been minimized", ToolTipIcon.Info);
                
            }
            base.OnStateChanged(e);
        }
        */
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TextBox_ConfigPassword.Password) && !string.IsNullOrWhiteSpace(TextBox_ConfigPort.Text))
            {
                pass = TextBox_ConfigPassword.Password;
                port_conn = int.Parse(TextBox_ConfigPort.Text);
                InitControl_Socket();
                Create_TCPConnection_Keyboard();
                Create_UDPConnection_Mouse();
                Create_TCPConnection_Clipboard();
                
                Start_Button.IsEnabled = false;
                StartListen_Button.IsEnabled = true;
                CListener = new ClipboardListener(this);
                CListener.ClipboardChange += new RawClipboardEventHandler(CListener_ClipboardChange);
            }
            else
            {
                System.Windows.MessageBox.Show("Fill All the fields", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
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

        private void InitControl_Socket() {
            int locPort = port_conn;
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
       
        private void Create_TCPConnection_Keyboard() {
            int locPort = port_conn+10;
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

        private void Create_TCPConnection_Clipboard()
        {
            int locPort = port_conn + 30;
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
      
        private void Create_UDPConnection_Mouse() {
            int locPort = port_conn+20;
            try
            {
                SocketPermission permissionM = new SocketPermission(NetworkAccess.Accept, TransportType.Udp, "", SocketPermission.AllPorts);
               
                mouseSocket = null;

                permissionM.Demand();

                IPAddress ipAddr = IPAddress.Parse(locIp);

                ipEndPointM = new IPEndPoint(ipAddr, locPort);

                mouseSocket = new Socket(ipAddr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                mouseSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                //mouseSocket.LingerState = new LingerOption(true, 0);
                mouseSocket.Bind(ipEndPointM);
            }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
        }

        private void Listen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                controlSocket.Listen(100);
                tbConnectionStatus.Text = "Server is now listening on " + ipEndPoint.Address + " port: " + ipEndPoint.Port;
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                controlSocket.BeginAccept(aCallback, controlSocket);                
            }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
            StartListen_Button.IsEnabled = false;
            Close_Button.IsEnabled = true;
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
                    //controlSocket = handler;
                    receiveControl = handler;
                    receiveControl.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackCONNECTION), state);
                }
                if (endpoint.GetHashCode() == keyboardSocket.LocalEndPoint.GetHashCode())
                {
                    receiveKeyboard = handler;
                    receiveKeyboard.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackKB), state);
                }
                if (endpoint.GetHashCode() == clipboardSocket.LocalEndPoint.GetHashCode())
                {
                    receiveClipboard = handler;
                    CSender = new ClipboardSender(receiveClipboard);
                    //receiveClipboard.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackClipboard), state);
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

        public void ReceiveCallbackCONNECTION(IAsyncResult ar)
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
                            if (Check_Password(str, handler))
                            {
                                keyboardSocket.Listen(100);
                                AsyncCallback aCallback2 = new AsyncCallback(AcceptCallback);
                                keyboardSocket.BeginAccept(aCallback2, keyboardSocket);
                                StartListenMouse();
                                clipboardSocket.Listen(100);
                                AsyncCallback aCallback3 = new AsyncCallback(AcceptCallback);
                                clipboardSocket.BeginAccept(aCallback3, clipboardSocket);
                                
                                this.Dispatcher.Invoke((Action)(() =>
                                {
                                    tbConnectionStatus.Text = "Connection accepted.";
                                    tbKeyboardStatus.Text = "Connection Keyboard accepted.";
                                    tbMouseStatus.Text = "Connection Mouse accepted.";
                                }));
                                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallbackCONNECTION), state);
                            }
                        }
                        else
                        {
                            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallbackCONNECTION), state);
                        }
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
        }

        public void ReceiveCallbackKB(IAsyncResult ar)
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

                        Parse_KB_Event(content);
                        handlerKb.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallbackKB), state);
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

                        Parse_Mouse_Event(content);

                        handlerM.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallbackMouse), state);
                    }
                }
            }
            catch (ObjectDisposedException e) { }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
        }
      
        public bool Check_Password(String inputpassword, Socket handler) 
        {
            try
            {
                if (pass.CompareTo(inputpassword) == 0)
                {
                    string str = "ok";
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
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
        }
        
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = "Disconnect";
                byte[] byteData = Encoding.Unicode.GetBytes(str);
                receiveControl.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), receiveControl);

                receiveControl.Shutdown(SocketShutdown.Both);
                receiveControl.Disconnect(true);
                receiveControl.Close();
                receiveControl.Dispose();

                receiveKeyboard.Shutdown(SocketShutdown.Both);
                receiveKeyboard.Disconnect(true);
                receiveKeyboard.Close();
                receiveKeyboard.Dispose();

                receiveClipboard.Shutdown(SocketShutdown.Both);
                receiveClipboard.Disconnect(true);
                receiveClipboard.Close();
                receiveClipboard.Dispose();

                mouseSocket.Shutdown(SocketShutdown.Both);
                mouseSocket.Close();

                this.Dispatcher.Invoke((Action)(() =>
                {
                    tbConnectionStatus.Text = "Connection Close.";
                    tbKeyboardStatus.Text = "Connection Keyboard Close.";
                    tbMouseStatus.Text = "Connection Mouse Close.";
                }));
            }
            catch (Exception exc) { System.Windows.MessageBox.Show(exc.ToString()); }
            
            Close_Button.IsEnabled = false;
            Start_Button.IsEnabled = true;
            StartListen_Button.IsEnabled = true;
        }

        private void Parse_KB_Event(string kbEvent)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                tbKeyboardStatus.Text = "received -> " + kbEvent;
            }));
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
       
        private void Parse_Mouse_Event(string mouseEvent)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                tbMouseStatus.Text = "mouse received -> " + mouseEvent;
            }));
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
                this.Dispatcher.Invoke((Action)(() =>
                {
                    tbMouseStatus.Text = "mouse received -> " + mouseEvent + " data -> " + words[3];
                }));
            }
        }

        void CListener_ClipboardChange(object sender, RawClipboardEventArgs args)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                tbKeyboardStatus.Text = "NEW CLIPBOARD CONTENT! --> " + args.changed ;
            }));
            //System.Windows.MessageBox.Show("ClipChange");
            if (CSender != null) 
            {
                Thread t = new Thread(() => CSender.SendClipboard());
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }
                
                
        }
        /*
        protected override void Dispose(bool disposing)
        {
            // Clean up any components being used. 
            if (disposing)
                if (components != null)
                    components.Dispose();
           
            //base.Dispose(disposing);
        }
        */
        private void notifyIcon1_DoubleClick(object Sender, EventArgs e)
        {
            // Show the form when the user double clicks on the notify icon. 

            // Set the WindowState to normal if the form is minimized. 
            if (this.WindowState == WindowState.Minimized)
                this.WindowState = WindowState.Normal;

            // Activate the form. 
            this.Activate();
        }

        private void menuItem1_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application. 
            this.Close();
        }
    }
}
