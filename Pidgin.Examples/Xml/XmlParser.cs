using System;
using System.Collections.Generic;
using System.Linq;

using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Pidgin.Examples.Xml;

public static class XmlParser
{
    public static Result<char, Tag> Parse(string input) => _node.Parse(input);

    private static readonly Parser<char, string> _identifier =
        from first in Token(char.IsLetter)
        from rest in Token(char.IsLetterOrDigit).ManyString()
        select first + rest;
    private static readonly Parser<char, char> _lt = Char('<');
    private static readonly Parser<char, char> _gt = Char('>');
    private static readonly Parser<char, char> _quote = Char('"');
    private static readonly Parser<char, char> _equal = Char('=');
    private static readonly Parser<char, char> _slash = Char('/');
    private static readonly Parser<char, Unit> _slashGT =
        _slash.Then(Whitespaces).Then(_gt).Then(Return(Unit.Value));
    private static readonly Parser<char, Unit> _ltSlash =
        _lt.Then(Whitespaces).Then(_slash).Then(Return(Unit.Value));
    private static readonly Parser<char, string> _attrValue =
        Token(c => c != '"').ManyString();
    private static readonly Parser<char, Attribute> _attr =
        from name in _identifier
        from eq in _equal.Between(SkipWhitespaces)
        from val in _attrValue.Between(_quote)
        select new Attribute(name, val);
    private static readonly Parser<char, OpeningTagInfo> _tagBody =
        from name in _identifier
        from attrs in (
            from ws in Try(Whitespace.SkipAtLeastOnce())
            from attrs in _attr.Separated(SkipWhitespaces)
            select attrs
        ).Optional()
        select new OpeningTagInfo(name, attrs.GetValueOrDefault(Enumerable.Empty<Attribute>()));
    private static readonly Parser<char, Tag> _emptyElementTag =
        from opening in _lt
        from body in _tagBody.Between(SkipWhitespaces)
        from closing in _slashGT
        select new Tag(body.Name, body.Attributes, null);
    private static readonly Parser<char, OpeningTagInfo> _openingTag =
        _tagBody
            .Between(SkipWhitespaces)
            .Between(_lt, _gt);

    private static Parser<char, string> ClosingTag =>
        _identifier
            .Between(SkipWhitespaces)
            .Between(_ltSlash, _gt);

    private static readonly Parser<char, Tag> _tag =
        from open in _openingTag
        from children in Try(_node!).Separated(SkipWhitespaces).Between(SkipWhitespaces)
        from close in ClosingTag
        where open.Name.Equals(close, StringComparison.Ordinal)
        select new Tag(open.Name, open.Attributes, children);
    private static readonly Parser<char, Tag> _node = Try(_emptyElementTag).Or(_tag);

    private struct OpeningTagInfo
    {
        public string Name { get; }
        public IEnumerable<Attribute> Attributes { get; }

        public OpeningTagInfo(string name, IEnumerable<Attribute> attributes)
        {
            Name = name;
            Attributes = attributes;
        }
    }
}
