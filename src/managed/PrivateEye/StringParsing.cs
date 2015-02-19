using System;
using System.Text;

namespace PrivateEye
{
    static class StringParsing
    {
        //TODO move to binary protocol? This is likely fast enough and is easily readble through files etc
        public static Tuple<string, ParserState> ReadNextLine(byte[] buffer, int length, ParserState state)
        {
            for (var i = state.Position; i < length; i++)
            {
                if (buffer[i] != '\n') continue;
                var str = Encoding.ASCII.GetString(buffer, state.Position, i - state.Position);
                if (state.Buffer != null)
                    str = state.Buffer + str;
                return new Tuple<string, ParserState>(str, new ParserState { LineRead = true, Position = i + 1 });
            }
            var left = Encoding.ASCII.GetString(buffer, state.Position, length - state.Position);
            return new Tuple<string, ParserState>("", new ParserState { Buffer = left, LineRead = false, Position = 0 });
        }

        public static Tuple<ulong, int> ReadULong(string line, int start)
        {
            ulong ret = 0;
            for (var i = start; i < line.Length; i++)
            {
                if (line[i] == ',' || line[i] == '\n')
                {
                    return new Tuple<ulong, int>(ret, i+1);
                }
                ret = ret * 10 + line[i] - '0';
            }
            return new Tuple<ulong, int>(ret, line.Length);
        }

        public static Tuple<string, int> ReadString(string line, int start)
        {
            return new Tuple<string, int>(line.Substring(start, line.Length - start), line.Length);
        }
    }

    struct ParserState
    {
        public int Position;
        public bool LineRead;
        public string Buffer;
    }
}
