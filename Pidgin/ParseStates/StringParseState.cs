using System;

namespace Pidgin.ParseStates
{
    internal sealed class StringParseState : InMemoryParseState<char>
    {
        private readonly string _input;
        public StringParseState(string input, Func<char, SourcePos, SourcePos> posCalculator) : base(input.Length, posCalculator)
        {
            _input = input;
        }

        protected sealed override char GetElement(int index)
            => _input[index];
    }
}