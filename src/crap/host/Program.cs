using System;
using System.IO;
using System.Threading;

namespace host
{
    class Program
    {

        static void Flush()
        {
            
        }

        static void Start()
        {
            
        }

        static void Stop()
        {
            
        }

        static void Main(string[] args)
        {
            using (var stream = new FileStream("/tmp/test", FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[4906];
                while (true)
                {
                    var read = stream.Read(buffer, 0, buffer.Length);
                    if (read > 0)
                    {
                        Console.WriteLine("read {0} bytes", read);
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
