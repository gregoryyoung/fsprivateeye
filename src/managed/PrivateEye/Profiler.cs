using System;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<ulong, ClassDefinition> _classDefinitions = new ConcurrentDictionary<ulong, ClassDefinition>();
        private static readonly ConcurrentDictionary<ulong, MethodDefinition> _methodDefinitions = new ConcurrentDictionary<ulong, MethodDefinition>(); 


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
                if (read > 0)
                {
                    var parsed = StringParsing.ReadNextLine(buffer, read, lastState);
                    while (parsed.Item2.LineRead)
                    {
                        parsed = StringParsing.ReadNextLine(buffer, read, parsed.Item2);
                        ProcessLine(parsed.Item1);
                    }
                    lastState = parsed.Item2;
                }
                else
                {
                    Thread.Sleep(1);
                }
            }

        }

        private static void ProcessLine(string line)
        {
            if (line.Length == 0) return;
            switch (line[0])
            {
                case 'A':
                    //allocation
                    break;
                case 'E':
                    var enter = ReadEvent(line);
                    MethodDefinition methoddef;
                    _methodDefinitions.TryGetValue(enter.Identifier, out methoddef);
                    if(methoddef != null)
                        OnOnMethodCalled(new MethodCalled(null, enter.Time, methoddef.Name));
                    //enter
                    break;
                case 'L':
                    //leave
                    break;
                case 'T':
                    //type definition                    
                    var data = ReadDefinition(line);
                    _classDefinitions.TryAdd(data.Identifier,
                        new ClassDefinition
                        {
                            ModuleId = 1,
                            Name = data.Name,
                            Token = 0,
                            Type = Type.GetType(data.Name)
                        });
                    break;
                case 'M':
                    //method definition
                    //TODO hook method definition
                    var mdata = ReadDefinition(line);
                    _methodDefinitions.TryAdd(mdata.Identifier,
                        new MethodDefinition
                        {
                            ModuleId = 1,
                            Name = mdata.Name,
                            Token = 0,
                            MethodInfo = null
                        });

                    break;

            }
        }

        private static DefinitionData ReadDefinition(string line)
        {
            var res = StringParsing.ReadULong(line, 2);
            var res2 = StringParsing.ReadString(line, res.Item2);
            return new DefinitionData() {Identifier = res.Item1, Name = res2.Item1};
        }

        private static EventData ReadEvent(string line)
        {
            var res = StringParsing.ReadULong(line, 2);
            var res2 = StringParsing.ReadULong(line, res.Item2);
            var res3 = StringParsing.ReadULong(line, res2.Item2);
            return new EventData() {Identifier = res3.Item1, Thread= (uint)res2.Item1, Time = res.Item1};
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

    struct DefinitionData
    {
        public string Name;
        public ulong Identifier;
    }

    struct EventData
    {
        public ulong Time;
        public UInt32 Thread;
        public ulong Identifier;
    }

    public delegate void AllocatedDelegate(ObjectAllocated data);
    public delegate void MethodCalledDelegate(MethodCalled data);
    public delegate void GCOccuredDelegate(GCOccured data);
    public delegate void JitOccurredDelegate(JitOccured data);
}
