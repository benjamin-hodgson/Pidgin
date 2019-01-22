Changelog
=========

2.0.0
-----

### Added

* A `ParserExtensions.Parse` overload which accepts a `ReadOnlySpan`.
* A compile target for `netstandard2.0`. This should simplify installation into .NET Framework projects.
* Performance improvements across the board


### Changed

* Error handling was rewritten.
  * `ParseError` is now a class and not a struct.
  * The order of items in `Expected` has changed.
* `Parser<TToken>.End` is now a property and not a method.
* `Result<TToken, T>` is now a class and not a struct.
* When parsing from streaming inputs like `Stream` or `TextReader`, the stream will now usually advance beyond the last character consumed by the parser

### Fixed

* Fixed an internal potential memory leak due to the use of pooled memory
