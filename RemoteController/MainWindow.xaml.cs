using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using Utils.Keyboard;
using Utils.Mouse;
using Utils.ClipboardSend;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteController
{
    public class Server
    {
        public String name { get; set; }
        public String ipAddress { get; set; }
        public String password { get; set; }
        public String port { get; set; }
    }

    public class StateObject
    {
        public Socket workSocket = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
    }

    public partial class MainWindow : Window
    {
        private Socket controlSocket, keyboardSocket, mouseSocket, clipboardSocket;
        private String workingServerIp = "";
        private int workingPort = 0;
        private String workingPassword = "";
        private String workingSelection = "";
        private List<Server> serverList = new List<Server>();
        private KeyboardListener KListener;
        private MouseListener MListener;
        private ClipboardSender CSender;
        private bool connected = false;
        public delegate void ReceiveCallbackClipboard();

        public MainWindow()
        {
            InitializeComponent();
            Disconnect_Button.IsEnabled = true;
        }

        private void ConnectClick(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        private int ReceivePasswordCheck()
        {
            byte[] bytes = new byte[1024];
            try
            {
                int bytesReceived = controlSocket.Receive(bytes);
                String passwordCheck = Encoding.Unicode.GetString(bytes, 0, bytesReceived);
                if (passwordCheck.CompareTo("ok") == 0)
                {
                    return 1;
                }
                return 0;
            }
            catch (SocketException) { return 2; }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
                return 2;
            }
        }

        private void ListenFromServer()
        {
            try
            {
                StateObject state = new StateObject();
                state.workSocket = controlSocket;

                controlSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveControlCallback), state);
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        public void Accept()
        {
            Socket listener = null;
            try
            {
                byte[] buffer = new byte[1024];

                listener = controlSocket;

                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = listener;
                listener.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveControlCallback), obj);
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        public void ReceiveControlCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket controlSocket = state.workSocket;

                string content = string.Empty;
                int bytesRead = controlSocket.EndReceive(ar);

                if (bytesRead > 0)
                {
                    content += Encoding.Unicode.GetString(state.buffer, 0, bytesRead);

                    if (content.IndexOf("Disconnect") > -1)
                    {
                        try
                        {
                            controlSocket.Shutdown(SocketShutdown.Both);
                            controlSocket.Close();
                            controlSocket.Dispose();
                            keyboardSocket.Close();
                            mouseSocket.Close();
                            clipboardSocket.Shutdown(SocketShutdown.Both);
                            clipboardSocket.Close();
                            clipboardSocket.Dispose();
                            this.Dispatcher.Invoke((Action)(() =>
                            {
                                Disconnect_Button.IsEnabled = false;
                                Connect_Button.IsEnabled = true;
                                tbConnectionStatus.Text = "Not connected";
                            }));
                            StopHooks();
                            connected = false;
                        }
                        catch (Exception exc) { MessageBox.Show(exc.ToString()); }
                    }
                    else
                    {
                        controlSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveControlCallback), state);
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        private void InitControlSocket(int p){

            IPEndPoint ipEndPoint = null;
            SocketPermission permission = new SocketPermission(NetworkAccess.Connect, TransportType.Tcp, "", SocketPermission.AllPorts);
            permission.Demand();

            IPAddress ipAddr = IPAddress.Parse(workingServerIp);
            try
            {
                ipEndPoint = new IPEndPoint(ipAddr, p);
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentNullException();
            }

            controlSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            controlSocket.NoDelay = true;
            controlSocket.LingerState = new LingerOption(true, 0);
            controlSocket.ReceiveTimeout = 2000;
            try
            {
                controlSocket.Connect(ipEndPoint);
            }
            catch (ArgumentNullException)
            {
                //address is null.
                ArgumentNullException e = new ArgumentNullException();
                throw e;
            }
            catch (ArgumentException)
            {
                // The length of address is zero.
                ArgumentException e = new ArgumentException();
                throw e;
            }
            catch (SocketException)
            {
                // An error occurred when attempting to access the socket.
                SocketException e = new SocketException();
                throw e;
            }
        }

        private void InitKeyboardSocket(int p)
        {

            IPEndPoint ipEndPoint = null;
            SocketPermission permission = new SocketPermission(NetworkAccess.Connect, TransportType.Tcp, "", SocketPermission.AllPorts);
            permission.Demand();

            IPAddress ipAddr = IPAddress.Parse(workingServerIp);
            try
            {
                ipEndPoint = new IPEndPoint(ipAddr, p);
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentNullException();
            }

            keyboardSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            keyboardSocket.NoDelay = true;
            keyboardSocket.LingerState = new LingerOption(true, 0);
            try
            {
                keyboardSocket.Connect(ipEndPoint);
            }
            catch (ArgumentNullException)
            {
                //address is null.
                ArgumentNullException e = new ArgumentNullException();
                throw e;
            }
            catch (ArgumentException)
            {
                // The length of address is zero.
                ArgumentException e = new ArgumentException();
                throw e;
            }
            catch (SocketException)
            {
                // An error occurred when attempting to access the socket.
                SocketException e = new SocketException();
                throw e;
            }
        }

        private void InitClipboardSocket(int p) 
        {
            IPEndPoint ipEndPoint = null;
            SocketPermission permission = new SocketPermission(NetworkAccess.Connect, TransportType.Tcp, "", SocketPermission.AllPorts);
            permission.Demand();

            IPAddress ipAddr = IPAddress.Parse(workingServerIp);
            try
            {
                ipEndPoint = new IPEndPoint(ipAddr, p);
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentNullException();
            }

            clipboardSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            clipboardSocket.NoDelay = true;
            clipboardSocket.LingerState = new LingerOption(true, 0);
            try
            {
                clipboardSocket.Connect(ipEndPoint);
            }
            catch (ArgumentNullException)
            {
                //address is null.
                ArgumentNullException e = new ArgumentNullException();
                throw e;
            }
            catch (ArgumentException)
            {
                // The length of address is zero.
                ArgumentException e = new ArgumentException();
                throw e;
            }
            catch (SocketException)
            {
                // An error occurred when attempting to access the socket.
                SocketException e = new SocketException();
                throw e;
            }
        }

        private void InitMouseSocket(int p) 
        {
            
            IPEndPoint ipEndPoint = null;
            SocketPermission permission = new SocketPermission( NetworkAccess.Connect, TransportType.Udp, "", SocketPermission.AllPorts);
            permission.Demand();

            IPAddress ipAddr = IPAddress.Parse(workingServerIp);
            try
            {
                ipEndPoint = new IPEndPoint(ipAddr, p);
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentNullException();
            }

            mouseSocket = new Socket(ipAddr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                mouseSocket.Connect(ipEndPoint);
            }
            catch (ArgumentNullException)
            {
                //address is null.
                throw new ArgumentNullException();
            }
            catch (ArgumentException)
            {
                // The length of address is zero.
                throw new ArgumentException();
            }
            catch (SocketException)
            {
                // An error occurred when attempting to access the socket.
                throw new SocketException();
            }
        }

        private void DisconnectClick(object sender, RoutedEventArgs e)
        {
            Disconnect();
        }

        private void AddItemClick(object sender, RoutedEventArgs e)
        {
            String serverName = TextBox_AddName.Text;
            String ipAddress = TextBox_AddIp.Text;
            String password = TextBox_AddPassword.Password;
            String port = TextBox_AddPort.Text;

            if (!string.IsNullOrWhiteSpace(serverName) && !string.IsNullOrWhiteSpace(ipAddress)
                && !string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(port))
            {
                Server s = new Server();

                s.name = serverName;
                s.ipAddress = ipAddress;
                s.password = password;
                s.port = port;

                serverList.Add(s);
                listBoxServers.Items.Add(TextBox_AddName.Text);
                TextBox_AddName.Clear();
                TextBox_AddIp.Clear();
                TextBox_AddPassword.Clear();
                TextBox_AddPort.Clear();
            }
            else
            {
                MessageBox.Show("Fill all fields", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        private void DeleteItemClick(object sender, RoutedEventArgs e)
        {
            String selectedServer = listBoxServers.SelectedItem.ToString();
            foreach (Server s in serverList)
            {
                if (s.name == selectedServer)
                {
                    serverList.Remove(s);
                    listBoxServers.Items.Remove(s.name);
                    TextBox_AddName.Text = "";
                    TextBox_AddPassword.Password = "";
                    TextBox_AddIp.Text = "";
                    TextBox_AddPort.Text = "";
                    break;
                }
            }
        }

        private void SelectItem(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (listBoxServers.SelectedItem != null)
            {
                try
                {
                    workingSelection = listBoxServers.SelectedItem.ToString();

                    foreach (Server server in serverList)
                    {
                        if (server.name.CompareTo(workingSelection) == 0)
                        {
                            TextBox_AddIp.Text = server.ipAddress;
                            TextBox_AddName.Text = server.name;
                            TextBox_AddPort.Text = server.port;
                            TextBox_AddPassword.Password = server.password;
                        }
                    }
                    workingServerIp = TextBox_AddIp.Text;
                    workingPort = int.Parse(TextBox_AddPort.Text);
                    workingPassword = TextBox_AddPassword.Password;
                }
                catch (Exception exc) { MessageBox.Show(exc.ToString()); }
            }

        }

        private void LoadListItem(object sender, RoutedEventArgs e){ }

        private void EditItemClick(object sender, RoutedEventArgs e)
        {
            string ricerca = listBoxServers.SelectedItem.ToString();

            foreach (Server s in serverList)
            {
                if (s.name == ricerca)
                {
                    s.name = TextBox_AddName.Text;
                    s.ipAddress = TextBox_AddIp.Text;
                    s.port = TextBox_AddPort.Text;
                    s.password = TextBox_AddPassword.Password;
                    break;
                }
            }

            listBoxServers.Items.Clear();
            foreach (Server s in serverList)
            {
                listBoxServers.Items.Add(s.name);
            }
            TextBox_AddName.Clear();
            TextBox_AddIp.Clear();
            TextBox_AddPassword.Clear();
            TextBox_AddPort.Clear();

        }

        void MListener_LeftDown(object sender, RawMouseEventArgs args)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                tbMouseCapture.Text = "LEFTDOWN " + args.x + " - " + args.y;
                
                    SendMouse("LEFTDOWN", args.x, args.y, 0);
            }));
        }

        void MListener_LeftUp(object sender, RawMouseEventArgs args)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                tbMouseCapture.Text = "LEFTUP " + args.x + " - " + args.y;
                
                    SendMouse("LEFTUP", args.x, args.y, 0);
            }));
        }

        void MListener_RightDown(object sender, RawMouseEventArgs args)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                tbMouseCapture.Text = "RIGHTDOWN " + args.x + " - " + args.y;
                
                    SendMouse("RIGHTDOWN", args.x, args.y, 0);
            }));
        }

        void MListener_RightUp(object sender, RawMouseEventArgs args)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                tbMouseCapture.Text = "RIGHTUP " + args.x + " - " + args.y;
                
                    SendMouse("RIGHTUP", args.x, args.y, 0);
            }));
        }

        void MListener_MouseMove(object sender, RawMouseEventArgs args)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                tbMouseCapture.Text = "MOVE " + args.x + " - " + args.y;
                
                    SendMouse("MOVE", args.x, args.y, 0);
            }));
        }

        void MListener_MouseWheel(object sender, RawMouseEventArgs args)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                tbMouseCapture.Text = "WHEEL " + args.x + " - " + args.y + " - " + args.data;
                
                    SendMouse("WHEEL", args.x, args.y, args.data);
            }));
        }

        void KListener_KeyDown(object sender, RawKeyEventArgs args)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                tbKeyboardCapture.Text = "DOWN" + args.VKCode ;
                
                    SendKey("DOWN", args.VKCode);
            }));
        }

        void KListener_KeyUp(object sender, RawKeyEventArgs args)
        {
            this.Dispatcher.Invoke((Action)(() => { 
                tbKeyboardCapture.Text = "UP" + args.VKCode;
                
                    SendKey("UP", args.VKCode);
            }));
        }

        private void SendKey(string pressType, int VKCode)
        {
            if (keyboardSocket != null)
            {
                if (keyboardSocket.Connected)
                {
                    string kbEvent = pressType + "+" + VKCode;
                    byte[] kbEventToByte = Encoding.Unicode.GetBytes(kbEvent);
                    int bytesSend = keyboardSocket.Send(kbEventToByte);
                }
            }          
        }

        private void SendMouse(string mouseEventType, int x, int y, int data)
        {
            if (mouseSocket != null)
            {
                if (mouseSocket.Connected)
                {
                    string mouseEvent = mouseEventType + "+" + x + "+" + y + "+" + data;
                    byte[] mouseEventToByte = Encoding.Unicode.GetBytes(mouseEvent);
                    int bytesSend = mouseSocket.Send(mouseEventToByte);
                } 
            }
        }

        private void ActivateControl(object sender, EventArgs e)
        {
            if (connected)
            {
                InitHooks();
                Thread t = new Thread(() => CSender.SendClipboard());
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }
        }

        private void DeactivateControl(object sender, EventArgs e)
        {
            StopHooks();
        }

        private void InitHooks()
        {
            KListener = new KeyboardListener();
            MListener = new MouseListener();
            KListener.KeyDown += new RawKeyEventHandler(KListener_KeyDown);
            KListener.KeyUp += new RawKeyEventHandler(KListener_KeyUp);
            MListener.LeftDown += new RawMouseEventHandler(MListener_LeftDown);
            MListener.LeftUp += new RawMouseEventHandler(MListener_LeftUp);
            MListener.RightDown += new RawMouseEventHandler(MListener_RightDown);
            MListener.RightUp += new RawMouseEventHandler(MListener_RightUp);
            MListener.MouseMove += new RawMouseEventHandler(MListener_MouseMove);
            MListener.MouseWheel += new RawMouseEventHandler(MListener_MouseWheel);
        }

        private void StopHooks()
        {
            try
            {
                KListener.Dispose();
                MListener.Dispose();
            }
            catch (NullReferenceException) { }
        }

        private void DetectShortcut(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) &&
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) &&
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.S))
            {
                StopHooks();
            }

            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) &&
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) &&
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.C))
            {
                InitHooks();
            }

            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) &&
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) &&
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.D))
            {
                SendKey("UP", 160);
                SendKey("UP", 162);
                Disconnect();
            }

            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) &&
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) &&
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.D1))
            {
                SwitchServer(0);
            }

            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) &&
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) &&
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.D2))
            {
                SwitchServer(1);
            }

            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) &&
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) &&
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.D3))
            {
                SwitchServer(2);
            }

            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) &&
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift) &&
                System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.D4))
            {
                SwitchServer(3);
            }
        }

        private void Disconnect()
        {
            try
            {
                string disconnectMessage = "<Disconnect>";
                byte[] disconnectToByte = Encoding.Unicode.GetBytes(disconnectMessage);
                int bytesSend = controlSocket.Send(disconnectToByte);
                controlSocket.Close();
                keyboardSocket.Close();
                clipboardSocket.Shutdown(SocketShutdown.Both);
                clipboardSocket.Close();
                mouseSocket.Close();

                Disconnect_Button.IsEnabled = false;
                Connect_Button.IsEnabled = true;
                tbConnectionStatus.Text = "Not connected";

                StopHooks();
                Connect_Button.IsEnabled = true;
                connected = false;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        private void Connect()
        {
            InitControlSocket(workingPort);

            string passwordMessage = workingPassword + "<PasswordCheck>";
            byte[] passwordToByte = Encoding.Unicode.GetBytes(passwordMessage);
            int bytesSend = controlSocket.Send(passwordToByte);

            int pwdAccepted = ReceivePasswordCheck();

            if (pwdAccepted == 1)
            {
                InitKeyboardSocket(workingPort + 10);
                InitMouseSocket(workingPort + 20);
                InitClipboardSocket(workingPort + 30);
                CSender = new ClipboardSender(clipboardSocket);
                ListenFromServer();

                tbConnectionStatus.Text = "Client connected to " + controlSocket.RemoteEndPoint.ToString();
                Connect_Button.IsEnabled = false;
                Disconnect_Button.IsEnabled = true;
                InitHooks();

                Thread tt = new Thread(() =>
                {
                    while (clipboardSocket.Connected)
                    {
                        Thread t = new Thread(() => CSender.ReceiveClipboard());
                        t.SetApartmentState(ApartmentState.STA);
                        t.Start();
                        t.Join();
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            tbClipboardStatus.Text = "New content on clipboard " + DateTime.Now.ToString();
                        }));
                    }
                });
                tt.Start();
                connected = true;
            }
            else if (pwdAccepted == 0)
            {
                controlSocket.Close();
                Disconnect_Button.IsEnabled = false;
                Connect_Button.IsEnabled = true;
                MessageBox.Show("Wrong Password");
            }
            else
            {
                MessageBox.Show("Server could not check password, try changing port");
                Disconnect_Button.IsEnabled = false;
                Connect_Button.IsEnabled = true;
            }
        }

        private void SwitchServer(int i)
        {
            if (i < serverList.Count)
            {
                if (connected)
                {
                    SendKey("UP", 160);
                    SendKey("UP", 162);
                    Disconnect();
                }

                Server server = serverList[i];
                workingServerIp = server.ipAddress;
                workingPort = int.Parse(server.port);
                workingPassword = server.password;

                Connect();
            }
        }
    }
}
