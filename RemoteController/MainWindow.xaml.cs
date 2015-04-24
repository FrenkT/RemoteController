using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using Utils.Keyboard;
using Utils.Mouse;

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
        public StringBuilder sb = new StringBuilder();
    }

    public partial class MainWindow : Window
    {
        Socket controlSocket, keyboardSocket, mouseSocket;
        //Socket handler;
        String workingServerIp = "";
        int workingPort = 0;
        String workingPassword = "";
        String workingSelection = "";
        List<Server> serverList = new List<Server>();
        KeyboardListener KListener;
        MouseListener MListener;

        public MainWindow()
        {
            InitializeComponent();
            Disconnect_Button.IsEnabled = true;
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            InitControlSocket(workingPort);

            string passwordMessage = workingPassword + "<PasswordCheck>";
            byte[] passwordToByte = Encoding.Unicode.GetBytes(passwordMessage);
            int bytesSend = controlSocket.Send(passwordToByte);

            if (ReceivePasswordCheck())
            {
                InitKeyboardSocket(workingPort+10);
                InitMouseSocket(workingPort+20);

                //controlSocket.Listen(1);
                //AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                //controlSocket.BeginAccept(aCallback, controlSocket);
                ListenFromServer();

                tbConnectionStatus.Text = "Client connected to " + controlSocket.RemoteEndPoint.ToString();
                tbKeyboardConnection.Text = "Client connected to " + keyboardSocket.RemoteEndPoint.ToString();
                tbMouseConnection.Text = "Client connected to " + mouseSocket.RemoteEndPoint.ToString();
                Connect_Button.IsEnabled = false;
                Disconnect_Button.IsEnabled = true;
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
            else 
            {
                controlSocket.Close();
                Disconnect_Button.IsEnabled = false;
                Connect_Button.IsEnabled = true;
                MessageBox.Show("Wrong Password");
            } 
        }

        private bool ReceivePasswordCheck()
        {
            bool accepted = false;
            byte[] bytes = new byte[1024];
            try
            {
                int bytesReceived = controlSocket.Receive(bytes);
                String passwordCheck = Encoding.Unicode.GetString(bytes, 0, bytesReceived);
                if (passwordCheck.CompareTo("ok") == 0) {
                    accepted = true;
                }
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
            return accepted;
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
                        {   // TODO check socket closing
                            controlSocket.Close();
                            keyboardSocket.Close();
                            mouseSocket.Close();
                            this.Dispatcher.Invoke((Action)(() =>
                            {
                                Disconnect_Button.IsEnabled = false;
                                Connect_Button.IsEnabled = true;
                                tbConnectionStatus.Text = "Not connected";
                                tbKeyboardConnection.Text = "Not connected";
                                tbMouseConnection.Text = "Not connected";
                            }));
                            KListener.Dispose();
                            MListener.Dispose();
                        }
                        catch (Exception exc) { MessageBox.Show(exc.ToString()); }
                    }
                    else
                    {
                        controlSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveControlCallback), state);
                    }
                }
            }
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

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                controlSocket.Close();
                keyboardSocket.Close();
                mouseSocket.Close();
                
                Disconnect_Button.IsEnabled = false;
                Connect_Button.IsEnabled = true;
                tbConnectionStatus.Text = "Not connected";
                tbKeyboardConnection.Text = "Not connected";
                tbMouseConnection.Text = "Not connected";

                KListener.Dispose();
                MListener.Dispose();
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        private void Add_Item_Click(object sender, RoutedEventArgs e)
        {
            String serverName = TextBox_AddName.Text;
            String ipAddress = TextBox_AddIp.Text;
            String password = TextBox_AddPassword.Text;
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

        private void Delete_Item_Click(object sender, RoutedEventArgs e)
        {
            String selectedServer = listBoxServers.SelectedItem.ToString();
            foreach (Server s in serverList)
            {
                if (s.name == selectedServer)
                {
                    serverList.Remove(s);
                    listBoxServers.Items.Remove(s.name);
                    break;
                }
            }
        }

        private void Select_Item(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
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
                            TextBox_AddPassword.Text = server.password;
                        }
                    }
                    workingServerIp = TextBox_AddIp.Text;
                    workingPort = int.Parse(TextBox_AddPort.Text);
                    workingPassword = TextBox_AddPassword.Text;
                }
                catch (Exception exc) { MessageBox.Show(exc.ToString()); }
            }

        }

        private void LoadList_Item(object sender, RoutedEventArgs e){ }

        private void Edit_Item_Click(object sender, RoutedEventArgs e)
        {
            string ricerca = listBoxServers.SelectedItem.ToString();

            foreach (Server s in serverList)
            {
                if (s.name == ricerca)
                {
                    s.name = TextBox_AddName.Text;
                    s.ipAddress = TextBox_AddIp.Text;
                    s.port = TextBox_AddPort.Text;
                    s.password = TextBox_AddPassword.Text;
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

        private void Application_Startup(object sender, RoutedEventArgs e)
        {
            /*KListener.KeyDown += new RawKeyEventHandler(KListener_KeyDown);
            KListener.KeyUp += new RawKeyEventHandler(KListener_KeyUp);
            MListener.LeftDown += new RawMouseEventHandler(MListener_LeftDown);
            MListener.LeftUp += new RawMouseEventHandler(MListener_LeftUp);
            MListener.RightDown += new RawMouseEventHandler(MListener_RightDown);
            MListener.RightUp += new RawMouseEventHandler(MListener_RightUp);
            MListener.MouseMove += new RawMouseEventHandler(MListener_MouseMove);
            MListener.MouseWheel += new RawMouseEventHandler(MListener_MouseWheel);*/
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

        private void Application_Exit(object sender, EventArgs e)
        {
            //KListener.Dispose();
            //MListener.Dispose();
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

    }
}
