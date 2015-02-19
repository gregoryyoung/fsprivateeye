using System;
using PrivateEye;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting up the test app.");
            Profiler.OnMethodCalled += data => Console.WriteLine("Method Called " + data.Time);
            Console.WriteLine("Enabling profiler.");
            Profiler.Start();
            Profiler.EnableProfiler();
        }
    }
}
