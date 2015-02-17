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
        public readonly long Time;
        public readonly int AllocationCount;
        public readonly int AllocationSize;

        public MethodCalled(MethodDefinition method, long time, int allocationCount, int allocationSize)
        {
            Method = method;
            Time = time;
            AllocationCount = allocationCount;
            AllocationSize = allocationSize;
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