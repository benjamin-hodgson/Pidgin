namespace Pidgin
{
    internal static class InternalResult
    {
        public static InternalResult<T> Success<T>(T value, bool consumedInput)
            => new InternalResult<T>(true, consumedInput, value);
        
        /// <summary>
        /// NB! Remember to set IParseState.Error when you return a failure result
        /// </summary>
        public static InternalResult<T> Failure<T>(bool consumedInput)
            => new InternalResult<T>(false, consumedInput, default(T)!);
    }
    internal readonly struct InternalResult<T>
    {
        public bool Success { get; }
        public bool ConsumedInput { get; }
        public T Value { get; }

        public InternalResult(bool success, bool consumedInput, T value)
        {
            Success = success;
            ConsumedInput = consumedInput;
            Value = value;
        }
    }
}