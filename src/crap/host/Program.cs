using System;
using System.IO;
using System.Threading;
using System.Text;
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
            try {
                var bullshit = Native.Read(null, new byte[0],0 ,0);
            }
            catch(Exception ex) {
            }
            Console.WriteLine("starting");
                var handle = Native.OpenPipeNonBlocking("/tmp/test");
                Console.WriteLine("isinvalid = " + handle.IsInvalid);
                Console.WriteLine("calling start");
                BA91E1230BF74A17AB35D3879E65D032();
                Console.WriteLine("called start");
                Console.WriteLine("something else");
                AC4A98BC81E94DADB71D1FABA30E0703();
                Console.WriteLine("*******************************************************************************************************************");
                Console.WriteLine("file open, sending signal to start");
                Console.WriteLine("*******************************************************************************************************************");
                while(true) {
                    var buffer = new byte[4906];
                    int read = Native.Read(handle, buffer, 0, buffer.Length);
                    Console.WriteLine("*********************** READ " + read);
                    if (read > 0)
                    {
                        Console.WriteLine(Encoding.ASCII.GetString(buffer, 0, read));
                    }
                    else
                    {
                        Thread.Sleep(50);
                    }
                }
            }
    }
}
