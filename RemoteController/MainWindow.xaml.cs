using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using Utils.Keyboard;

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

        // Receiving byte array  
        byte[] bytes = new byte[1024];
        Socket senderSock;
        String workingServerIp = "";
        String workingPort = "";
        String workingPassword = "";
        String workingSelection = "";
        List<Server> serverList = new List<Server>();
        KeyboardListener KListener = new KeyboardListener();

        public MainWindow()
        {
            InitializeComponent();
            Disconnect_Button.IsEnabled = true;
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            IPEndPoint ipEndPoint = null;
            int p = Convert.ToInt32(workingPort);

            // Create one SocketPermission for socket access restrictions 
            SocketPermission permission = new SocketPermission(
                NetworkAccess.Connect,    // Connection permission 
                TransportType.Tcp,        // Defines transport types 
                "",                       // Gets the IP addresses 
                SocketPermission.AllPorts // All ports 
                );

            // Ensures the code to have permission to access a Socket 
            permission.Demand();

            // Gets first IP address associated with a localhost 
            IPAddress ipAddr = IPAddress.Parse(workingServerIp);

            try
            {
                // Creates a network endpoint 
                ipEndPoint = new IPEndPoint(ipAddr, p);
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentNullException();
            }

            // Create one Socket object to setup Tcp connection 
            senderSock = new Socket(
                ipAddr.AddressFamily,// Specifies the addressing scheme 
                SocketType.Stream,   // The type of socket  
                ProtocolType.Tcp     // Specifies the protocols  
                );

            senderSock.NoDelay = false;   // Using the Nagle algorithm 
            try
            {
                // Establishes a connection to a remote host 
                senderSock.Connect(ipEndPoint);
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


            tbConnectionStatus.Text = "Client connected to " + senderSock.RemoteEndPoint.ToString();

            Connect_Button.IsEnabled = false;
            Disconnect_Button.IsEnabled = true;
        }

        /*
        private void sendUsernPasswd(String u, String p) { 
            // Sending User+Passwd
            UserClient Uclient = new UserClient();
            Uclient.setUsername(u);
            Uclient.setPassword(p);
           
        }
        */

        /*public string GetMD5(String password)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(password));
            byte[] result = md5.Hash;
            StringBuilder str = new StringBuilder();
            for (int i = 1; i < result.Length; i++)
            {
                str.Append(result[i].ToString("x2"));
            }
            return str.ToString();
        }*/

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Closes the Socket connection and releases all resources 
                senderSock.Close();

                Disconnect_Button.IsEnabled = false;
                Connect_Button.IsEnabled = true;
                tbConnectionStatus.Text = "Not connected";
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
                MessageBox.Show("Fill al fields", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    workingPort = TextBox_AddPort.Text;
                    workingPassword = TextBox_AddPassword.Text;
                }
                catch (Exception exc) { MessageBox.Show(exc.ToString()); }
            }

        }

        private void LoadList_Item(object sender, RoutedEventArgs e)
        {

        }

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
        }

        private void Application_Startup(object sender, RoutedEventArgs e)
        {
            KListener.KeyDown += new RawKeyEventHandler(KListener_KeyDown);
            KListener.KeyUp += new RawKeyEventHandler(KListener_KeyUp);
        }

        void KListener_KeyDown(object sender, RawKeyEventArgs args)
        {
            this.Dispatcher.Invoke((Action)(() => { tbKeyboardCapture.Text += "DOWN" + args.Key; }));
        }

        void KListener_KeyUp(object sender, RawKeyEventArgs args)
        {
            this.Dispatcher.Invoke((Action)(() => { tbKeyboardCapture.Text += "UP" + args.Key; }));
        }

        private void Application_Exit(object sender, EventArgs e)
        {
            KListener.Dispose();
        }


    }
}
