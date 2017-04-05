namespace Pidgin.Examples.Json
{
    public class JsonString : IJson
    {
        public string Value { get; }
        public JsonString(string value)
        {
            Value = value;
        }

        public override string ToString()
            => $"\"{Value}\"";
    }
}