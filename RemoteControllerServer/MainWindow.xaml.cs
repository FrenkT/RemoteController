using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System.Net.Sockets;
using System.IO;
using Utils.InputGenerator;

namespace RemoteControllerServer
{
    public partial class MainWindow : Window
    {

        Socket sListener, sListenerKb, sListenerM;
        IPEndPoint ipEndPoint, ipEndPointKb, ipEndPointM;
        Socket handler, handlerKb, handlerM;
        String pass = "1234";
        String locIp = GetIP4Address();
        int port_conn = 4510;
        int flag_connection = 0;
        private TextBox tbAux = new TextBox();

        public MainWindow()
        {
            InitializeComponent();
            Start_Button.IsEnabled = true;
            StartListen_Button.IsEnabled = false;
            Close_Button.IsEnabled = false;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Create_TCPConnection();
            Create_TCPConnection_Keyboard();
            Create_UDPConnection_Mouse();
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
            int locPort = port_conn;
            try
            {
                SocketPermission permission = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts);

                sListener = null;

                permission.Demand();

                IPAddress ipAddr = IPAddress.Parse(locIp);

                ipEndPoint = new IPEndPoint(ipAddr, locPort);

                sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                sListener.Bind(ipEndPoint);

                Start_Button.IsEnabled = false;
                StartListen_Button.IsEnabled = true;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }    
        }

