namespace Interceptor
{
    using System;
    using System.Runtime.InteropServices;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SslReadDelegate(IntPtr ssl, IntPtr buffer, int length);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SslWriteDelegate(IntPtr ssl, IntPtr buffer, int length);
}