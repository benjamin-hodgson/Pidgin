using System;
using System.Collections;

namespace Pidgin.TokenStreams
{
    internal class StringTokenStream : InMemoryTokenStream<char>
    {
        private readonly string _input;

        public StringTokenStream(string value) : base(value.Length)
        {
            _input = value;
        }

        public override char Current => _input[_index];
    }
}
