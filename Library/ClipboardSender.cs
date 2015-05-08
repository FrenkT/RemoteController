using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace Utils.ClipboardSend
{
    class ClipboardSender
    {

        Socket ClipboardSender;

        public ClipboardSender(Socket socket) {
            ClipboardSender = socket;
        }
        
        public void SendClipboard()
        {
            if (ClipboardSender != null)
            {
                if (ClipboardSender.Connected)
                {

                }
            }
        }
 
        public void ReceiveCallbackClipboard(IAsyncResult ar)
        {
        }
    }
}
