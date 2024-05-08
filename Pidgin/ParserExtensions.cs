using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

using Pidgin.Configuration;
using Pidgin.TokenStreams;

using Config = Pidgin.Configuration.Configuration;

namespace Pidgin;

/// <summary>
/// Extension methods for running parsers.
/// </summary>
public static class ParserExtensions
{
    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input string.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <returns>The result of parsing.</returns>
    public static Result<char, T> Parse<T>(this Parser<char, T> parser, string input, IConfiguration<char>? configuration = null)
        => Parse(parser, input.AsSpan(), configuration);

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input list.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <returns>The result of parsing.</returns>
    public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, IList<TToken> input, IConfiguration<TToken>? configuration = null)
        => Parse(parser, new ListTokenStream<TToken>(input), configuration);

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input list.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <returns>The result of parsing.</returns>
    public static Result<TToken, T> ParseReadOnlyList<TToken, T>(this Parser<TToken, T> parser, IReadOnlyList<TToken> input, IConfiguration<TToken>? configuration = null)
        => Parse(parser, new ReadOnlyListTokenStream<TToken>(input), configuration);

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input enumerable.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <returns>The result of parsing.</returns>
    public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, IEnumerable<TToken> input, IConfiguration<TToken>? configuration = null)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        using var e = input.GetEnumerator();
        return Parse(parser, e, configuration);
    }

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input enumerator.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <returns>The result of parsing.</returns>
    public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, IEnumerator<TToken> input, IConfiguration<TToken>? configuration = null)
        => Parse(parser, new EnumeratorTokenStream<TToken>(input), configuration);

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// Note that more characters may be consumed from <paramref name="input"/> than were required for parsing.
    /// You may need to manually rewind <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input stream.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <returns>The result of parsing.</returns>
    public static Result<byte, T> Parse<T>(this Parser<byte, T> parser, Stream input, IConfiguration<byte>? configuration = null)
        => Parse(parser, new StreamTokenStream(input), configuration);

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input reader.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <returns>The result of parsing.</returns>
    public static Result<char, T> Parse<T>(this Parser<char, T> parser, TextReader input, IConfiguration<char>? configuration = null)
        => Parse(parser, new ReaderTokenStream(input), configuration);

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input array.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <returns>The result of parsing.</returns>
    public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, TToken[] input, IConfiguration<TToken>? configuration = null)
        => parser.Parse(input.AsSpan(), configuration);

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input span.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <returns>The result of parsing.</returns>
    public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, ReadOnlySpan<TToken> input, IConfiguration<TToken>? configuration = null)
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        var state = new ParseState<TToken>(configuration ?? Config.Default<TToken>(), input);
        var result = Parse(parser, ref state);
        return result;
    }

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input <see cref="ITokenStream{TToken}" />.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <returns>The result of parsing.</returns>
    public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, ITokenStream<TToken> input, IConfiguration<TToken>? configuration = null)
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var state = new ParseState<TToken>(configuration ?? Config.Default<TToken>(), input);
        return Parse(parser, ref state);
    }

    /// <summary>
    /// Run the <paramref name="parser"/> on the input <paramref name="state"/>.
    ///
    /// WARNING: This API is <strong>unstable</strong>
    /// and subject to change in future versions of the library.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="state">An input <see cref="ParseState{TToken}" />.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <returns>The result of parsing.</returns>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static Result<TToken, T> Parse<TToken, T>(this Parser<TToken, T> parser, ref ParseState<TToken> state)
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        var expecteds = new PooledList<Expected<TToken>>(state.Configuration.ArrayPoolProvider.GetArrayPool<Expected<TToken>>());

        var result1 = parser.TryParse(ref state, ref expecteds, out var result)
            ? new Result<TToken, T>(result)
            : new Result<TToken, T>(state.BuildError(ref expecteds));

        expecteds.Dispose();
        state.Dispose();  // ensure we return the state's buffers to the buffer pool

        return result1;
    }

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input string.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <exception cref="ParseException">Thrown when an error occurs during parsing.</exception>
    /// <returns>The result of parsing.</returns>
    public static T ParseOrThrow<T>(this Parser<char, T> parser, string input, IConfiguration<char>? configuration = null)
        => GetValueOrThrow(parser.Parse(input, configuration));

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input list.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <exception cref="ParseException">Thrown when an error occurs during parsing.</exception>
    /// <returns>The result of parsing.</returns>
    public static T ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, IList<TToken> input, IConfiguration<TToken>? configuration = null)
        => GetValueOrThrow(parser.Parse(input, configuration));

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input list.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <exception cref="ParseException">Thrown when an error occurs during parsing.</exception>
    /// <returns>The result of parsing.</returns>
    public static T ParseReadOnlyListOrThrow<TToken, T>(this Parser<TToken, T> parser, IReadOnlyList<TToken> input, IConfiguration<TToken>? configuration = null)
        => GetValueOrThrow(parser.ParseReadOnlyList(input, configuration));

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input enumerable.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <exception cref="ParseException">Thrown when an error occurs during parsing.</exception>
    /// <returns>The result of parsing.</returns>
    public static T ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, IEnumerable<TToken> input, IConfiguration<TToken>? configuration = null)
        => GetValueOrThrow(parser.Parse(input, configuration));

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input enumerator.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <exception cref="ParseException">Thrown when an error occurs during parsing.</exception>
    /// <returns>The result of parsing.</returns>
    public static T ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, IEnumerator<TToken> input, IConfiguration<TToken>? configuration = null)
        => GetValueOrThrow(parser.Parse(input, configuration));

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input stream.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <exception cref="ParseException">Thrown when an error occurs during parsing.</exception>
    /// <returns>The result of parsing.</returns>
    public static T ParseOrThrow<T>(this Parser<byte, T> parser, Stream input, IConfiguration<byte>? configuration = null)
        => GetValueOrThrow(parser.Parse(input, configuration));

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input reader.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <exception cref="ParseException">Thrown when an error occurs during parsing.</exception>
    /// <returns>The result of parsing.</returns>
    public static T ParseOrThrow<T>(this Parser<char, T> parser, TextReader input, IConfiguration<char>? configuration = null)
        => GetValueOrThrow(parser.Parse(input, configuration));

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input array.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <exception cref="ParseException">Thrown when an error occurs during parsing.</exception>
    /// <returns>The result of parsing.</returns>
    public static T ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, TToken[] input, IConfiguration<TToken>? configuration = null)
        => GetValueOrThrow(parser.Parse(input, configuration));

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input span.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <exception cref="ParseException">Thrown when an error occurs during parsing.</exception>
    /// <returns>The result of parsing.</returns>
    public static T ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, ReadOnlySpan<TToken> input, IConfiguration<TToken>? configuration = null)
        => GetValueOrThrow(parser.Parse(input, configuration));

    /// <summary>
    /// Applies <paramref name="parser"/> to <paramref name="input"/>.
    /// </summary>
    /// <param name="parser">A parser.</param>
    /// <param name="input">An input <see cref="ITokenStream{TToken}" />.</param>
    /// <param name="configuration">The configuration, or null to use the default configuration.</param>
    /// <typeparam name="TToken">The type of the tokens in the parser's input stream.</typeparam>
    /// <typeparam name="T">The type of the value returned by the parser.</typeparam>
    /// <exception cref="ParseException">Thrown when an error occurs during parsing.</exception>
    /// <returns>The result of parsing.</returns>
    public static T ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, ITokenStream<TToken> input, IConfiguration<TToken>? configuration = null)
        => GetValueOrThrow(parser.Parse(input, configuration));

    private static T GetValueOrThrow<TToken, T>(Result<TToken, T> result)
        => result.Success ? result.Value : throw new ParseException<TToken>(result.Error!);
}
