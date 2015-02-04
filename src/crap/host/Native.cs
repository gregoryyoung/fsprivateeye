using System;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

#if __MonoCS__ 
using Mono.Unix.Native;
using Mono.Unix;
#endif

namespace host
{
    public static unsafe class Native
    {
#if __MonoCS__
        private static readonly int EAGAIN = NativeConvert.FromErrno(Errno.EAGAIN);
#endif
        public static SafeFileHandle OpenPipeNonBlocking(string filename)
        {
#if __MonoCS__
            var flags = OpenFlags.O_RDONLY | OpenFlags.O_NONBLOCK;
            var han = Syscall.open(filename, flags, FilePermissions.S_IRWXU);
            if(han < 0) {
                Console.WriteLine("handle is " + han);
                 throw new Win32Exception();
            }

            var handle = new SafeFileHandle((IntPtr) han, true);
            if(handle.IsInvalid) throw new Exception("Invalid handle");
            return handle;
#else
            return new SafeFileHandle(new IntPtr(0), false);
#endif
        }

        public static int Read(SafeFileHandle handle, byte[] buffer, int offset, int count)
        {
#if __MonoCS__
            int r;
            fixed(byte *p = buffer) {
            do {
                r = (int) Syscall.read (handle.DangerousGetHandle().ToInt32(), p, (ulong) count);
            } while (UnixMarshal.ShouldRetrySyscall ((int) r));
            if(r == -1) {
                int errno = Marshal.GetLastWin32Error();
                if (errno == EAGAIN) {
                    return 0;
                }
                throw new Win32Exception();
            }
            return r;
#else
            return 0;
#endif
            
        }
    }
}
