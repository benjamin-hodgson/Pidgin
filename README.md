Pidgin
======
[![Build status](https://ci.appveyor.com/api/projects/status/gaqvyy3x8ukj6qp9?svg=true)](https://ci.appveyor.com/project/benjamin-hodgson/pidgin)


A lightweight, fast, and flexible parsing library for C#, developed at Stack Overflow.

Installing
----------

Pidgin is [available on Nuget](https://www.nuget.org/packages/Pidgin/). Dev builds can be found [on our AppVeyor Nuget feed](https://ci.appveyor.com/nuget/pidgin-gmkqlk6fi2fp).

Tutorial
--------

### Getting started

Pidgin is a _parser combinator library_, a lightweight, high-level, declarative tool for constructing parsers. Parsers written with parser combinators look like a high-level specification of a language's grammar, but they're expressed within a general-purpose programming language and require no special tools to produce executable code. Parser combinators are more powerful than regular expressions - they can parse a larger class of languages - but simpler and easier to use than parser generators like ANTLR.

Pidgin's core type, `Parser<TToken, T>`, represents a procedure which consumes an input stream of `TToken`s, and may either fail with a parsing error or produce a `T` as output. You can think of it as:

```csharp
delegate T? Parser<TToken, T>(IEnumerator<TToken> input);
```

In order to start building parsers we need to import two classes which contain factory methods: `Parser` and `Parser<TToken>`. 

```csharp
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;  // we'll be parsing strings - sequences of of characters. For other applications (eg parsing binary file formats) TToken may be some other type (eg byte).
```

### Primitive parsers

Now we can create some simple parsers. `Any` represents a parser which consumes a single character and returns that character.

```csharp
Assert.AreEqual('a', Any.ParseOrThrow("a"));
Assert.AreEqual('b', Any.ParseOrThrow("b"));
```

`Char`, an alias for `Token` consumes a _particular_ character and returns that character. If it encounters some other character then it fails.

```csharp
Parser<char, char> parser = Char('a');
Assert.AreEqual('a', parser.ParseOrThrow("a"));
Assert.Throws<ParseException>(() => parser.ParseOrThrow("b"));
```

`Digit` parses and returns a single digit character.
```csharp
Assert.AreEqual('3', Digit.ParseOrThrow("3"));
Assert.Throws<ParseException>(() => Digit.ParseOrThrow("a"));
```

`String` parses and returns a particular string. If you give it input other than the string it was expecting it fails.

```csharp
Parser<char, string> parser = String("foo");
Assert.AreEqual("foo", parser.ParseOrThrow("foo"));
Assert.Throws<ParseException>(() => parser.ParseOrThrow("bar")));
```

`Return` (and its synonym `FromResult`) never consumes any input, and just returns the given value. Likewise, `Fail` always fails without consuming any input.

```csharp
Parser<char, int> parser = Return(3);
Assert.AreEqual(3, parser.ParseOrThrow("foo"));

Parser<char, int> parser2 = Fail<int>();
Assert.Throws<ParseException>(() => parser2.ParseOrThrow("bar"));
```

### Sequencing parsers

The power of parser combinators is that you can build big parsers out of little ones. The simplest way to do this is using `Then`, which builds a new parser representing two parsers applied sequentially (discarding the result of the first).

```csharp
Parser<char, string> parser1 = String("foo");
Parser<char, string> parser2 = String("bar");
Parser<char, string> sequencedParser = parser1.Then(parser2);
Assert.AreEqual("bar", sequencedParser.ParseOrThrow("foobar"));  // "foo" got thrown away
Assert.Throws<ParseException>(() => sequencedParser.ParseOrThrow("food")));
```

`Before` throws away the second result, not the first.

```csharp
Parser<char, string> parser1 = String("foo");
Parser<char, string> parser2 = String("bar");
Parser<char, string> sequencedParser = parser1.Before(parser2);
Assert.AreEqual("foo", sequencedParser.ParseOrThrow("foobar"));  // "foo" got thrown away
Assert.Throws<ParseException>(() => sequencedParser.ParseOrThrow("food")));
```

`Map` does a similar job, except it keeps both results and applies a transformation function to them. This is especially useful when you want your parser to return a custom data structure. (`Map` has overloads which operate on between one and eight parsers; the one-parser version also has a postfix synonym `Select`.)

```csharp
Parser<char, string> parser1 = String("foo");
Parser<char, string> parser2 = String("bar");
Parser<char, string> sequencedParser = Map((foo, bar) => bar + foo, parser1, parser2);
Assert.AreEqual("barfoo", sequencedParser.ParseOrThrow("foobar")));
Assert.Throws<ParseException>(() => sequencedParser.ParseOrThrow("food")));
```

`Bind` uses the result of a parser to choose the next parser. This enables parsing of context-sensitive languages. For example, here's a parser which parses any character repeated twice.

```csharp
/// parse any character, then parse a character matching the first character
Parser<char, char> parser = Any.Bind(c => Char(c));
Assert.AreEqual('a', parser.ParseOrThrow("aa"));
Assert.AreEqual('b', parser.ParseOrThrow("bb"));
Assert.Throws<ParseException>(() => parser.ParseOrThrow("ab")));
```

Pidgin parsers support LINQ query syntax. It may be easier to see what the above example does when it's written out using LINQ:

```csharp
Parser<char, char> parser =
    from c in Any
    from c2 in Char(c)
    select c2;
```

Parsers written like this look like a simple imperative script. "Run the `Any` parser and name its result `c`, then run `Char(c)` and name its result `c2`, then return `c2`."

### Choosing from alternatives

`Or` represents a parser which can parse one of two alternatives. It runs the left parser first, and if it fails it tries the right parser.

```csharp
Parser<char, string> parser = String("foo").Or(String("bar"));
Assert.AreEqual("foo", parser.ParseOrThrow("foo"));
Assert.AreEqual("bar", parser.ParseOrThrow("bar"));
Assert.Throws<ParseError>(() => parser.ParseOrThrow("baz"));
```

`OneOf` is equivalent to `Or`, except it takes a variable number of arguments. Here's a parser which is equivalent to the one using `Or` above:

```csharp
Parser<char, string> parser = OneOf(String("foo"). String("bar"));
```

If one of `Or` or `OneOf`'s component parsers fails _after consuming input_, the whole parser will fail.

```csharp
Parser<char, string> parser = String("food").Or("foul");
Assert.Throws<ParseError>(() => parser.ParseOrThrow("foul"));  // why didn't it choose the second option?
```

What happened here? When a parser successfully parses a character from the input stream, it advances the input stream to the next character. `Or` only chooses the next alternative if the given parser fails _without consuming any input_, so that the state Pidgin does not perform any lookahead or backtracking by default. Backtracking is enabled using the `Try` function.

```csharp
// apply Try to the first option, so we can return to the beginning if it fails
Parser<char, string> parser = Try(String("food")).Or("foul");
Assert.AreEqual("foul", parser.ParseOrThrow("foul"));
```

### Recursive grammars

Almost any non-trivial programming language, markup language, or data interchange language will feature some sort of recursive structure. C# doesn't support recursive values: a recursive referral to a variable currently being initialised will return `null`. So we need some sort of deferred execution of recursive parsers, which Pidgin enables using the `Rec` combinator. Here's a simple parser which parses arbitrarily nested parentheses with a single digit inside them.

```csharp
Parser<char, char> expr = null;
Parser<char, char> parenthesised = Char('(')
    .Then(Rec(() => expr))  // using a lambda to (mutually) recursively refer to expr
    .Before(Char(')'));
expr = Digit.Or(parenthesised);
Assert.AreEqual('1', expr.ParseOrThrow("1"));
Assert.AreEqual('1', expr.ParseOrThrow("(1)"));
Assert.AreEqual('1', expr.ParseOrThrow("(((1)))"));
```

However, Pidgin does not support left recursion. A parser must consume some input before making a recursive call. The following example will produce a stack overflow because a recursive call to `arithmetic` occurs before any input can be consumed by `Digit` or `Char('+')`:

```csharp
Parser<char, int> arithmetic = null;
Parser<char, int> addExpr = Map(
    (x, y) => x + y,
    Rec(() => arithmetic),
    Char('+'),
    Rec(() => arithmetic)
);
arithmetic = addExpr.Or(Digit.Select(char.GetNumericValue));

arithmetic.Parse("2+2");  // stack overflow!
```

### Derived combinators

Another powerful element of this programming model is that you can write your own functions to compose parsers. Pidgin contains a large number of higher-level combinators, built from the primitives outlined above. For example, `Between` runs a parser surrounded by two others, keeping only the result of the central parser.

```csharp
Parser<TToken, T> InBraces<TToken, T, U, V>(this Parser<TToken, T> parser, Parser<TToken, U> before, Parser<TToken, V> after)
    => before.Then(parser).Before(after);
```

### Parsing expressions

Pidgin features operator-precedence parsing tools, for parsing expression grammars with associative infix operators. The `ExpressionParser` class builds a parser from a parser to parse a single expression term and a table of operators with rules to combine expressions.

### More examples

Examples, such as parsing (a subset of) JSON and XML into document structures, can be found in the `Pidgin.Examples` project.

Tips
----

### Speed tips

Pidgin is designed to be fast and produce a minimum of garbage. A carefully written Pidgin parser can be as fast as a hand-written recursive descent parser. If you find that parsing is a bottleneck in your code, here are some tips for minimising the runtime of your parser.

* Avoid LINQ query syntax. Query comprehensions are defined by translation into core C# using `SelectMany`, however, for long queries the translation can allocate a large number of anonymous objects. This generates a lot of garbage; while those objects often won't survive the nursery it's still preferable to avoid allocating them!
* Avoid backtracking where possible.
* Use specialised parsers where possible: the provided `Skip*` parsers can be used when the result of parsing is not required. They typically run faster than their counterparts because they don't need to save the values generated.
* Avoid `Bind` and `SelectMany` where possible. These functions build parsers dynamically, based on the result of the previous parser. Building a parser can be an expensive operation. Many practical grammars are _context-free_ and can therefore be written purely with `Map`. If you do have a context-sensitive grammar, it may make sense to parse it in a context-free fashion and then run a semantic checker over the result.
* Build your parser statically where possible.

Comparison to other tools
-------------------------

### Pidgin vs Sprache

[Sprache](https://github.com/sprache/Sprache) is another parser combinator library for C# and served as one of the sources of inspiration for Pidgin. Pidgin's API is somewhat similar to that of Sprache, but Pidgin aims to improve on Sprache in a number of ways:

* Sprache's input must be a string. This makes it inappropriate for parsing binary protocols or tokenised inputs. Pidgin supports input tokens of arbitrary type.
* Sprache's input must be a string - an _in-memory_ array of characters. Pidgin supports streaming inputs.
* Sprache automatically backtracks on failure. Pidgin uses a special combinator to enable backtracking because backtracking can be a costly operation.
* Pidgin comes bundled with operator-precedence tools for parsing expression languages with associative infix operators.
* Pidgin is faster and allocates less memory than Sprache.
* Pidgin has more documentation coverage than Sprache.

### Pidgin vs FParsec

[FParsec](https://github.com/stephan-tolksdorf/fparsec) is a parser combinator library for F# based on [Parsec](https://hackage.haskell.org/package/parsec-3.1.11).

* FParsec is an F# library and consuming it from C# can be awkward. Pidgin is implemented in pure C#, and is designed for C# consumers.
* FParsec only supports character input streams.
* FParsec supports stateful parsing - it has an extra type parameter for an arbitrary user-defined state - which can make it easier to parse context-sensitive grammars.
* FParsec is faster than Sprache (though we hope to catch up!)

