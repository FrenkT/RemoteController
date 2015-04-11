using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace Utils.KeyboardSender
{
    public class KeyboardSender
    {
        [DllImport("user32.dll")]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);    //uint vs UInt32

        [DllImport("kernel32.dll")]
        public static extern int GetTickCount();

        [DllImport("user32.dll")]
        public static extern IntPtr GetMessageExtraInfo();

        public struct INPUT
        {
            public int type;    //int vs UInt32
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public Int32 dx;
            public Int32 dy;
            public Int32 mouseData;
            public Int32 dwFlags;
            public Int32 time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            /*Virtual Key code.  Must be from 1-254.  If the dwFlags member specifies KEYEVENTF_UNICODE, wVk must be 0.*/
            public ushort wVk;   //Int16 vs ushort
            /*A hardware scan code for the key. If dwFlags specifies KEYEVENTF_UNICODE, wScan specifies a Unicode character
             * which is to be sent to the foreground application.*/
            public ushort wScan;   //Int16 vs ushort
            /*Specifies various aspects of a keystroke.  See the KEYEVENTF_ constants for more information.*/
            public uint dwFlags;   //Int32 vs uint
            /*The time stamp for the event, in milliseconds. If this parameter is zero, the system will provide its own time stamp.*/
            public uint time;    //Int32 vs uint
            /*An additional value associated with the keystroke. Use the GetMessageExtraInfo function to obtain this information.*/
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public Int32 uMsg;
            public Int16 wParamL;
            public Int16 wParamH;
        }

        public const int INPUT_MOUSE = 0;
        public const int INPUT_KEYBOARD = 1;
        public const int INPUT_HARDWARE = 2;
        public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const uint KEYEVENTF_UNICODE = 0x0004;
        public const uint KEYEVENTF_SCANCODE = 0x0008;
        public const uint XBUTTON1 = 0x0001;
        public const uint XBUTTON2 = 0x0002;
        public const uint MOUSEEVENTF_MOVE = 0x0001;
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        public const uint MOUSEEVENTF_XDOWN = 0x0080;
        public const uint MOUSEEVENTF_XUP = 0x0100;
        public const uint MOUSEEVENTF_WHEEL = 0x0800;
        public const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
        public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        
        
        /// <summary>
        /// Synthesizes keystrokes corresponding to the specified Unicode string,
        /// sending them to the currently active window.
        /// <param name="s">The string to send.</param>
        /*public static void SendString(char c)
        {
            // Construct list of inputs in order to send them through a single SendInput call at the end.
            List<INPUT> inputs = new List<INPUT>();
      
                // First send a key down, then a key up.
                foreach (bool keyUp in new bool[] { false, true })
                {
                    // INPUT is a multi-purpose structure which can be used 
                    // for synthesizing keystrokes, mouse motions, and button clicks.
                    INPUT input = new INPUT
                    {
                        // Need a keyboard event.
                        type = INPUT_KEYBOARD,
                        u = new InputUnion
                        {
                            // KEYBDINPUT will contain all the information for a single keyboard event
                            // (more precisely, for a single key-down or key-up).
                            ki = new KEYBDINPUT
                            {
                                // Virtual-key code must be 0 since we are sending Unicode characters.
                                wVk = 0,

                                // The Unicode character to be sent.
                                wScan = 0,

                                // Indicate that we are sending a Unicode character.
                                // Also indicate key-up on the second iteration.
                                dwFlags = 0,//KEYEVENTF_UNICODE | (keyUp ? KEYEVENTF_KEYUP : 0),

                                dwExtraInfo = GetMessageExtraInfo(),
                            }
                        }
                    };

                    // Add to the list (to be sent later).
                    inputs.Add(input);
                }
            

            // Send all inputs together using a Windows API call.
            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
        }*/

        public static void SendKeyPress(Key keyCode)
        {
            INPUT input = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0x44,
                        wScan = 0,
                        dwFlags = 0,
                        dwExtraInfo = IntPtr.Zero,
                    }
                }
            };

            INPUT input2 = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0x44,
                        wScan = 0,
                        dwFlags = 2,
                        dwExtraInfo = IntPtr.Zero,
                    }
                }
            };

            INPUT[] inputs = new INPUT[] { input, input2 };
            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    
    }

    

}
