using System.Runtime.InteropServices;

namespace Interceptor
{
    internal static class WinApi
    {
        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int AllocConsole();
    }
}