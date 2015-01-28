using System;
using Microsoft.Win32.SafeHandles;

namespace host
{
    public static class Native
    {
        public static SafeFileHandle OpenPipeNonblocking(string filename)
        {
#if __Mono_CS__
            Syscalls.open
#else
            return new SafeFileHandle(new IntPtr(0), false);
#endif
        }
    }
}