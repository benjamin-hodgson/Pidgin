namespace Pidgin.ParseStates
{
    internal struct Positioned<T>
    {
        public T Value { get; }
        public SourcePos Pos { get; }

        public Positioned(T value, SourcePos sourcePos)
        {
            Value = value;
            Pos = sourcePos;
        }
    }
}