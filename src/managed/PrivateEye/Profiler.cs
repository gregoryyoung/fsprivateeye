using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PrivateEye
{
    public static class Profiler
    {
        static void ReadLoop()
        {
            var handle = Native.OpenPipeNonBlocking("/tmp/test");
            //START
            StartProfiling();
            //ENTER
            EnterMatryoshka();

            var lastState = new ParserState() { LineRead = false, Position = 0 };
            while (true)
            {
                var buffer = new byte[4906];
                var read = Native.Read(handle, buffer, 0, buffer.Length);
                Console.WriteLine("read " + read);
                if (read > 0)
                {
                    var parsed = Parser.ReadNextLine(buffer, read, lastState);
                    while (parsed.Item2.LineRead)
                    {
                        Console.WriteLine("line read '{0}'", parsed.Item1);
                        parsed = Parser.ReadNextLine(buffer, read, parsed.Item2);
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
}