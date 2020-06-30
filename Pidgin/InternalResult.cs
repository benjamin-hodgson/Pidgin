namespace Pidgin
{
    internal static class InternalResult
    {
        public static InternalResult<T> Success<T>(T value)
            => new InternalResult<T>(true, value);
        
        /// <summary>
        /// NB! Remember to set IParseState.Error when you return a failure result
        /// </summary>
        public static InternalResult<T> Failure<T>()
            => new InternalResult<T>(false, default(T)!);
    }
    internal readonly struct InternalResult<T>
    {
        public bool Success { get; }
        public T Value { get; }

        public InternalResult(bool success, T value)
        {
            Success = success;
            Value = value;
        }
    }
}