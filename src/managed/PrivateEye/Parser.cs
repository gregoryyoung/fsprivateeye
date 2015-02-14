using System;
using System.Text;

namespace PrivateEye
{
    static class Parser
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
    }

    struct ParserState
    {
        public int Position;
        public bool LineRead;
        public string Buffer;
    }
}