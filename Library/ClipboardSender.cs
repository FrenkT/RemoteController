using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Windows;

namespace Utils.ClipboardSend
{
    public class ClipboardSender
    {

        Socket clipboardSocket;

        public ClipboardSender(Socket socket) {
            clipboardSocket = socket;
        }
        
        public void SendClipboard()
        {
            if (clipboardSocket != null)
            {
                if (clipboardSocket.Connected)
                {
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        string data = System.Windows.Clipboard.GetText();
                        byte[] dataToByte = Encoding.Unicode.GetBytes(data);

                        byte[] clipboardTypeToByte = new byte[4];
                        clipboardTypeToByte = Encoding.Unicode.GetBytes("t");
                        int sent = clipboardSocket.Send(clipboardTypeToByte);
                        
                        int dataSize = dataToByte.Length;
                        byte[] dataSizeToByte = new byte[4];
                        dataSizeToByte = BitConverter.GetBytes(dataSize);
                        sent = clipboardSocket.Send(dataSizeToByte);
                        
                        int total = 0;
                        int dataLeft = dataSize;

                        while (total < dataSize)
                        {
                            sent = clipboardSocket.Send(dataToByte, total, dataLeft, SocketFlags.None);
                            total += sent;
                            dataLeft -= sent;
                        }
                        MessageBox.Show("sent everything");
                    }

                }
            }
        }
 
        public void ReceiveClipboard()
        {
            if (clipboardSocket != null)
            {
                if (clipboardSocket.Connected)
                {
                    byte[] clipboardTypeToByte = new byte[4];
                    int bytesReceived = clipboardSocket.Receive(clipboardTypeToByte);
                    String clipboardType = Encoding.Unicode.GetString(clipboardTypeToByte, 0, bytesReceived);

                    if (clipboardType.CompareTo("t") == 0)
                    {
                        byte[] clipboardSizeToByte = new byte[4];
                        bytesReceived = clipboardSocket.Receive(clipboardSizeToByte);
                        int clipboardSize = BitConverter.ToInt32(clipboardSizeToByte, 0);
                        MessageBox.Show("receivd t, size = " + clipboardSize);
                    }

                    ReceiveClipboard();

                }
            }

        }
    }
}
