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
using System.Collections;

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

                    if (System.Windows.Clipboard.ContainsAudio())
                    {
                        Stream data = System.Windows.Clipboard.GetAudioStream();
                        byte[] dataToByte;
                        using (var memoryStream = new MemoryStream())
                        {
                            data.CopyTo(memoryStream);
                            dataToByte = memoryStream.ToArray();
                        }

                        byte[] clipboardTypeToByte = new byte[4];
                        clipboardTypeToByte = Encoding.Unicode.GetBytes("a");
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

                    if (System.Windows.Clipboard.ContainsFileDropList())
                    {
                        byte[] clipboardTypeToByte = new byte[4];
                        clipboardTypeToByte = Encoding.Unicode.GetBytes("d");
                        int sent = clipboardSocket.Send(clipboardTypeToByte);

                        System.Collections.Specialized.StringCollection dropList = System.Windows.Clipboard.GetFileDropList();
                        foreach (string path in dropList)
                        {
                            FileInfo fileInfo = new FileInfo(path);
                            string fileName = fileInfo.Name;
                            byte[] fileContentToByte = File.ReadAllBytes(path);
                            int fileSize = fileContentToByte.Length;

                            byte[] fileNameToByte = new byte[1024];
                            fileNameToByte = Encoding.Unicode.GetBytes(fileName);
                            sent = clipboardSocket.Send(fileNameToByte);

                            byte[] fileSizeToByte = new byte[4];
                            fileSizeToByte = BitConverter.GetBytes(fileSize);
                            sent = clipboardSocket.Send(fileSizeToByte);

                            int total = 0;
                            int dataLeft = fileSize;
                            while (total < fileSize)
                            {
                                sent = clipboardSocket.Send(fileContentToByte, total, dataLeft, SocketFlags.None);
                                total += sent;
                                dataLeft -= sent;
                            }
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

                    if (clipboardType.CompareTo("a") == 0)
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
                        System.Windows.Clipboard.SetAudio(clipboardContent);
                    }

                    if (clipboardType.CompareTo("d") == 0)
                    {
                        byte[] fileNameToByte = new byte[1024];
                        bytesReceived = clipboardSocket.Receive(fileNameToByte);
                        string fileName = Encoding.Unicode.GetString(fileNameToByte, 0, 1024);

                        byte[] fileSizeToByte = new byte[4];
                        bytesReceived = clipboardSocket.Receive(fileSizeToByte);
                        int fileSize = BitConverter.ToInt32(fileSizeToByte, 0);

                        int total = 0;
                        int recv;
                        int dataleft = fileSize;
                        byte[] fileContent = new byte[fileSize];
                        while (total < fileSize)
                        {
                            recv = clipboardSocket.Receive(fileContent, total, dataleft, SocketFlags.None);
                            if (recv == 0)
                            {
                                fileContent = null;
                                break;
                            }
                            total += recv;
                            dataleft -= recv;
                        }
                        File.WriteAllBytes("C:/tmp/" + fileName, fileContent);
                    }

                    ReceiveClipboard();  //TODO

                }
            }

        }
    }
}
