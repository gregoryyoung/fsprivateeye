using System;
using System.Text;

namespace host
{
    static class Parser
    {
        public static Tuple<string, ParserState> ReadNextLine(byte []buffer, int length, ParserState state)
        {
            for (var i = state.Position; i < length; i++)
            {
                if (buffer[i] != '\n') continue;
                var str = Encoding.ASCII.GetString(buffer, state.Position, i);
                if (state.Buffer != null)
                    str = state.Buffer + str;
                return new Tuple<string, ParserState>(str, new ParserState {LineRead=true,Position=i});
            }
            var left = Encoding.ASCII.GetString(buffer, state.Position, length - state.Position);
            return new Tuple<string, ParserState>("", new ParserState {Buffer=left,LineRead = false, Position = buffer.Length});
        }
    }

    struct ParserState
    {
        public int Position;
        public bool LineRead;
        public string Buffer;
    }
}