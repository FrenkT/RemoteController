using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.IO;

namespace Utils.Clipboard
{
    public class ClipboardListener : IDisposable
    {
        public event RawClipboardEventHandler ClipboardChange;
        private IntPtr hWndNextViewer;
        private HwndSource hWndSource;
        private delegate void ClipboardCallbackAsync(bool changed);
        private ClipboardCallbackAsync hookedClipboardCallbackAsync;
        private InterceptClipboard.LowLevelClipboardProc hookedLowLevelClipboardProc;

        public ClipboardListener(System.Windows.Window w)
        {
            hookedLowLevelClipboardProc = (InterceptClipboard.LowLevelClipboardProc)LowLevelClipboardProc;

            WindowInteropHelper wih = new WindowInteropHelper(w);
            hWndSource = HwndSource.FromHwnd(wih.Handle);
            hWndSource.AddHook(LowLevelClipboardProc);   // start processing window messages
            hookedClipboardCallbackAsync = new ClipboardCallbackAsync(ClipboardListener_ClipboardCallbackAsync);
            hWndNextViewer = InterceptClipboard.SetClipboardViewer(hWndSource.Handle);   // set this window as a viewer
        }

        ~ClipboardListener()
        {
            Dispose();
        }

        public void Dispose()
        {
            // remove this window from the clipboard viewer chain
            InterceptClipboard.ChangeClipboardChain(hWndSource.Handle, hWndNextViewer);
            hWndNextViewer = IntPtr.Zero;
            hWndSource.RemoveHook(this.LowLevelClipboardProc);
            //pnlContent.Children.Clear();
        }

        private IntPtr LowLevelClipboardProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case (int)InterceptClipboard.ClipboardEvent.WM_CHANGECBCHAIN:
                    if (wParam == hWndNextViewer)
                    {
                        // clipboard viewer chain changed, need to fix it.
                        hWndNextViewer = lParam;
                    }
                    else if (hWndNextViewer != IntPtr.Zero)
                    {
                        // pass the message to the next viewer.
                        InterceptClipboard.SendMessage(hWndNextViewer, msg, wParam, lParam);
                    }
                    break;

                case (int)InterceptClipboard.ClipboardEvent.WM_DRAWCLIPBOARD:
                    // clipboard content changed
                    hookedClipboardCallbackAsync.BeginInvoke(true, null, null);
                    // pass the message to the next viewer.
                    InterceptClipboard.SendMessage(hWndNextViewer, msg, wParam, lParam);
                    break;
            }
            return IntPtr.Zero;
        }

        void ClipboardListener_ClipboardCallbackAsync(bool changed)
        {
            ClipboardChange(this, new RawClipboardEventArgs());
        }
    }

    public class RawClipboardEventArgs : EventArgs
    {
        public bool changed;

        public RawClipboardEventArgs()
        {
            this.changed = true;
        }
    }

    public delegate void RawClipboardEventHandler(object sender, RawClipboardEventArgs args);

    internal static class InterceptClipboard
    {
        public delegate IntPtr LowLevelClipboardProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);

        public enum ClipboardEvent : int
        {
            WM_DRAWCLIPBOARD = 0x0308,
            WM_CHANGECBCHAIN = 0x030D,
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    }
}
