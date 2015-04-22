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

    public partial class MainWindow : Window
    {
        Socket senderSock, senderSock_Keyboard, senderSock_mouse;
        String workingServerIp = "";
        int workingPort = 0;
        String workingPassword = "";
        String workingSelection = "";
        List<Server> serverList = new List<Server>();
        KeyboardListener KListener = new KeyboardListener();
        MouseListener MListener = new MouseListener();

        public MainWindow()
        {
            InitializeComponent();
            Disconnect_Button.IsEnabled = true;
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            Create_SocketTCP(workingPort);

            string passwordMessage = workingPassword + "<PasswordCheck>";
            byte[] msg = Encoding.Unicode.GetBytes(passwordMessage);

            int bytesSend = senderSock.Send(msg);

            if (ReceivePasswordCheck())
            {
                Create_SocketTCP_Keyboard(workingPort+10);
                Create_SocketUDP(workingPort+20);

                tbConnectionStatus.Text = "Client connected to " + senderSock.RemoteEndPoint.ToString();
                tbKeyboardConnection.Text = "Client connected to " + senderSock_Keyboard.RemoteEndPoint.ToString();
                tbMouseConnection.Text = "Client connected to " + senderSock_mouse.RemoteEndPoint.ToString();
                Connect_Button.IsEnabled = false;
                Disconnect_Button.IsEnabled = true;
            }
            else 
            {
                senderSock.Close();
                Disconnect_Button.IsEnabled = false;
                Connect_Button.IsEnabled = true;
                MessageBox.Show("Wrong Password");
            } 
        }

        private bool ReceivePasswordCheck()
        {
            byte[] bytes = new byte[1024];
            bool accepted = false;
            try
            {
                int bytesRec = senderSock.Receive(bytes);
                String theMessageToReceive = Encoding.Unicode.GetString(bytes, 0, bytesRec);
                if (theMessageToReceive.CompareTo("ok") == 0) {
                    accepted = true;
                }
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
            return accepted;
        }

        private void Create_SocketTCP(int p){

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

            senderSock = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            senderSock.NoDelay = true;
            try
            {
                senderSock.Connect(ipEndPoint);
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

        private void Create_SocketTCP_Keyboard(int p)
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

            senderSock_Keyboard = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            senderSock_Keyboard.NoDelay = true;
            try
            {
                senderSock_Keyboard.Connect(ipEndPoint);
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

        private void Create_SocketUDP(int p) {
            
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

            senderSock_mouse = new Socket(ipAddr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                senderSock_mouse.Connect(ipEndPoint);
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
                senderSock.Close();
                senderSock_Keyboard.Close();
                senderSock_mouse.Close();
                
                Disconnect_Button.IsEnabled = false;
                Connect_Button.IsEnabled = true;
                tbConnectionStatus.Text = "Not connected";
                tbKeyboardConnection.Text = "Not connected";
                tbMouseConnection.Text = "Not connected";
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
            KListener.KeyDown += new RawKeyEventHandler(KListener_KeyDown);
            KListener.KeyUp += new RawKeyEventHandler(KListener_KeyUp);
            MListener.LeftDown += new RawMouseEventHandler(MListener_LeftDown);
            MListener.LeftUp += new RawMouseEventHandler(MListener_LeftUp);
            MListener.RightDown += new RawMouseEventHandler(MListener_RightDown);
            MListener.RightUp += new RawMouseEventHandler(MListener_RightUp);
            MListener.MouseMove += new RawMouseEventHandler(MListener_MouseMove);
            MListener.MouseWheel += new RawMouseEventHandler(MListener_MouseWheel);
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
            KListener.Dispose();
        }

        private void SendKey(string pressType, int VKCode)
        {
            if (senderSock_Keyboard != null)
            {
                if (senderSock_Keyboard.Connected)
                {
                    string kbEvent = pressType + "+" + VKCode;
                    byte[] ReadyKbEvent = Encoding.Unicode.GetBytes(kbEvent);
                    int bytesSend = senderSock_Keyboard.Send(ReadyKbEvent);
                }
            }          
        }

        private void SendMouse(string mouseEventType, int x, int y, int data)
        {
            if (senderSock_mouse != null)
            {
                if (senderSock_mouse.Connected)
                {
                    string mouseEvent = mouseEventType + "+" + x + "+" + y + "+" + data;
                    byte[] ReadyMouseEvent = Encoding.Unicode.GetBytes(mouseEvent);
                    int bytesSend = senderSock_mouse.Send(ReadyMouseEvent);
                } 
            }
        }

    }
}
