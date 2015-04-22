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
    public class UdpState
    {
        public IPEndPoint e { get; set; }
        public UdpClient u { get; set; }
    }

    public partial class MainWindow : Window
    {

        Socket sListener, sListenerKb, sListenerM;
        IPEndPoint ipEndPoint, ipEndPointKb, ipEndPointM;
        EndPoint RemoteEndpoint;
        Socket handler, handlerKb, handlerM;
        String pass = "1234";
        int port_conn = 4510, port_kb = 4520, port_m = 4530;
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
            String locIp = GetIP4Address();
            int locPort = port_conn;
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

                Start_Button.IsEnabled = false;
                StartListen_Button.IsEnabled = true;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }    
        }

        private void Create_TCPConnection_Keyboard() {
            String locIp = GetIP4Address();
            int locPort = port_kb;
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
                sListenerKb = null;

                // Ensures the code to have permission to access a Socket 
                permissionKb.Demand();

                IPAddress ipAddr = IPAddress.Parse(locIp);

                // Creates a network endpoint 
                ipEndPointKb = new IPEndPoint(ipAddr, locPort);

                // Create one Socket object to listen the incoming connection 
                sListenerKb = new Socket(
                    ipAddr.AddressFamily,
                    SocketType.Stream,
                    ProtocolType.Tcp
                    );

                // Associates a Socket with a local endpoint 
                sListenerKb.Bind(ipEndPointKb);
                
                Start_Button.IsEnabled = false;
                StartListen_Button.IsEnabled = true;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
        
        private void Create_UDPConnection_Mouse() {
            String locIp = GetIP4Address();
            int locPort = port_m;
            try
            {
                // Creates one SocketPermission object for access restrictions
                SocketPermission permissionM = new SocketPermission(
                NetworkAccess.Accept,     // Allowed to accept connections 
                TransportType.Udp,        // Defines transport types 
                "",                       // The IP addresses of local host 
                SocketPermission.AllPorts // Specifies all ports 
                );
               
                // Listening Socket object 
                sListenerM = null;

                // Ensures the code to have permission to access a Socket 
                permissionM.Demand();

                IPAddress ipAddr = IPAddress.Parse(locIp);

                // Creates a network endpoint 
                ipEndPointM = new IPEndPoint(ipAddr, locPort);

                // Create one Socket object to listen the incoming connection 
                sListenerM = new Socket(
                    ipAddr.AddressFamily,
                    SocketType.Dgram,
                    ProtocolType.Udp
                    );

                // Associates a Socket with a local endpoint 
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
                // Length of the pending connections queue 
                sListener.Listen(1);
                sListenerKb.Listen(1);
                //sListenerM.Listen(1); // UDP è connectionless non necessita la listen
                
                // Begins an asynchronous operation to accept an attempt 
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                
                // Connection
                sListener.BeginAccept(aCallback, sListener); // posso usare la Accept solo se prima ho fatto la Listen
                
                // KB
                AsyncCallback aCallback2 = new AsyncCallback(AcceptCallback2);
                sListenerKb.BeginAccept(aCallback2, sListenerKb); // posso usare la Accept solo se prima ho fatto la Listen
                
                // Mouse
                /*
                 With UDP, you just have to Bind to the the port and then use the ReceiveFrom and SendFrom methods 
                 or the equivalent async methods.
                 If you are using a connectionless protocol such as UDP, you can use BeginSendTo and EndSendTo to 
                 send datagrams, and BeginReceiveFrom and EndReceiveFrom to receive datagrams.
                 */
                //AsyncCallback aCallback3 = new AsyncCallback(AcceptCallback3);
                //sListenerM.BeginAccept(aCallback3, sListenerM);

                UdpClient u = new UdpClient(ipEndPoint);
                UdpState s = new UdpState();
                s.e = ipEndPoint;
                s.u = u;
                u.BeginReceive(new AsyncCallback(ReceiveCallbackMouse), s);

                tbConnectionStatus.Text = "Server is now listening on " + ipEndPoint.Address + " port: " + ipEndPoint.Port;
                StartListen_Button.IsEnabled = false;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = null;
            Socket handler = null;
            try
            {
                // Receiving byte array 
                byte[] buffer = new byte[1024];

                // Get Listening Socket object 
                listener = (Socket)ar.AsyncState;
                EndPoint endpoint = listener.LocalEndPoint;
                
                handler = listener.EndAccept(ar);

                handler.NoDelay = true;

                // Creates one object array for passing data 
                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = handler;
                handler.BeginReceive(
                        buffer,        // An array of type Byt for received data 
                        0,             // The zero-based position in the buffer  
                        buffer.Length, // The number of bytes to receive 
                        SocketFlags.None,// Specifies send and receive behaviors 
                        new AsyncCallback(ReceiveCallbackCONNECTION),//An AsyncCallback delegate 
                        obj            // Specifies infomation for receive operation 
                        );
               
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);

                listener.BeginAccept(aCallback, listener);
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        public void AcceptCallback2(IAsyncResult ar)
        {
            Socket listener = null;
            Socket handler = null;

            try
            {
                // Receiving byte array 
                byte[] buffer = new byte[1024];

                // Get Listening Socket object 
                listener = (Socket)ar.AsyncState;
                EndPoint endpoint = listener.LocalEndPoint;

                handler = listener.EndAccept(ar);

                if (listener.ProtocolType.ToString().CompareTo("Tcp") == 0)
                {
                    // Using the Nagle algorithm 
                    handler.NoDelay = true;
                }

                // Creates one object array for passing data 
                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = handler;
                handler.BeginReceive(
                        buffer,        // An array of type Byt for received data 
                        0,             // The zero-based position in the buffer  
                        buffer.Length, // The number of bytes to receive 
                        SocketFlags.None,// Specifies send and receive behaviors 
                        new AsyncCallback(ReceiveCallbackKB),//An AsyncCallback delegate 
                        obj            // Specifies infomation for receive operation 
                        );
               
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback2);

                listener.BeginAccept(aCallback, listener);
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        public void AcceptCallback3(IAsyncResult ar)
        {
            Socket listener = null;
            Socket handler = null;
            try
            {
                // Receiving byte array 
                byte[] buffer = new byte[1024];

                // Get Listening Socket object 
                listener = (Socket)ar.AsyncState;
                EndPoint endpoint = listener.LocalEndPoint;

                //handler = listener.EndAccept(ar);

                // Creates one object array for passing data 
                object[] obj = new object[2];
                obj[0] = buffer;
                obj[1] = handler;
                RemoteEndpoint = sListener.RemoteEndPoint;
                handler.BeginReceiveFrom(
                    buffer,        // An array of type Byt for received data 
                    0,             // The zero-based position in the buffer  
                    buffer.Length, // The number of bytes to receive 
                    SocketFlags.None,// Specifies send and receive behaviors 
                    ref RemoteEndpoint,
                    new AsyncCallback(ReceiveCallbackMouse),//An AsyncCallback delegate 
                    obj            // Specifies infomation for receive operation 
                    );   
                 
                AsyncCallback aCallback = new AsyncCallback(AcceptCallback3);
                listener.BeginAccept(aCallback, listener);
                
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }


        public void ReceiveCallbackCONNECTION(IAsyncResult ar)
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

                    // If message contains "<Client Quit>", finish receiving
                    if (content.IndexOf("<Client Quit>") > -1)
                    {
                        // Convert byte array to string
                        string str = content.Substring(0, content.LastIndexOf("<Client Quit>"));
                        Verifica_Password(str);
                    }
                    else
                    {
                        // Continues to asynchronously receive data
                        byte[] buffernew = new byte[1024];
                        obj[0] = buffernew;
                        obj[1] = handler;

                        handler.BeginReceive(buffernew, 0, buffernew.Length,
                            SocketFlags.None,
                            new AsyncCallback(ReceiveCallbackCONNECTION), obj);
                    }
                }
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        public void ReceiveCallbackKB(IAsyncResult ar)
        {
            UdpClient u = (UdpClient)((UdpState)(ar.AsyncState)).u;
            IPEndPoint e = (IPEndPoint)((UdpState)(ar.AsyncState)).e;

            byte[] buffer = u.EndReceive(ar, ref e);

            u.BeginReceive(new AsyncCallback(ReceiveCallbackMouse), ar);
            /*try
            {
                // Fetch a user-defined object that contains information 
                object[] obj = new object[2];
                obj = (object[])ar.AsyncState;

                // Received byte array 
                byte[] buffer = (byte[])obj[0];

                // A Socket to handle remote host communication. 
                handlerKb = (Socket)obj[1];

                // Received message 
                string content = string.Empty;

                // The number of bytes received. 
                int bytesRead = handlerKb.EndReceive(ar);

                if (bytesRead > 0)
                {
                    content += Encoding.Unicode.GetString(buffer, 0,
                        bytesRead);

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
                        obj[1] = handlerKb;

                        handlerKb.BeginReceive(
                            buffernew, 
                            0, 
                            buffernew.Length,
                            SocketFlags.None,
                            new AsyncCallback(ReceiveCallbackKB), obj);
                    }
                }
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }*/
        }
        
        public void ReceiveCallbackMouse(IAsyncResult ar)
        {
            try
            {
                // Fetch a user-defined object that contains information 
                object[] obj = new object[2];
                obj = (object[])ar.AsyncState;

                // Received byte array 
                byte[] buffer = (byte[])obj[0];

                // A Socket to handle remote host communication. 
                handlerM = (Socket)obj[1];

                // Received message 
                string content = string.Empty;

                // The number of bytes received. 
                int bytesRead = handlerM.EndReceive(ar);

                if (bytesRead > 0)
                {
                    content += Encoding.Unicode.GetString(buffer, 0,
                        bytesRead);
                    
                    Parse_Mouse_Event(content);

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
                        obj[1] = handlerM;
                        handlerM.BeginReceive(
                            buffernew, 0, 
                            buffernew.Length,
                            SocketFlags.None,
                            new AsyncCallback(ReceiveCallbackMouse), 
                            obj);
                    }
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
