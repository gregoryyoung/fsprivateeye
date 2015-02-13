using System;
using System.Threading;

namespace host
{
    class Program
    {

        static void Flush()
        {
            
        }

        static void BA91E1230BF74A17AB35D3879E65D032()
        {
           Console.WriteLine("*******************************************************************************************************************");
           //starts profiling. 
        }

        static void BB8F606F50BD474293A734159ABA1D23() 
        {
            Console.WriteLine("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");
            //leave matryoshka
        }

        static void AC4A98BC81E94DADB71D1FABA30E0703() {
            Console.WriteLine("###################################################################################################################");
            //enter matryoshka
        }

        static void Start()
        {
            
        }

        static void Main(string[] args)
        {
            var t = new Thread(x => {
                    while(true) {
                        var b = new byte[300];
                        Thread.Sleep(1000);
                    }
                     });
            t.Start();
            Console.WriteLine("starting");
                var handle = Native.OpenPipeNonBlocking("/tmp/test");
                Console.WriteLine("isinvalid = " + handle.IsInvalid);
                Console.WriteLine("calling start");
                BA91E1230BF74A17AB35D3879E65D032();
                Console.WriteLine("called start");
                Console.WriteLine("something else");
                Console.WriteLine("*******************************************************************************************************************");
                Console.WriteLine("file open, sending signal to start");
                Console.WriteLine("*******************************************************************************************************************");
                AC4A98BC81E94DADB71D1FABA30E0703();

                var lastState = new ParserState() {LineRead = false, Position=0};
                while(true) {
                    var buffer = new byte[4906];
                    var read = Native.Read(handle, buffer, 0, buffer.Length);
                    Console.WriteLine("read " + read);
                    if (read > 0)
                    {
                        var parsed = Parser.ReadNextLine(buffer, read, lastState);
                        while(parsed.Item2.LineRead) {
                            Console.WriteLine("line read '{0}'", parsed.Item1);
                            lastState = parsed.Item2;
                            parsed = Parser.ReadNextLine(buffer, read, parsed.Item2);
                        }
                        lastState = parsed.Item2;
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
    }
}
