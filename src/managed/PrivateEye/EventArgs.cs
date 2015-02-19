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
        public readonly string Name;
        public readonly ulong Time;

        public MethodCalled(MethodDefinition method, ulong time, string name)
        {
            Method = method;
            Time = time;
            Name = name;
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