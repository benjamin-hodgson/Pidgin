namespace Pidgin.ParseStates
{
    internal struct Bookmark
    {
        public int Value { get; }
        public SourcePos Pos { get; }

        public Bookmark(int value, SourcePos sourcePos)
        {
            Value = value;
            Pos = sourcePos;
        }
    }
}