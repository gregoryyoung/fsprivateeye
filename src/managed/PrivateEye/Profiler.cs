using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace PrivateEye
{
    public static class Profiler
    {
        public static IObservable<JitOccured> JitOccurred_;


        public static event JitOccurredDelegate JitOccurred;
        public static event GCOccuredDelegate GCOccured;

        public static event MethodCalledDelegate OnMethodCalled;
        public static event AllocatedDelegate OnAllocation;

        private static Thread _thread;

        private static void OnJitOccurred(JitOccured data)
        {
            var handler = JitOccurred;
            if (handler != null) handler(data);
        }


        private static void OnGcOccured(GCOccured data)
        {
            var handler = GCOccured;
            if (handler != null) handler(data);
        }

        private static void OnOnMethodCalled(MethodCalled data)
        {
            var handler = OnMethodCalled;
            if (handler != null) handler(data);
        }

        private static void OnOnAllocation(ObjectAllocated data)
        {
            var handler = OnAllocation;
            if (handler != null) handler(data);
        }


        public static void Start()
        {
            var handle = Native.OpenPipeNonBlocking("/tmp/test");
            EnableProfiler();
            _thread = new Thread(x => ReadLoop(handle));
            _thread.Start();
        }

        public static void EnableProfiler()
        {
            StartProfiling();
            EnterMatryoshka();
        }

        public static void Summary()
        {
            Console.WriteLine("Summary");
        }

        static void ReadLoop(SafeFileHandle handle)
        {
            var lastState = new ParserState { LineRead = false, Position = 0 };
            var buffer = new byte[4906];
            while (true)
            {
                var read = Native.Read(handle, buffer, 0, buffer.Length);
                Console.WriteLine("read " + read);
                if (read > 0)
                {
                    var parsed = Parser.ReadNextLine(buffer, read, lastState);
                    while (parsed.Item2.LineRead)
                    {
                        Console.WriteLine("line read '{0}'", parsed.Item1);
                        parsed = Parser.ReadNextLine(buffer, read, parsed.Item2);
                        //process.
                    }
                    lastState = parsed.Item2;
                }
                else
                {
                    Thread.Sleep(1);
                }
            }

        }
        //command helpers
        static void StartProfiling() { BA91E1230BF74A17AB35D3879E65D032();}
        static void LeaveMatryoshka() { BB8F606F50BD474293A734159ABA1D23();}
        static void EnterMatryoshka() { AC4A98BC81E94DADB71D1FABA30E0703();}
        static void ForceFlush() { DE259A95ABCA4803A7731665B38DB33A(); }

        #region profiler command methods
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void BA91E1230BF74A17AB35D3879E65D032()
        {
            //starts profiling. 
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void BB8F606F50BD474293A734159ABA1D23()
        {
            //leave matryoshka
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void AC4A98BC81E94DADB71D1FABA30E0703()
        {
            //enter matryoshka
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void DE259A95ABCA4803A7731665B38DB33A()
        {
            //force a flush
        }

        #endregion
    }

    public delegate void AllocatedDelegate(ObjectAllocated data);
    public delegate void MethodCalledDelegate(MethodCalled data);
    public delegate void GCOccuredDelegate(GCOccured data);
    public delegate void JitOccurredDelegate(JitOccured data);
}