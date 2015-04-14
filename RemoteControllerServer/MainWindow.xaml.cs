﻿using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Utils.KeyboardSender;

namespace RemoteControllerServer
{
    public partial class MainWindow : Window
    {
        Socket sListener, sListener2;
        IPEndPoint ipEndPoint, ipEndPoint2;
        Socket handler;
        String pass = "1234";

        private TextBox tbAux = new TextBox();

        public MainWindow()
        {
            InitializeComponent();
            //tbAux.SelectionChanged += tbAux_SelectionChanged;

            Start_Button.IsEnabled = true;
            StartListen_Button.IsEnabled = false;
            Close_Button.IsEnabled = false;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            
            Create_TCPConnection();
                        
            Create_TCPConnection_Keyboard();
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

        private void Create_TCPConnection() {
            
            // trovare metodo che definisca dinamicamente ip macchina nella rete
            String locIp = GetIP4Address();
            int locPort = 4510;
            try
            {
                // Creates one SocketPermission object for access restrictions
                SocketPermission permission = new SocketPermission(
                NetworkAccess.Accept,     // Allowed to accept connections 
                TransportType.Tcp,        // Defines transport types 
                "",                       // The IP addresses of local host 
                SocketPermission.AllPorts // Specifies all ports 
                );

                // Listening Socket object 
                sListener = null;

                // Ensures the code to have permission to access a Socket 
                permission.Demand();


                IPAddress ipAddr = IPAddress.Parse(locIp);

                // Creates a network endpoint 
                ipEndPoint = new IPEndPoint(ipAddr, locPort);

                // Create one Socket object to listen the incoming connection 
                sListener = new Socket(
                    ipAddr.AddressFamily,
                    SocketType.Stream,
                    ProtocolType.Tcp
                    );

                // Associates a Socket with a local endpoint 
                sListener.Bind(ipEndPoint);

                //tbConnectionStatus.Text = "Server started.";

                Start_Button.IsEnabled = false;
                StartListen_Button.IsEnabled = true;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }    
        }

        private void Create_TCPConnection_Keyboard() {
            // trovare metodo che definisca dinamicamente ip macchina nella rete
            String locIp = GetIP4Address();
            int locPort = 4520;
            try
            {
                // Creates one SocketPermission object for access restrictions
                SocketPermission permissionKb = new SocketPermission(
                NetworkAccess.Accept,     // Allowed to accept connections 
                TransportType.Tcp,        // Defines transport types 
                "",                       // The IP addresses of local host 
                SocketPermission.AllPorts // Specifies all ports 
                );

                // Listening Socket object 
                sListener2 = null;

                // Ensures the code to have permission to access a Socket 
                permissionKb.Demand();

                IPAddress ipAddr = IPAddress.Parse(locIp);

                // Creates a network endpoint 
                ipEndPoint2 = new IPEndPoint(ipAddr, locPort);

                // Create one Socket object to listen the incoming connection 
                sListener2 = new Socket(
                    ipAddr.AddressFamily,
                    SocketType.Stream,
                    ProtocolType.Tcp
                    );

                // Associates a Socket with a local endpoint 
                sListener2.Bind(ipEndPoint2);

                //tbConnectionStatus.Text = "Server started.";
                //tbKeyboardStatus.Text = "Server started.";
                
                Start_Button.IsEnabled = false;
                StartListen_Button.IsEnabled = true;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
        
        private void Create_UDPConnection_Mouse() { 
            
        }
        
        private void Listen_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                // Places a Socket in a listening state and specifies the maximum 
                // Length of the pending connections queue 
                sListener.Listen(10);
                sListener2.Listen(10);
                // Begins an asynchronous operation to accept an attempt 
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                // Accepting connections asynchronously gives you the ability to send and receive data within a separate execution thread
                // Begins an asynchronous operation to accept an incoming connection attempt.
                sListener.BeginAccept(aCallback, sListener);
                AsyncCallback aCallback2 = new AsyncCallback(AcceptCallback);
                sListener2.BeginAccept(aCallback2, sListener2);
                /*
                    BeginAccept(AsyncCallback callback, object state):
                    Essentially, after calling the Listen() method of the main Socket object, you call this asynchronous
                    method and specify a call back function (1), which you designated to do the further processing related 
                    to the client connection. The state object (2) can be null in this particular instance.
                */

                tbConnectionStatus.Text = "Server is now listening on " + ipEndPoint.Address + " port: " + ipEndPoint.Port;
                StartListen_Button.IsEnabled = false;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        /* 
           The accept callback method is called when a new connection request is received on the socket
           The accept callback method implements the AsyncCallback delegate; it returns a void and takes 
           a single parameter of type IAsyncResult 
        */

        public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = null;

            // A new Socket to handle remote host communication 
            Socket handler = null;
            try
            {
                // Receiving byte array 
                byte[] buffer = new byte[1024];

                // Get Listening Socket object 
                listener = (Socket)ar.AsyncState;

                handler = listener.EndAccept(ar);

                // Using the Nagle algorithm 
                handler.NoDelay = true;

                // Creates one object array for passing data 
                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = handler;
                this.Dispatcher.Invoke((Action)(() => { 
                    
                    tbConnectionStatus.Text = "Connection accepted.";
                    tbKeyboardStatus.Text = "Connection Keyboard accepted.";
                }));
                handler.BeginReceive(
                     buffer,        // An array of type Byt for received data 
                     0,             // The zero-based position in the buffer  
                     buffer.Length, // The number of bytes to receive 
                     SocketFlags.None,// Specifies send and receive behaviors 
                     new AsyncCallback(ReceiveCallback),//An AsyncCallback delegate 
                     obj            // Specifies infomation for receive operation 
                     );
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);

                listener.BeginAccept(aCallback, listener);
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
        
        public void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Fetch a user-defined object that contains information 
                object[] obj = new object[2];
                obj = (object[])ar.AsyncState;

                // Received byte array 
                byte[] buffer = (byte[])obj[0];

                // A Socket to handle remote host communication. 
                handler = (Socket)obj[1];

                // Received message 
                string content = string.Empty;


                // The number of bytes received. 
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    content += Encoding.Unicode.GetString(buffer, 0,
                        bytesRead);

                    this.Dispatcher.Invoke((Action)(() => { tbConnectionStatus.Text = content; }));
                    Parse_KB_Event(content);

                    // If message contains "<Client Quit>", finish receiving
                    if (content.IndexOf("<Client Quit>") > -1)
                    {
                        // Convert byte array to string
                        string str = content.Substring(0, content.LastIndexOf("<Client Quit>"));
                                            }
                    else
                    {
                        // Continues to asynchronously receive data
                        byte[] buffernew = new byte[1024];
                        obj[0] = buffernew;
                        obj[1] = handler;

                        // Call the BeginReceive() asynchronous function to wait for any socket write activity by the server.
                        handler.BeginReceive(buffernew, 0, buffernew.Length,
                            SocketFlags.None,
                            new AsyncCallback(ReceiveCallback), obj);
                    }
                }
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }



        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sListener.Connected)
                {
                    sListener.Shutdown(SocketShutdown.Receive);
                    sListener.Close();
                }

                Close_Button.IsEnabled = false;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        private void Parse_KB_Event(string kbEvent)
        {
            //KeyboardSender.SendKeyPress();
            string[] words = kbEvent.Split(new char[] { '+' }, 2);
            if (words[0] == "UP")
            {
                KeyboardSender.SendKeyUP(words[1]);
            }
            if (words[0] == "DOWN")
            {
                KeyboardSender.SendKeyDown(words[1]);
            }
            //KeyboardSender.SendKeyDown(words[1]);
            //KeyboardSender.SendKeyUP(words[1]);
        }
    }
}
