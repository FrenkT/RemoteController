using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Windows;

namespace Utils.ClipboardSend
{
    class ClipboardSender
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
                        int dataSize = dataToByte.Length;
                        byte[] dataSizeToByte = new byte[4];
                        dataSizeToByte = BitConverter.GetBytes(dataSize);

                        int sent = clipboardSocket.Send(dataSizeToByte);
                        int total = 0;
                        int dataLeft = dataSize;

                        while (total < dataSize)
                        {
                            sent = clipboardSocket.Send(dataToByte, total, dataLeft, SocketFlags.None);
                            total += sent;
                            dataLeft -= sent;
                        }
                    }

                }
            }
        }
 
        public void ReceiveCallbackClipboard(IAsyncResult ar)
        {

        }
    }
}
