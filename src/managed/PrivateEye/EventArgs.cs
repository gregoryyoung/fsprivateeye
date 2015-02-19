using System;

namespace PrivateEye
{
    public class ObjectAllocated
    {
        public readonly MethodDefinition Method;
        public readonly Type Type;
        public readonly uint Size;
    }

    public class MethodCalled
    {
        public readonly MethodDefinition Method;
        public readonly ulong Time;

        public MethodCalled(MethodDefinition method, ulong time)
        {
            Method = method;
            Time = time;
        }
    }

    public class JitOccured
    {
        public readonly MethodDefinition Method;
        public readonly int CodeSize;

        public JitOccured(MethodDefinition method, int codeSize)
        {
            Method = method;
            CodeSize = codeSize;
        }
    }

    public class GCOccured
    {
        public readonly long Time;

        public GCOccured(long time)
        {
            Time = time;
        }
    }
}