        private void Create_TCPConnection_Keyboard() {
            int locPort = port_conn+10;
            try
            {
                SocketPermission permissionKb = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts);

                sListenerKb = null;

                permissionKb.Demand();

                IPAddress ipAddr = IPAddress.Parse(locIp);

                ipEndPointKb = new IPEndPoint(ipAddr, locPort);

                sListenerKb = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                sListenerKb.Bind(ipEndPointKb);
                
                Start_Button.IsEnabled = false;
                StartListen_Button.IsEnabled = true;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
        
        private void Create_UDPConnection_Mouse() {
            int locPort = port_conn+20;
            try
            {
                SocketPermission permissionM = new SocketPermission(NetworkAccess.Accept, TransportType.Udp, "", SocketPermission.AllPorts);
               
                sListenerM = null;

                permissionM.Demand();

                IPAddress ipAddr = IPAddress.Parse(locIp);

                ipEndPointM = new IPEndPoint(ipAddr, locPort);

                sListenerM = new Socket(ipAddr.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                sListenerM.Bind(ipEndPointM);

                Start_Button.IsEnabled = false;
                StartListen_Button.IsEnabled = true;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
        
        private void Listen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                sListener.Listen(1);
                sListenerKb.Listen(1);
                
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                sListener.BeginAccept(aCallback, sListener);
                
                // KB
                AsyncCallback aCallback2 = new AsyncCallback(AcceptCallback);
                sListenerKb.BeginAccept(aCallback2, sListenerKb);
                
                // Mouse
                ReceiveMouse();

                tbConnectionStatus.Text = "Server is now listening on " + ipEndPoint.Address + " port: " + ipEndPoint.Port;
                StartListen_Button.IsEnabled = false;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        public void ReceiveMouse()
        {
            byte[] buffer = new byte[1024];
            Socket handler = sListenerM;

            object[] obj = new object[2];
            obj[0] = buffer;
            obj[1] = handler;

            handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallbackMouse), obj);
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = null;
            Socket handler = null;
            try
            {
                byte[] buffer = new byte[1024];

                listener = (Socket)ar.AsyncState;
                EndPoint endpoint = listener.LocalEndPoint;
                handler = listener.EndAccept(ar);
                handler.NoDelay = true;

                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = handler;
                if (endpoint.GetHashCode() == sListener.LocalEndPoint.GetHashCode())
                {
                    handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallbackCONNECTION), obj);
                }
                if (endpoint.GetHashCode() == sListenerKb.LocalEndPoint.GetHashCode())
                {
                    handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallbackKB), obj);
                }
                
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                listener.BeginAccept(aCallback, listener);
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        public void ReceiveCallbackCONNECTION(IAsyncResult ar)
        {
            try
            {
                object[] obj = new object[2];
                obj = (object[])ar.AsyncState;

                byte[] buffer = (byte[])obj[0];

                handler = (Socket)obj[1];

                string content = string.Empty;

                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    content += Encoding.Unicode.GetString(buffer, 0, bytesRead);

                    if (content.IndexOf("<Client Quit>") > -1)
                    {
                        string str = content.Substring(0, content.LastIndexOf("<Client Quit>"));
                        Verifica_Password(str);
                    }
                    else
                    {
                        byte[] buffernew = new byte[1024];
                        obj[0] = buffernew;
                        obj[1] = handler;

                        handler.BeginReceive(buffernew, 0, buffernew.Length, SocketFlags.None, new AsyncCallback(ReceiveCallbackCONNECTION), obj);
                    }
                }
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        public void ReceiveCallbackKB(IAsyncResult ar)
        {
            try
            {
                object[] obj = new object[2];
                obj = (object[])ar.AsyncState;

                byte[] buffer = (byte[])obj[0];

                handlerKb = (Socket)obj[1];

                string content = string.Empty;

                int bytesRead = handlerKb.EndReceive(ar);

                if (bytesRead > 0)
                {
                    content += Encoding.Unicode.GetString(buffer, 0, bytesRead);

                    Parse_KB_Event(content);

                    byte[] buffernew = new byte[1024];
                    obj[0] = buffernew;
                    obj[1] = handlerKb;

                    handlerKb.BeginReceive(buffernew, 0, buffernew.Length, SocketFlags.None, new AsyncCallback(ReceiveCallbackKB), obj);
                }
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
        
        public void ReceiveCallbackMouse(IAsyncResult ar)
        {
            try
            {
                object[] obj = new object[2];
                obj = (object[])ar.AsyncState;

                byte[] buffer = (byte[])obj[0];

                handlerM = (Socket)obj[1];

                string content = string.Empty;

                int bytesRead = handlerM.EndReceive(ar);

                if (bytesRead > 0)
                {
                    content += Encoding.Unicode.GetString(buffer, 0, bytesRead);
                    
                    Parse_Mouse_Event(content);

                    byte[] buffernew = new byte[1024];
                    obj[0] = buffernew;
                    obj[1] = handlerM;
                    handlerM.BeginReceive(buffernew, 0, buffernew.Length,SocketFlags.None,new AsyncCallback(ReceiveCallbackMouse), obj);
                }
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }        

        public void Verifica_Password(String inputpassword) {
            try
            {
                if (pass.CompareTo(inputpassword) == 0)
                {
                    // Convert byte array to string 
                    string str = "ok";
                    flag_connection = 1;
                    // Prepare the reply message 
                    byte[] byteData =
                        Encoding.Unicode.GetBytes(str);
                
                    // Sends data asynchronously to a connected Socket 
                    handler.BeginSend(byteData, 0, byteData.Length, 0,
                        new AsyncCallback(SendCallback), handler);
                }
                else {
                    // Convert byte array to string 
                    string str = "quit";

                    // Prepare the reply message 
                    byte[] byteData =
                        Encoding.Unicode.GetBytes(str);

                    // Sends data asynchronously to a connected Socket 
                    handler.BeginSend(byteData, 0, byteData.Length, 0,
                        new AsyncCallback(SendCallback), handler);
                }
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
        
        public void SendCallback(IAsyncResult ar)
        {
            try
            {
                // A Socket which has sent the data to remote host 
                Socket handler = (Socket)ar.AsyncState;

                // The number of bytes sent to the Socket 
                int bytesSend = handler.EndSend(ar);
                if (flag_connection == 1)
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {

                        tbConnectionStatus.Text = "Connection accepted.";
                        tbKeyboardStatus.Text = "Connection Keyboard accepted.";
                        tbMouseStatus.Text = "Connection Mouse accepted.";
                    }));
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
        }
    }
}
