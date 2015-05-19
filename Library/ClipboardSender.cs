using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;

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
                    }

                    if (System.Windows.Clipboard.ContainsImage())
                    {
                        System.Windows.Media.Imaging.BitmapSource data = System.Windows.Clipboard.GetImage();
                        BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(data));
                        //encoder.QualityLevel = 100;      
                        byte[] bit = new byte[0];
                        using (MemoryStream stream = new MemoryStream())
                        {
                            encoder.Frames.Add(BitmapFrame.Create(data));
                            encoder.Save(stream);
                            bit = stream.ToArray(); 
                            stream.Close();               
                        }
                        byte[] dataToByte = bit;

                        byte[] clipboardTypeToByte = new byte[4];
                        clipboardTypeToByte = Encoding.Unicode.GetBytes("i");
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

                        int total = 0;
                        int recv;
                        int dataleft = clipboardSize;
                        byte[] clipboardContent = new byte[clipboardSize];
                        while (total < clipboardSize)
                        {
                            recv = clipboardSocket.Receive(clipboardContent, total, dataleft, SocketFlags.None);
                            if (recv == 0)
                            {
                                clipboardContent = null;
                                break;
                            }
                            total += recv;
                            dataleft -= recv;
                        }

                        String clipboardContentToString = Encoding.Unicode.GetString(clipboardContent, 0, total);
                        System.Windows.Clipboard.SetText(clipboardContentToString);
                    }

                    if (clipboardType.CompareTo("i") == 0)
                    {
                        byte[] clipboardSizeToByte = new byte[4];
                        bytesReceived = clipboardSocket.Receive(clipboardSizeToByte);
                        int clipboardSize = BitConverter.ToInt32(clipboardSizeToByte, 0);

                        int total = 0;
                        int recv;
                        int dataleft = clipboardSize;
                        byte[] clipboardContent = new byte[clipboardSize];
                        while (total < clipboardSize)
                        {
                            recv = clipboardSocket.Receive(clipboardContent, total, dataleft, SocketFlags.None);
                            if (recv == 0)
                            {
                                clipboardContent = null;
                                break;
                            }
                            total += recv;
                            dataleft -= recv;
                        }

                        var stream = new MemoryStream(clipboardContent);
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = stream;
                        image.EndInit();


                        System.Windows.Clipboard.SetImage(image);
                    }

                    ReceiveClipboard();  //TODO

                }
            }

        }
    }
}
