using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Utils.Mouse
{
    public class MouseListener : IDisposable
    {   
        private IntPtr hookId = IntPtr.Zero;
        private InterceptMouse.LowLevelMouseProc hookedLowLevelMouseProc;
        private delegate void MouseCallbackAsync(InterceptMouse.MouseMessages mouseMessage, InterceptMouse.MSLLHOOKSTRUCT mouseStruct);
        private MouseCallbackAsync hookedMouseCallbackAsync;
        public event RawMouseEventHandler LeftDown;
        public event RawMouseEventHandler LeftUp;
        public event RawMouseEventHandler RightDown;
        public event RawMouseEventHandler RightUp;
        public event RawMouseEventHandler MouseMove;
        public event RawMouseEventHandler MouseWheel;

        public MouseListener()
        {
            hookedLowLevelMouseProc = (InterceptMouse.LowLevelMouseProc)LowLevelMouseProc;

            hookId = InterceptMouse.SetHook(hookedLowLevelMouseProc);

            hookedMouseCallbackAsync = new MouseCallbackAsync(MouseListener_MouseCallbackAsync);
        }

        ~MouseListener()
        {
            Dispose();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IntPtr LowLevelMouseProc(int nCode, UIntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam.ToUInt32() == (int)InterceptMouse.MouseMessages.WM_LBUTTONDOWN ||
                    wParam.ToUInt32() == (int)InterceptMouse.MouseMessages.WM_LBUTTONUP ||
                    wParam.ToUInt32() == (int)InterceptMouse.MouseMessages.WM_MOUSEMOVE ||
                    wParam.ToUInt32() == (int)InterceptMouse.MouseMessages.WM_MOUSEWHEEL ||
                    wParam.ToUInt32() == (int)InterceptMouse.MouseMessages.WM_RBUTTONDOWN ||
                    wParam.ToUInt32() == (int)InterceptMouse.MouseMessages.WM_RBUTTONUP)
                {
                    hookedMouseCallbackAsync.BeginInvoke((InterceptMouse.MouseMessages)wParam.ToUInt32(), (InterceptMouse.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(InterceptMouse.MSLLHOOKSTRUCT)), null, null);
                }
            }
            if (wParam.ToUInt32() == (int)InterceptMouse.MouseMessages.WM_LBUTTONDOWN ||
                wParam.ToUInt32() == (int)InterceptMouse.MouseMessages.WM_LBUTTONUP ||
                wParam.ToUInt32() == (int)InterceptMouse.MouseMessages.WM_MOUSEWHEEL ||
                wParam.ToUInt32() == (int)InterceptMouse.MouseMessages.WM_RBUTTONDOWN ||
                wParam.ToUInt32() == (int)InterceptMouse.MouseMessages.WM_RBUTTONUP)
            {
                return new IntPtr(1);
            }
            return InterceptMouse.CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            InterceptMouse.UnhookWindowsHookEx(hookId);
        }

        void MouseListener_MouseCallbackAsync(InterceptMouse.MouseMessages mouseMessage, InterceptMouse.MSLLHOOKSTRUCT mouseStruct)
        {
            switch (mouseMessage)
            {
                case InterceptMouse.MouseMessages.WM_LBUTTONDOWN:
                    if (LeftDown != null)
                        LeftDown(this, new RawMouseEventArgs(mouseStruct.pt.x, mouseStruct.pt.y, mouseStruct.mouseData, mouseStruct.flags));
                    break;
                case InterceptMouse.MouseMessages.WM_LBUTTONUP:
                    if (LeftUp != null)
                        LeftUp(this, new RawMouseEventArgs(mouseStruct.pt.x, mouseStruct.pt.y, mouseStruct.mouseData, mouseStruct.flags));
                    break;
                case InterceptMouse.MouseMessages.WM_RBUTTONDOWN:
                    if (RightDown != null)
                        RightDown(this, new RawMouseEventArgs(mouseStruct.pt.x, mouseStruct.pt.y, mouseStruct.mouseData, mouseStruct.flags));
                    break;
                case InterceptMouse.MouseMessages.WM_RBUTTONUP:
                    if (RightUp != null)
                        RightUp(this, new RawMouseEventArgs(mouseStruct.pt.x, mouseStruct.pt.y, mouseStruct.mouseData, mouseStruct.flags));
                    break;
                case InterceptMouse.MouseMessages.WM_MOUSEMOVE:
                    if (MouseMove != null)
                        MouseMove(this, new RawMouseEventArgs(mouseStruct.pt.x, mouseStruct.pt.y, mouseStruct.mouseData, mouseStruct.flags));
                    break;
                case InterceptMouse.MouseMessages.WM_MOUSEWHEEL:
                    if (MouseWheel != null)
                        MouseWheel(this, new RawMouseEventArgs(mouseStruct.pt.x, mouseStruct.pt.y, mouseStruct.mouseData, mouseStruct.flags));
                    break;
                default:
                    break;
            }
        }
    }

    public class RawMouseEventArgs : EventArgs
    {
        public int x;
        public int y;
        public int data;
        public uint flags;


        public RawMouseEventArgs(int x, int y, int data, uint flags)
        {
            this.x = InterceptMouse.CalculateAbsoluteCoordinateX(x);
            this.y = InterceptMouse.CalculateAbsoluteCoordinateY(y);
            this.data = data;
            this.flags = flags;
        }
    }

    public delegate void RawMouseEventHandler(object sender, RawMouseEventArgs args);

    internal static class InterceptMouse
    {
        public delegate IntPtr LowLevelMouseProc(int nCode, UIntPtr wParam, IntPtr lParam);
        public const int WH_MOUSE_LL = 14;

        public enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        enum SystemMetric
        {
            SM_CXSCREEN = 0,
            SM_CYSCREEN = 1,
        }

        public static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public static int CalculateAbsoluteCoordinateX(int x)
        {
            return (x * 65536) / GetSystemMetrics(SystemMetric.SM_CXSCREEN);
        }

        public static int CalculateAbsoluteCoordinateY(int y)
        {
            return (y * 65536) / GetSystemMetrics(SystemMetric.SM_CYSCREEN);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, UIntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(SystemMetric smIndex);

    }
}
