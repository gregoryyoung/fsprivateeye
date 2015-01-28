using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;


namespace ConsoleTest
{
    public class Filter
    {
        public Filter()
        {
            BufferSize = 64 * 1024;
            ThresholdSize = 2 * 1024;
        }

        /// <summary>
        /// 
        /// </summary>
        public int BufferSize { get; set; }
        public int ThresholdSize { get; set; }
        public string Includes { get; set; }
        public string Excludes { get; set; }
    }

    public static class Control
    {
        /// <summary>
        /// Run a process
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="process"></param>
        /// <param name="onCallback"></param>
        /// <param name="onError"></param>
        /// <param name="onComplete"></param>
        public static void RunProcess(Filter filter, Action<Action<StringDictionary>> process, Action<int, byte[]> onCallback, Action<Exception> onError, Action onComplete)
        {
            filter = filter ?? new Filter();
            var tag = (new Random((int)DateTime.UtcNow.Ticks)).Next().ToString();

            var readBufferEvt = new EventWaitHandle(false, EventResetMode.ManualReset, "Local\\MM_ReadBuffer" + tag);
            var bufferReadEvt = new EventWaitHandle(false, EventResetMode.ManualReset, "Local\\MM_BufferRead" + tag);
            var workItemClosedEvt = new AutoResetEvent(false);
            var waitHandles = new WaitHandle[] { workItemClosedEvt, readBufferEvt };

            var includes = string.IsNullOrWhiteSpace(filter.Includes) ? String.Empty.ToCharArray() : filter.Includes.ToCharArray();
            var excludes = string.IsNullOrWhiteSpace(filter.Excludes) ? String.Empty.ToCharArray() : filter.Excludes.ToCharArray();

            const int pfs = (32768 * 4) + 4 + 4;

            using (var mmf2 = MemoryMappedFile.CreateOrOpen("Local\\MMProfilerMapBuffer" + tag, 4 + filter.BufferSize + filter.ThresholdSize))
            using (var mmf = MemoryMappedFile.CreateOrOpen("Local\\MMProfilerMapControl" + tag, pfs))
            using (var filterAccessor = mmf.CreateViewAccessor(0, pfs, MemoryMappedFileAccess.ReadWrite))
            using (var lengthAccessor = mmf2.CreateViewAccessor(0, 4, MemoryMappedFileAccess.ReadWrite))
            using (var streamAccessor = mmf2.CreateViewStream(4, filter.BufferSize + filter.ThresholdSize, MemoryMappedFileAccess.ReadWrite))
            {
                filterAccessor.Write(0, filter.BufferSize);
                filterAccessor.Write(4, filter.ThresholdSize);
                filterAccessor.WriteArray(4 + 4, includes, 0, includes.Count());
                filterAccessor.WriteArray(32768 * 2 + 4 + 4, excludes, 0, excludes.Count());

                ThreadPool.QueueUserWorkItem((state) =>
                {
                    try
                    {
                        process((dictionary) =>
                        {
                            if (dictionary == null) return;
                            dictionary["MMProfiler_Tag"] = tag;
                            dictionary["COR_PROFILER"] = "{36C8D782-F697-45C4-856A-92D05C061A39}";
                            dictionary["COR_ENABLE_PROFILING"] = "1";
                        });
                    }
                    finally
                    {
                        workItemClosedEvt.Set();
                    }
                });

                var data = new byte[filter.BufferSize + filter.ThresholdSize];
                var length = 0;
                var error = false;
                try
                {
                    bool bTerminate = false;
                    while (!bTerminate)
                    {
                        switch (WaitHandle.WaitAny(waitHandles))
                        {
                            case 0:
                                bTerminate = true;
                                break;
                            case 1:
                                readBufferEvt.Reset();

                                length = lengthAccessor.ReadInt32(0);
                                lengthAccessor.Write(0, (Int32)0);
                                streamAccessor.Read(data, 0, length);
                                streamAccessor.Seek(0, SeekOrigin.Begin);

                                bufferReadEvt.Set();
                                bufferReadEvt.Reset();

                                onCallback(length, data);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    onError(ex);
                    error = true;
                }
                finally
                {
                    if (!error)
                    {
                        length = lengthAccessor.ReadInt32(0);
                        streamAccessor.Read(data, 0, length);
                        onCallback(length, data);
                        onComplete();
                    }
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var fileName = Path.Combine(Path.GetTempPath(), "MMProfiler.log");
            File.Delete(fileName);
            Console.WriteLine(fileName);
            var fs = File.Create(fileName);

            Control.RunProcess(new Filter() { BufferSize = 1024 * 1024, ThresholdSize = 64 * 1024, Includes = "Test" },
                (env) =>
                {
                    var info = new ProcessStartInfo {UseShellExecute = false, FileName = "fsi.exe"};
                    env(info.EnvironmentVariables);
                    testProcess(info);
                },
                (length, buffer) => fs.Write(buffer, 0, length),
                (exception) => { },
                fs.Close);

            ValidateOutputFile(fileName);
        }

        private static void testProcess(ProcessStartInfo info)
        {
            Process.Start(info).WaitForExit();
        }

        private static void ValidateOutputFile(string fileName)
        {
            var validate = File.OpenRead(fileName);
            var reader = new BinaryReader(validate);
            reader.BaseStream.Position = 0;
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var key = reader.ReadByte();
                switch (key)
                {
                    case 1:
                        reader.ReadUInt32();
                        reader.ReadDouble();
                        reader.ReadInt32();
                        reader.ReadUInt64();
                        Console.WriteLine("{0}", Encoding.Unicode.GetString(reader.ReadBytes(reader.ReadInt32() * 2)));
                        Console.WriteLine("\t{0}", Encoding.Unicode.GetString(reader.ReadBytes(reader.ReadInt32() * 2)));
                        break;
                    case 2:
                        reader.ReadUInt32();
                        reader.ReadDouble();
                        reader.ReadInt32();
                        reader.ReadUInt64();
                        break;
                    case 3:
                        reader.ReadUInt32();
                        reader.ReadDouble();
                        reader.ReadInt32();
                        reader.ReadUInt64();
                        break;
                    default:
                        Console.WriteLine("Validation failed {0} {1}", key, reader.BaseStream.Position);
                        return;
                }
            }

            Console.WriteLine("File validated");
        }
    
    
    }
}
