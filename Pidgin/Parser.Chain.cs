using System;
using System.Collections.Immutable;

namespace Pidgin
{
    public partial class Parser<TToken, T>
    {
        private Parser<TToken, U> ReturnSeed<U>(Func<U> seed)
            => Parser<TToken>
                .Return(Unit.Value)
                .Select(_ => seed());

        internal Parser<TToken, U> ChainL<U>(Func<U> seed, Func<U, T, U> func)
            => this.ChainAtLeastOnceL(seed, func)
                .Or(ReturnSeed(seed));

        internal Parser<TToken, U> ChainAtLeastOnceL<U>(Func<U> seed, Func<U, T, U> func)
            => new ChainAtLeastOnceLParser<U>(this, seed, func);

        private class ChainAtLeastOnceLParser<U> : Parser<TToken, U>
        {
            private readonly Parser<TToken, T> _parser;
            private readonly Func<U> _seed;
            private readonly Func<U, T, U> _func;

            public ChainAtLeastOnceLParser(Parser<TToken, T> parser, Func<U> seed, Func<U, T, U> func)
            {
                _parser = parser;
                _seed = seed;
                _func = func;
            }

            internal override InternalResult<U> Parse(ref ParseState<TToken> state)
            {
                var result1 = _parser.Parse(ref state);
                if (!result1.Success)
                {
                    // state.Error set by _parser
                    return InternalResult.Failure<U>(result1.ConsumedInput);
                }
                var z = _func(_seed(), result1.Value);
                var consumedInput = result1.ConsumedInput;

                state.BeginExpectedTran();
                var result = _parser.Parse(ref state);
                while (result.Success)
                {
                    state.EndExpectedTran(false);
                    if (!result.ConsumedInput)
                    {
                        throw new InvalidOperationException("Many() used with a parser which consumed no input");
                    }
                    consumedInput = true;
                    z = _func(z, result.Value);

                    state.BeginExpectedTran();
                    result = _parser.Parse(ref state);
                }
                state.EndExpectedTran(result.ConsumedInput);
                if (result.ConsumedInput)  // the most recent parser failed after consuming input
                {
                    // state.Error set by _parser
                    return InternalResult.Failure<U>(true);
                }
                return InternalResult.Success<U>(z, consumedInput);
            }
        }
	
        // Alternative chain parser
        // This is a new type of chain route that allows the accumulation of complex strings
        // and the subsequent validation of the result.

		internal Parser<TToken, U> ChainAtLeastOnceAL<U>(
                Func<U> seed, 
                Func<U, T, string, U> func,
                Func<U, string, bool> post)
           => new ChainAtLeastOnceLParserA<U>(this, seed, func, post);

        // Class for chaining multiple parsers with string processing
        // Supports seed, processes (func), and post process validation (post)
        private class ChainAtLeastOnceLParserA<U> : Parser<TToken, U>
        {
            private readonly Parser<TToken, T> _parser;
            private readonly Func<U> _seed;
            private readonly Func<U, T, string, U> _func;
            private readonly Func<U, string, bool> _post;

            public ChainAtLeastOnceLParserA(Parser<TToken, T> parser, 
                Func<U> seed, Func<U, T, string, U> func, Func<U, string, bool> post)
            {
                _parser = parser;
                _seed = seed;
                _func = func;
                _post = post;
            }

            internal override InternalResult<U> Parse(ref ParseState<TToken> state)
            {
                var result1 = _parser.Parse(ref state);
                if (!result1.Success)
                {
                    // state.Error set by _parser
                    return InternalResult.Failure<U>(result1.ConsumedInput);
                }
                var s = RestoreCharacter(result1.Value);
                var z = _func(_seed(), result1.Value, s);
                var consumedInput = result1.ConsumedInput;

                state.BeginExpectedTran();
                var result = _parser.Parse(ref state);
                while (result.Success)
                {
                    state.EndExpectedTran(false);
                    if (!result.ConsumedInput)
                    {
                        throw new InvalidOperationException("Many() used with a parser which consumed no input");
                    }
                    consumedInput = true;
                    s = s + RestoreCharacter(result.Value);
                    z = _func(z, result.Value, s);

                    state.BeginExpectedTran();
                    result = _parser.Parse(ref state);
                }

                state.EndExpectedTran(result.ConsumedInput);

                // After callback (if defined
                //if (_post != null)
                //{
                    if (_post(z, s) == false)
                    {
                        return InternalResult.Failure<U>(true);
                    }
                //}
               
                if (result.ConsumedInput)  // the most recent parser failed after consuming input
                {
                    // state.Error set by _parser
                    return InternalResult.Failure<U>(true);
                }
                return InternalResult.Success<U>(z, consumedInput);
            }

        }

        // This method effectively returns the parser (transition) character.
        private string RestoreCharacter(object ResultValue)
        {
            string returnString = "";
            if (ResultValue != null)
            {
                int i = Convert.ToInt32(ResultValue);
                char c = Convert.ToChar(i);
                returnString = c.ToString();
            }
            return returnString;

        }
    }
}