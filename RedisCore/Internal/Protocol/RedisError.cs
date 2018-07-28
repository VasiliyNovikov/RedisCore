namespace RedisCore.Internal.Protocol
{
    internal sealed class RedisError : RedisObject
    {
        public string Type { get; }
        public string Message { get; }

        public RedisError(string type, string message)
        {
            Type = type ?? "Error";
            Message = message;
        }

        public override string ToString() => $"{Type}: {Message}";
    }
}