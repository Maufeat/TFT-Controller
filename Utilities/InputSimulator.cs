using System;
using System.Runtime.InteropServices;

namespace TFTController.Utilities
{
    public static class InputSimulator
    {
        public static uint KEYEVENTF_SCANCODE = 0x0008;
        public static uint KEYEVENTF_KEYUP = 0x0002;
        public static uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public static uint MOUSEEVENTF_LEFTUP = 0x0004;
        public static uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public static uint MOUSEEVENTF_RIGHTUP = 0x0010;
        public static ushort XINPUT_GAMEPAD_DPAD_UP = 0x0001;
        public static ushort XINPUT_GAMEPAD_DPAD_DOWN = 0x0002;
        public static ushort XINPUT_GAMEPAD_DPAD_LEFT = 0x0004;
        public static ushort XINPUT_GAMEPAD_DPAD_RIGHT = 0x0008;
        public static ushort XINPUT_GAMEPAD_A = 0x1000;
        public static ushort XINPUT_GAMEPAD_X = 0x4000; // Xbox X -> D key
        public static ushort XINPUT_GAMEPAD_Y = 0x8000; // Xbox Y -> F key
        public static ushort XINPUT_GAMEPAD_LEFT_SHOULDER = 0x0100; // LB -> E key
        public static int ERROR_SUCCESS = 0;
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            // Mouse and hardware inputs omitted for brevity.
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        /// <summary>
        /// Simulates a key press using scan codes.
        /// </summary>
        public static void SimulateKeyPress(byte vk)
        {
            uint scanCode = MapVirtualKey(vk, 0);
            INPUT[] inputs = new INPUT[2];

            // Key down.
            inputs[0].type = 1;
            inputs[0].U.ki.wVk = 0;
            inputs[0].U.ki.wScan = (ushort)scanCode;
            inputs[0].U.ki.dwFlags = KEYEVENTF_SCANCODE;
            inputs[0].U.ki.time = 0;
            inputs[0].U.ki.dwExtraInfo = IntPtr.Zero;

            // Key up.
            inputs[1].type = 1;
            inputs[1].U.ki.wVk = 0;
            inputs[1].U.ki.wScan = (ushort)scanCode;
            inputs[1].U.ki.dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP;
            inputs[1].U.ki.time = 0;
            inputs[1].U.ki.dwExtraInfo = IntPtr.Zero;

            uint result = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (result == 0)
            {
                int error = Marshal.GetLastWin32Error();
                // Optionally, log the error.
            }
        }

        // Mouse simulation methods. All accept a Point for the current cursor position.
        public static void SimulateMouseDown(Point pos)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)pos.X, (uint)pos.Y, 0, 0);
        }

        public static void SimulateMouseUp(Point pos)
        {
            mouse_event(MOUSEEVENTF_LEFTUP, (uint)pos.X, (uint)pos.Y, 0, 0);
        }

        public static void SimulateMouseClick(Point pos)
        {
            SimulateMouseDown(pos);
            SimulateMouseUp(pos);
        }

        public static void SimulateRightMouseDown(Point pos)
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)pos.X, (uint)pos.Y, 0, 0);
        }

        public static void SimulateRightMouseUp(Point pos)
        {
            mouse_event(MOUSEEVENTF_RIGHTUP, (uint)pos.X, (uint)pos.Y, 0, 0);
        }

        public static void SimulateRightMouseClick(Point pos)
        {
            SimulateRightMouseDown(pos);
            SimulateRightMouseUp(pos);
        }
    }
}
