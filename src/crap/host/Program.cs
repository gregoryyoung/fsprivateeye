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
           //starts profiling. 
           Console.WriteLine("*******************************************************************************************************************");
        }

        static void Start()
        {
            
        }

        static void Main(string[] args)
        {
            Console.WriteLine("starting");
            Console.WriteLine("calling flush method");
            BA91E1230BF74A17AB35D3879E65D032();
            Console.WriteLine("called flush opening file");
            Console.WriteLine("just some other shit!");
            using (var stream = new FileInfo("/tmp/test").OpenRead())
            {
                Console.WriteLine("file open, sending signal to start");
                var buffer = new byte[4906];
                while (true)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        var str = Encoding.ASCII.GetString(buffer);
                        Console.WriteLine("read {0} bytes {1}", read, str);
                    }
                    else
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(50));
                    }
                }
            }
        }
    }
}
