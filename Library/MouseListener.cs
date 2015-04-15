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
        //private delegate void MouseCallbackAsync(InterceptMouse.KeyEvent keyEvent, int vkCode);
        public event EventHandler MouseAction;

        public MouseListener()
        {
            hookedLowLevelMouseProc = (InterceptMouse.LowLevelMouseProc)LowLevelMouseProc;

            hookId = InterceptMouse.SetHook(hookedLowLevelMouseProc);

            //hookedKeyboardCallbackAsync = new KeyboardCallbackAsync(KeyboardListener_KeyboardCallbackAsync);
            MouseAction = delegate { };
        }

        ~MouseListener()
        {
            Dispose();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (InterceptMouse.MouseMessages.WM_LBUTTONDOWN == (InterceptMouse.MouseMessages)wParam)
                {
                InterceptMouse.MSLLHOOKSTRUCT hookStruct = (InterceptMouse.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(InterceptMouse.MSLLHOOKSTRUCT));
                //Console.WriteLine(hookStruct.pt.x + ", " + hookStruct.pt.y);
                MouseAction(null, new EventArgs());
                }
            }
            return InterceptMouse.CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            InterceptMouse.UnhookWindowsHookEx(hookId);
        }
    }

    internal static class InterceptMouse
    {
        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
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
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
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

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

    }
}
