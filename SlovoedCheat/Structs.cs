using System;
using System.Runtime.InteropServices;

namespace SlovoedCheat
{
    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        int dx;
        int dy;
        int mouseData;
        public int dwFlags;
        int time;
        IntPtr dwExtraInfo;
    }
    struct INPUT
    {
        public uint dwType;
        public MOUSEINPUT mi;
    }

}
