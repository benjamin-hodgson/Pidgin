Changelog
=========

3.2.2
-----

### Added

* Support for AOT/trimming deployment scenarios.

3.2.1
-----

### Fixed

* A critical bug in `Try()` which had been broken in 3.2.0.
  * Thanks to @PJB3005 for reporting this bug in [#140](https://github.com/benjamin-hodgson/Pidgin/issues/140)

### Added

* Support for Source Link


3.2.0
-----

### Added

* Four new combinators: `ManyThen`, `AtLeastOnceThen`, `SkipManyThen`, and `SkipAtLeastOnceThen`
  * These are versions of `Until`, `AtLeastOnceUntil`, `SkipUntil`, and `SkipAtLeastOnceUntil` which return the terminator.
  * Thanks to @chyyran and @atrauzzi, who asked for this in [#121](https://github.com/benjamin-hodgson/Pidgin/issues/121)

### Changed

* Improved documentation for `ExpressionParser`, now including an example.
  * Thanks to @hswami, who asked for this in [#113](https://github.com/benjamin-hodgson/Pidgin/issues/113)
* Removed some Nuget dependencies which are no longer required (since they are part of .NET 5).


3.1.0
-----

### Added

* A pair of parsers for enum values, `Enum` and `CIEnum`. Thanks @RomanSoloweow!


3.0.0
-----

### Removed

* Removed support for .NET 4 and .NET Core 3.1. Pidgin is now a .NET 5 library.

### Added

* Published the (previously internal) `TokenStream` API. You can now write parsers which consume custom input streams.
* Support for resumable parsing, through the `ResumableTokenStream` class.
* An **experimental** API for writing your own parsers by subclassing `Parser`.

### Changed

* `Parser.Real` is now a property and not a method.
* A new design for computing source positions. `posCalculator` now returns a `SourcePosDelta` struct representing the amount of text covered by a token, rather than updating the current source position in place.
  * Performance improvements in the code which computes source positions.
* The `Parse` methods in `ParseExtensions` now take an `IConfiguration` object as an optional parameter (instead of a `posCalculator` func).
  * If you were using `posCalculator`, you can instead subclass `DefaultConfiguration` and override the `PosCalculator` property.
* Performance improvements across the board thanks to a new `Span`-based implementation.
* Performance improvements in `SkipWhitespaces`.
* A new CI build system based on GitHub Actions.

2.5.0
-----

### Changed

* Pidgin's assembly is now strong named.
* Performance improvements to `CurrentPos`.
* Internal simplifications to the error handling machinery.
* Pidgin's PDBs are now distributed through nuget.org as a snupkg (not from smbsrc.net).


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
