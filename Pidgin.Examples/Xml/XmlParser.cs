using System.Collections.Generic;
using System.Linq;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Pidgin.Examples.Xml
{
    public static class XmlParser
    {
        public static Result<char, Tag> Parse(string input) => Node.Parse(input);

        static readonly Parser<char, string> Identifier =
            from first in Token(char.IsLetter)
            from rest in Token(char.IsLetterOrDigit).Many()
            select new string(new[]{ first }.Concat(rest).ToArray());

        static readonly Parser<char, char> LT = Char('<');
        static readonly Parser<char, char> GT = Char('>');
        static readonly Parser<char, char> Quote = Char('"');
        static readonly Parser<char, char> Equal = Char('=');
        static readonly Parser<char, char> Slash = Char('/');
        static readonly Parser<char, Unit> SlashGT =
            Slash.Then(Whitespaces).Then(GT).Then(Return(Unit.Value));
        static readonly Parser<char, Unit> LTSlash = 
            LT.Then(Whitespaces).Then(Slash).Then(Return(Unit.Value));
        
        static readonly Parser<char, string> AttrValue = 
            Token(c => c != '"')
                .Many()
                .Select(cs => new string(cs.ToArray()));

        static readonly Parser<char, Attribute> Attr = 
            from name in Identifier
            from eq in Equal.Between(Whitespaces)
            from val in AttrValue.Between(Quote)
            select new Attribute(name, val);

        static readonly Parser<char, OpeningTagInfo> TagBody =
            from name in Identifier
            from attrs in (
                from ws in Try(Whitespace.AtLeastOnce())
                from attrs in Attr.Separated(Whitespaces)
                select attrs
            ).Optional()
            select new OpeningTagInfo(name, attrs.GetValueOrDefault(Enumerable.Empty<Attribute>()));

        static readonly Parser<char, Tag> EmptyElementTag =
            from opening in LT
            from body in TagBody.Between(Whitespaces)
            from closing in SlashGT
            select new Tag(body.Name, body.Attributes, null);

        static readonly Parser<char, OpeningTagInfo> OpeningTag =
            TagBody
                .Between(Whitespaces)
                .Between(LT, GT);

        static Parser<char, string> ClosingTag =>
            Identifier
                .Between(Whitespaces)
                .Between(LTSlash, GT);

        static readonly Parser<char, Tag> Tag =
            from open in OpeningTag
            from children in Try(Node).Separated(Whitespaces).Between(Whitespaces)
            from close in ClosingTag
            where open.Name.Equals(close)
            select new Tag(open.Name, open.Attributes, children);
    
        static readonly Parser<char, Tag> Node = Try(EmptyElementTag).Or(Tag);

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
}