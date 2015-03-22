﻿using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Sockets;

namespace RemoteControllerServer
{
    public partial class MainWindow : Window
    {

        SocketPermission permission;
        Socket sListener;
        IPEndPoint ipEndPoint;
        Socket handler;

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
            String locIp = "127.0.0.1";
            int locPort = 4510;
            try
            {
                // Creates one SocketPermission object for access restrictions
                permission = new SocketPermission(
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

                tbConnectionStatus.Text = "Server started.";

                Start_Button.IsEnabled = false;
                StartListen_Button.IsEnabled = true;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        private void Listen_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                // Places a Socket in a listening state and specifies the maximum 
                // Length of the pending connections queue 
                sListener.Listen(10);

                // Begins an asynchronous operation to accept an attempt 
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);

                // Accepting connections asynchronously gives you the ability to send and receive data within a separate execution thread
                // Begins an asynchronous operation to accept an incoming connection attempt.
                sListener.BeginAccept(aCallback, sListener);
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
                handler.NoDelay = false;

                // Creates one object array for passing data 
                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = handler;
                this.Dispatcher.Invoke((Action)(() => { tbConnectionStatus.Text = "Connection accepted."; }));
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);

                listener.BeginAccept(aCallback, listener);
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
    }
}
