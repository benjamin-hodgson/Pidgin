Changelog
=========

2.4.0
-----

### Added

* `CurrentOffset`, which returns the number of tokens consumed
* A new overload of `Rec`: `Func<Parser<TToken, T> Rec(Func<Parser<TToken, T>, Parser<TToken, T>>)`
* `Slice`, a synonym of `MapWithInput`

### Fixed

* The `Real` parser now uses the invariant culture. Thank you @SigridAndersen for the contribution!

2.3.0
-----

### Added

* Nullability checks

### Fixed

* A bug in `HexNum`. Thank you @comaid for the contribution!
* Some `ArrayPool` leaks.
* A bug causing `Sequence` to fail when its type argument was `IComparable` but not `IEquatable`

### Changed

* Significant performance improvements when parsing from a non-chunked in-memory source such as a string or an array.
* `SkipWhitespaces` has been rewritten --- it should now run much faster

### Removed

* Support for netstandard1.3


2.2.0
-----

### Added

* `MapWithInput`, giving access to a `Span` containing the input tokens which were matched by the parser.
* An infix version of `Map` (synonym of `Select`).
* `Real`, a parser for floating point values in the format `+1.23e4`

### Changed

* Under-the-hood performance improvements to the way `SourcePos` is handled
* A faster implementation of `CIString`


2.1.0
-----

### Added

* Overloads of `ExpressionParser.Build` to make recursive grammars more convenient


2.0.1
-----

### Fixed

* An `ArrayPool` leak when certain parsers failed


2.0.0
-----

### Added

* A `ParserExtensions.Parse` overload which accepts a `ReadOnlySpan`.
* A compile target for `netstandard2.0`. This should simplify installation into .NET Framework projects.
* Performance improvements across the board

### Changed

* Error handling was rewritten.
  * `ParseError` is now a class and not a struct.
  * Fewer items are reported in `Expected`.
* `Parser<TToken>.End` is now a property and not a method.
* `Result<TToken, T>` is now a class and not a struct.
* When parsing from streaming inputs like `Stream` or `TextReader`, the stream will now usually advance beyond the last character consumed by the parser

### Fixed

* Fixed an internal potential memory leak due to the use of pooled memory
