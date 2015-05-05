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
    // State object for reading client data asynchronously
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    public partial class MainWindow : Window
    {

        Socket sListener, sListenerKb, sListenerM;
        IPEndPoint ipEndPoint, ipEndPointKb, ipEndPointM;
        //Socket handler, handlerKb, handlerM;
        String pass = "";
        String locIp = GetIP4Address();
        int port_conn = 4510;

        public MainWindow()
        {
            InitializeComponent();
            Start_Button.IsEnabled = true;
            StartListen_Button.IsEnabled = false;
            Close_Button.IsEnabled = false;
        }
       
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TextBox_ConfigPassword.Password) && !string.IsNullOrWhiteSpace(TextBox_ConfigPort.Text))
            {
                pass = TextBox_ConfigPassword.Password;
                port_conn = int.Parse(TextBox_ConfigPort.Text);
                InitControl_Socket();
                Create_TCPConnection_Keyboard();
                Create_UDPConnection_Mouse();
                Start_Button.IsEnabled = false;
                StartListen_Button.IsEnabled = true;
            }
            else
            {
                MessageBox.Show("Fill All the fields", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                
                sListener = null;

                permission.Demand();

                IPAddress ipAddr = IPAddress.Parse(locIp);

                ipEndPoint = new IPEndPoint(ipAddr, locPort);

                sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                sListener.Bind(ipEndPoint);
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
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        private void Listen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                sListener.Listen(1);

                AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
                sListener.BeginAccept(aCallback, sListener);
                
                tbConnectionStatus.Text = "Server is now listening on " + ipEndPoint.Address + " port: " + ipEndPoint.Port;
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
            StartListen_Button.IsEnabled = false;
            Close_Button.IsEnabled = true;
        }

        public void StartListenMouse()
        {
            //byte[] buffer = new byte[1024];
            //Socket handler = sListenerM;
            Socket handler = null;
            
            //object[] obj = new object[2];
            //obj[0] = buffer;
            //obj[1] = handler;

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = sListenerM;

            sListenerM.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallbackMouse), state);
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = null;
            Socket handler = null;
            try
            {
                //byte[] buffer = new byte[1024];

                listener = (Socket)ar.AsyncState;
                EndPoint endpoint = listener.LocalEndPoint;
                handler = listener.EndAccept(ar);
                handler.NoDelay = true;

                //object[] obj = new object[2];
                //obj[0] = buffer;
                //obj[1] = handler;

                // Create the state object.
                StateObject state = new StateObject();
                state.workSocket = handler;

                if (endpoint.GetHashCode() == sListener.LocalEndPoint.GetHashCode())
                {
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackCONNECTION), state);
                    //handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallbackCONNECTION), obj);
                }
                if (endpoint.GetHashCode() == sListenerKb.LocalEndPoint.GetHashCode())
                {
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallbackKB), state);
                    //handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallbackKB), obj);
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
                //object[] obj = new object[2];
                //obj = (object[])ar.AsyncState;

                //byte[] buffer = (byte[])obj[0];

                //handler = (Socket)obj[1];

                string content = string.Empty;

                // Retrieve the state object and the handler socket
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    //content += Encoding.Unicode.GetString(buffer, 0, bytesRead);
                    content += Encoding.Unicode.GetString(state.buffer, 0, bytesRead);
                    if (content.IndexOf("<PasswordCheck>") > -1)
                    {
                        string str = content.Substring(0, content.LastIndexOf("<PasswordCheck>"));
                        if (Check_Password(str, handler))
                        {
                            sListenerKb.Listen(1);
                            AsyncCallback aCallback2 = new AsyncCallback(AcceptCallback);
                            sListenerKb.BeginAccept(aCallback2, sListenerKb);
                            StartListenMouse();
                            this.Dispatcher.Invoke((Action)(() =>
                            { tbConnectionStatus.Text = "Connection accepted.";
                              tbKeyboardStatus.Text = "Connection Keyboard accepted.";
                              tbMouseStatus.Text = "Connection Mouse accepted.";
                            }));
                            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallbackCONNECTION), state);
                        }
                    }
                    else
                    {
                        //byte[] buffernew = new byte[1024];
                        //obj[0] = buffernew;
                        //obj[1] = handler;

                        //handler.BeginReceive(buffernew, 0, buffernew.Length, SocketFlags.None, new AsyncCallback(ReceiveCallbackCONNECTION), obj);
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallbackCONNECTION), state);
                    }
                }
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }

        public void ReceiveCallbackKB(IAsyncResult ar)
        {
            try
            {
                //object[] obj = new object[2];
                //obj = (object[])ar.AsyncState;

                //byte[] buffer = (byte[])obj[0];

                //handlerKb = (Socket)obj[1];

                string content = string.Empty;

                // Retrieve the state object and the handler socket
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket handlerKb = state.workSocket;

                int bytesRead = handlerKb.EndReceive(ar);

                if (bytesRead > 0)
                {
                    content += Encoding.Unicode.GetString(state.buffer, 0, bytesRead);

                    Parse_KB_Event(content);

                    //byte[] buffernew = new byte[1024];
                    //obj[0] = buffernew;
                    //obj[1] = handlerKb;

                    //handlerKb.BeginReceive(buffernew, 0, buffernew.Length, SocketFlags.None, new AsyncCallback(ReceiveCallbackKB), obj);
                    handlerKb.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallbackKB), state);
                }
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
      
        public void ReceiveCallbackMouse(IAsyncResult ar)
        {
            try
            {
                //object[] obj = new object[2];
                //obj = (object[])ar.AsyncState;

                //byte[] buffer = (byte[])obj[0];

                //handlerM = (Socket)obj[1];

                string content = string.Empty;

                // Retrieve the state object and the handler socket
                // from the asynchronous state object.
                StateObject state = (StateObject)ar.AsyncState;
                Socket handlerM = state.workSocket;

                int bytesRead = handlerM.EndReceive(ar);

                if (bytesRead > 0)
                {
                    content += Encoding.Unicode.GetString(state.buffer, 0, bytesRead);
                    
                    Parse_Mouse_Event(content);

                    //byte[] buffernew = new byte[1024];
                    //obj[0] = buffernew;
                    //obj[1] = handlerM;
                    //handlerM.BeginReceive(buffernew, 0, buffernew.Length,SocketFlags.None,new AsyncCallback(ReceiveCallbackMouse), obj);
                    handlerM.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(ReceiveCallbackMouse), state);
                }
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
      
        public bool Check_Password(String inputpassword, Socket handler) 
        {
            sListener = handler;
            //Socket handler = null;
            try
            {
                if (pass.CompareTo(inputpassword) == 0)
                {
                    string str = "ok";
                    byte[] byteData = Encoding.Unicode.GetBytes(str);
                    //handler.Send(byteData);
                    handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
                    //sListener.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), sListener);
                    return true;
                }
                else {
                    string str = "quit";
                    byte[] byteData = Encoding.Unicode.GetBytes(str);
                    handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
                }
                //int bytesSent = handler.EndSend(ar);
                //handler.EndSend(handler,);
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
            
            return false;
        }
        
        public void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSend = handler.EndSend(ar);
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
        }
        
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string str = "Disconnect";
                byte[] byteData = Encoding.Unicode.GetBytes(str);
                sListener.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), sListener);
                /*
                this.Dispatcher.Invoke((Action)(() =>
                {
                    sListener.Send(byteData);
                }));
                */
                MessageBox.Show("Inviato");
                sListener.Disconnect(true);
                sListenerKb.Disconnect(true);
                sListenerM.Disconnect(true);
                //sListener.Close();
                //sListenerKb.Close();
                //sListenerM.Close();
            }
            catch (Exception exc) { MessageBox.Show(exc.ToString()); }
            
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
    }
}
