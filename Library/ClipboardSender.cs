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
using System.IO.Compression;

namespace Utils.ClipboardSend
{
    public class ClipboardSender
    {

        Socket clipboardSocket;
        public bool justReceivedContent = false;

        public ClipboardSender(Socket socket) {
            clipboardSocket = socket;
        }

        public void SendClipboard()
        {
            try
            {
                if (clipboardSocket != null)
                {
                    if (clipboardSocket.Connected)
                    {
                        if (System.Windows.Clipboard.ContainsText())
                        {
                            string data = System.Windows.Clipboard.GetText();
                            byte[] dataToByte = Encoding.Unicode.GetBytes(data);

                            byte[] clipboardTypeToByte = new byte[2];
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
                            byte[] clipboardTypeToByte = new byte[2];
                            clipboardTypeToByte = Encoding.Unicode.GetBytes("d");
                            int sent = clipboardSocket.Send(clipboardTypeToByte);

                            System.Collections.Specialized.StringCollection dropList = System.Windows.Clipboard.GetFileDropList();

                            int dropListSize = dropList.Count;
                            byte[] dropListSizeToByte = new byte[4];
                            dropListSizeToByte = BitConverter.GetBytes(dropListSize);
                            sent = clipboardSocket.Send(dropListSizeToByte);


                            foreach (string path in dropList)
                            {
                                FileAttributes elementType = File.GetAttributes(path);
                                if (elementType.HasFlag(FileAttributes.Directory))
                                {
                                    //Transfer a zipped directory
                                    byte[] elementTypeToByte = new byte[2];
                                    elementTypeToByte = Encoding.Unicode.GetBytes("z");
                                    sent = clipboardSocket.Send(elementTypeToByte);

                                    if (!File.Exists(path + ".zip"))
                                    {
                                        ZipFile.CreateFromDirectory(path, path + ".zip");
                                    }
                                    string zipPath = path + ".zip";

                                    FileInfo fileInfo = new FileInfo(zipPath);
                                    string fileName = fileInfo.Name;
                                    byte[] fileContentToByte = File.ReadAllBytes(zipPath);
                                    int fileSize = fileContentToByte.Length;

                                    byte[] fileNameToByte = Encoding.Unicode.GetBytes(fileName);
                                    int fileNameSize = fileNameToByte.Length;
                                    byte[] fileNameSizeToByte = new byte[4];
                                    fileNameSizeToByte = BitConverter.GetBytes(fileNameSize);
                                    sent = clipboardSocket.Send(fileNameSizeToByte);

                                    int total = 0;
                                    int dataLeft = fileNameSize;
                                    while (total < fileNameSize)
                                    {
                                        sent = clipboardSocket.Send(fileNameToByte, total, dataLeft, SocketFlags.None);
                                        total += sent;
                                        dataLeft -= sent;
                                    }

                                    byte[] fileSizeToByte = new byte[4];
                                    fileSizeToByte = BitConverter.GetBytes(fileSize);
                                    sent = clipboardSocket.Send(fileSizeToByte);

                                    total = 0;
                                    dataLeft = fileSize;
                                    while (total < fileSize)
                                    {
                                        sent = clipboardSocket.Send(fileContentToByte, total, dataLeft, SocketFlags.None);
                                        total += sent;
                                        dataLeft -= sent;
                                    }
                                    File.Delete(zipPath);
                                }
                                else
                                {
                                    //Transfer a file
                                    byte[] elementTypeToByte = new byte[2];
                                    elementTypeToByte = Encoding.Unicode.GetBytes("f");
                                    sent = clipboardSocket.Send(elementTypeToByte);

                                    FileInfo fileInfo = new FileInfo(path);
                                    string fileName = fileInfo.Name;
                                    byte[] fileContentToByte = File.ReadAllBytes(path);
                                    int fileSize = fileContentToByte.Length;

                                    byte[] fileNameToByte = Encoding.Unicode.GetBytes(fileName);
                                    int fileNameSize = fileNameToByte.Length;
                                    byte[] fileNameSizeToByte = new byte[4];
                                    fileNameSizeToByte = BitConverter.GetBytes(fileNameSize);
                                    sent = clipboardSocket.Send(fileNameSizeToByte);

                                    int total = 0;
                                    int dataLeft = fileNameSize;
                                    while (total < fileNameSize)
                                    {
                                        sent = clipboardSocket.Send(fileNameToByte, total, dataLeft, SocketFlags.None);
                                        total += sent;
                                        dataLeft -= sent;
                                    }

                                    byte[] fileSizeToByte = new byte[4];
                                    fileSizeToByte = BitConverter.GetBytes(fileSize);
                                    sent = clipboardSocket.Send(fileSizeToByte);

                                    total = 0;
                                    dataLeft = fileSize;
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
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
        }
 
        public void ReceiveClipboard()
        {
            try
            {
                if (clipboardSocket != null)
                {
                    if (clipboardSocket.Connected)
                    {
                        byte[] clipboardTypeToByte = new byte[2];
                        int bytesReceived = clipboardSocket.Receive(clipboardTypeToByte, 2, SocketFlags.None);
                        String clipboardType = Encoding.Unicode.GetString(clipboardTypeToByte, 0, bytesReceived);

                        if (clipboardType.CompareTo("t") == 0)
                        {
                            byte[] clipboardSizeToByte = new byte[4];
                            bytesReceived = clipboardSocket.Receive(clipboardSizeToByte, 4, SocketFlags.None);
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

                            justReceivedContent = true;
                            String clipboardContentToString = Encoding.Unicode.GetString(clipboardContent, 0, total);
                            System.Windows.Clipboard.SetText(clipboardContentToString);
                        }

                        if (clipboardType.CompareTo("i") == 0)
                        {
                            byte[] clipboardSizeToByte = new byte[4];
                            bytesReceived = clipboardSocket.Receive(clipboardSizeToByte, 4, SocketFlags.None);
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

                            justReceivedContent = true;
                            System.Windows.Clipboard.SetImage(image);
                        }

                        if (clipboardType.CompareTo("a") == 0)
                        {
                            byte[] clipboardSizeToByte = new byte[4];
                            bytesReceived = clipboardSocket.Receive(clipboardSizeToByte, 4, SocketFlags.None);
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
                            justReceivedContent = true;
                            System.Windows.Clipboard.SetAudio(clipboardContent);
                        }

                        if (clipboardType.CompareTo("d") == 0)
                        {
                            byte[] dropListSizeToByte = new byte[4];
                            bytesReceived = clipboardSocket.Receive(dropListSizeToByte, 4, SocketFlags.None);
                            int dropListSize = BitConverter.ToInt32(dropListSizeToByte, 0);

                            System.Collections.Specialized.StringCollection dropList = new System.Collections.Specialized.StringCollection();

                            for (int i = 0; i < dropListSize; i++)
                            {
                                byte[] elementTypeToByte = new byte[2];
                                bytesReceived = clipboardSocket.Receive(elementTypeToByte, 2, SocketFlags.None);
                                String elementType = Encoding.Unicode.GetString(elementTypeToByte, 0, bytesReceived);

                                if (elementType.CompareTo("f") == 0)
                                {
                                    //Receive a file
                                    byte[] fileNameSizeToByte = new byte[4];
                                    bytesReceived = clipboardSocket.Receive(fileNameSizeToByte, 4, SocketFlags.None);
                                    int fileNameSize = BitConverter.ToInt32(fileNameSizeToByte, 0);

                                    int total = 0;
                                    int recv;
                                    int dataleft = fileNameSize;
                                    byte[] fileName = new byte[fileNameSize];
                                    while (total < fileNameSize)
                                    {
                                        recv = clipboardSocket.Receive(fileName, total, dataleft, SocketFlags.None);
                                        if (recv == 0)
                                        {
                                            fileName = null;
                                            break;
                                        }
                                        total += recv;
                                        dataleft -= recv;
                                    }
                                    string fileNameToString = Encoding.Unicode.GetString(fileName, 0, total);

                                    byte[] fileSizeToByte = new byte[4];
                                    bytesReceived = clipboardSocket.Receive(fileSizeToByte, 4, SocketFlags.None);
                                    int fileSize = BitConverter.ToInt32(fileSizeToByte, 0);

                                    total = 0;
                                    dataleft = fileSize;
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
                                    string path = Path.GetTempPath() + fileNameToString;
                                    File.WriteAllBytes(path, fileContent);
                                    dropList.Add(path);
                                }

                                if (elementType.CompareTo("z") == 0)
                                {
                                    //Receive a zipped directory
                                    byte[] fileNameSizeToByte = new byte[4];
                                    bytesReceived = clipboardSocket.Receive(fileNameSizeToByte, 4, SocketFlags.None);
                                    int fileNameSize = BitConverter.ToInt32(fileNameSizeToByte, 0);

                                    int total = 0;
                                    int recv;
                                    int dataleft = fileNameSize;
                                    byte[] fileName = new byte[fileNameSize];
                                    while (total < fileNameSize)
                                    {
                                        recv = clipboardSocket.Receive(fileName, total, dataleft, SocketFlags.None);
                                        if (recv == 0)
                                        {
                                            fileName = null;
                                            break;
                                        }
                                        total += recv;
                                        dataleft -= recv;
                                    }
                                    string fileNameToString = Encoding.Unicode.GetString(fileName, 0, total);

                                    byte[] fileSizeToByte = new byte[4];
                                    bytesReceived = clipboardSocket.Receive(fileSizeToByte, 4, SocketFlags.None);
                                    int fileSize = BitConverter.ToInt32(fileSizeToByte, 0);

                                    total = 0;
                                    dataleft = fileSize;
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
                                    string path = Path.GetTempPath() + fileNameToString;
                                    File.WriteAllBytes(path, fileContent);
                                    string folderPath = Path.GetTempPath() + Path.GetFileNameWithoutExtension(path);

                                    if (Directory.Exists(folderPath))
                                    {
                                        Directory.Delete(folderPath, true);
                                    }
                                    ZipFile.ExtractToDirectory(path, folderPath);
                                    File.Delete(path);

                                    dropList.Add(folderPath);
                                }
                            }
                            justReceivedContent = true;
                            System.Windows.Clipboard.SetFileDropList(dropList);
                        }
                    }
                }
            }
            catch (SocketException) {}
            catch (ObjectDisposedException) {}
        }
    }
}
